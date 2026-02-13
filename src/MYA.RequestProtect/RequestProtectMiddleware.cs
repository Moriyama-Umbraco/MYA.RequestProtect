using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;
using MYA.RequestProtect.Enums;
using MYA.RequestProtect.Logging;
using MYA.RequestProtect.Options;
using MYA.RequestProtect.Setup;
using System.Net;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace MYA.RequestProtect;

public sealed class RequestProtectMiddleware
{
    private readonly RequestProtectOptions config;
    private readonly RequestDelegate _next;
    private readonly ILogger logger;
    private readonly IDatetimeProvider dateTimeProvider;
    private readonly IWebHostEnvironment hostingEnvironment;
    private const string RequestProtectCookieName = "MYAPA";
    private readonly ConcurrentDictionary<string, Regex> _regexCache;
    private readonly WhitelistEntry[] _parsedWhitelist;
    private readonly HeaderEntry[] _parsedHeaders;

    private readonly struct WhitelistEntry
    {
        public readonly bool IsCidr;
        public readonly IPNetwork? Network;
        public readonly IPAddress? DirectIp;
        public readonly string Pattern;

        public WhitelistEntry(string pattern, bool isCidr, IPNetwork? network, IPAddress? directIp)
        {
            Pattern = pattern;
            IsCidr = isCidr;
            Network = network;
            DirectIp = directIp;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Matches(IPAddress ip) => IsCidr
            ? Network?.Contains(ip) == true
       : DirectIp?.Equals(ip) == true;
    }

    private readonly struct HeaderEntry
    {
        public readonly string Name;
        public readonly string? Value;
        public readonly bool IsWildcard;

        public HeaderEntry(string name, string? value)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = value;
            IsWildcard = value == "*";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Matches(IHeaderDictionary headers)
        {
            if (!headers.TryGetValue(Name, out var headerValue))
                return false;

            if (IsWildcard)
                return true;

            return !string.IsNullOrEmpty(Value) &&
                 headerValue.ToString().Equals(Value, StringComparison.Ordinal);
        }
    }

    public RequestProtectMiddleware(RequestDelegate next,
        ILogger<RequestProtectMiddleware> logger,
        IOptionsMonitor<RequestProtectOptions> config,
        IDatetimeProvider dateTimeProvider,
    IWebHostEnvironment hostingEnvironment)
    {
        this.config = config.CurrentValue;
        _next = next;
        this.logger = logger;
        this.dateTimeProvider = dateTimeProvider;
        this.hostingEnvironment = hostingEnvironment;
        _regexCache = new ConcurrentDictionary<string, Regex>();
        _parsedWhitelist = ParseWhitelist(this.config.Rules.IpWhitelist);
        _parsedHeaders = ParseHeaders(this.config.Rules.Headers);
    }

    private static HeaderEntry[] ParseHeaders(HeaderDetail[]? headers)
    {
        if (headers == null || headers.Length == 0)
            return Array.Empty<HeaderEntry>();

        var result = new HeaderEntry[headers.Length];
        for (var i = 0; i < headers.Length; i++)
        {
            if (string.IsNullOrEmpty(headers[i].Header))
                continue;

            result[i] = new HeaderEntry(headers[i].Header, headers[i].Value);
        }
        return result;
    }

    private static WhitelistEntry[] ParseWhitelist(string[]? whitelist)
    {
        if (whitelist == null || whitelist.Length == 0)
            return Array.Empty<WhitelistEntry>();

        var result = new List<WhitelistEntry>(whitelist.Length);

        foreach (var ip in whitelist)
        {
            if (string.IsNullOrWhiteSpace(ip)) continue;

            if (ip.Contains('/', StringComparison.Ordinal))
            {
                if (IPNetwork.TryParse(ip, out var network))
                {
                    result.Add(new WhitelistEntry(ip, true, network, null));
                }
            }
            else if (IPAddress.TryParse(ip, out var directIp))
            {
                result.Add(new WhitelistEntry(ip, false, null, directIp));
            }
        }

        return result.ToArray();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!config.Enabled || HasMiddlewareAuthCookie(context.Request))
        {
            await _next(context);
            return;
        }

        logger.LogAuthCookieNotFound();

        if (RequestIsAuthorised(context, out var setCookie))
        {
            if (setCookie is true)
            {
                context.Response.Cookies.Append(RequestProtectCookieName, dateTimeProvider.Now.Ticks.ToString(),
                    new CookieOptions
                    {
                        Expires = dateTimeProvider.NowOffSet.AddMinutes(30),
                        HttpOnly = true,
                        SameSite = SameSiteMode.Strict,
                        IsEssential = true,
                        Secure = true
                    });
            }
        }
        else
        {
            await HandleUnAuthorisedRequest(context);
            return;
        }

        await _next(context);
    }

    private async Task HandleUnAuthorisedRequest(HttpContext context)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(config.Response.Destination))
            {
                await DefaultResponse(context);
                return;
            }

            switch (config.Response.ResponseType)
            {
                case ResponseTypes.Redirect:
                    await HandleRedirectResponse(context);
                    return;
                case ResponseTypes.StaticFile:
                    await HandleStaticFileResponse(context);
                    return;
                case ResponseTypes.Default:
                default:
                    await DefaultResponse(context);
                    return;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occured when Handling UnAutorisedRequest");

            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Bad Request: Unknown path");
        }
    }

    private async Task HandleStaticFileResponse(HttpContext context)
    {
        if (string.IsNullOrWhiteSpace(config.Response.Destination))
        {
            await DefaultResponse(context);
            return;
        }

        var file = hostingEnvironment.WebRootFileProvider.GetFileInfo(config.Response.Destination);
        if (file.Exists)
        {
            using var readSteam = file.CreateReadStream();
            using var reader = new StreamReader(readSteam);
            var content = await reader.ReadToEndAsync();

            context.Response.ContentType = config.Response.MimeType;
            await context.Response.WriteAsync(content);

            return;
        }

        await DefaultResponse(context);
    }

    private async Task HandleRedirectResponse(HttpContext context)
    {
        if (!Uri.TryCreate(config.Response.Destination, UriKind.RelativeOrAbsolute, out var targetUri))
        {
            await DefaultResponse(context);
            return;
        }

        // Validate: absolute URIs must be http/https, relative URIs must start with /
        if (targetUri.IsAbsoluteUri)
        {
            if (targetUri.Scheme != Uri.UriSchemeHttp && targetUri.Scheme != Uri.UriSchemeHttps)
            {
                await DefaultResponse(context);
                return;
            }
        }
        else if (config.Response.Destination is not null && !config.Response.Destination.StartsWith('/'))
        {
            await DefaultResponse(context);
            return;
        }

        var currentUri = new Uri($"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}");

        if (!targetUri.IsAbsoluteUri)
        {
            targetUri = new Uri(currentUri, targetUri);
        }

        if (currentUri.GetLeftPart(UriPartial.Path).Equals(targetUri.GetLeftPart(UriPartial.Path), StringComparison.OrdinalIgnoreCase))
        {
            // Would cause redirect loop - use default response instead
            await DefaultResponse(context);
            return;
        }

        context.Response.Redirect(targetUri.ToString());
    }

    private async Task DefaultResponse(HttpContext context)
    {
        context.Response.StatusCode = config.Response.StatusCode;
        await context.Response.WriteAsync("Bad Request: Unknown path");
    }

    private bool RequestIsAuthorised(HttpContext context, out bool setCookie)
    {
        setCookie = false;
        try
        {
            if (AuthNotNeeded(context))
            {
                return true;
            }

            if (ValidateCode(context.Request.Query))
            {
                setCookie = true;
                return true;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to check authorisation of request");
        }

        return false;
    }

    private bool ValidateCode(IQueryCollection query)
    {
        var queryCode = query[config.QueryKey];
        return queryCode.Equals(config.Code);
    }

    private bool AuthNotNeeded(HttpContext context)
    {
        if (config.Rules.IpWhitelist is not null && config.Rules.IpWhitelist.Length > 0 && IsIpAllowed(context.Connection.RemoteIpAddress))
        {
            return true;
        }

        if (HeadersAuthorised(context))
        {
            return true;
        }

        bool hasRules = config.Rules.Rules is { Length: > 0 };
        bool hasGroups = config.Rules.RuleGroups is { Length: > 0 };

        if (hasRules || hasGroups)
        {
            bool matched = EvaluateRulesAndGroups(
                config.Rules.Rules, config.Rules.RuleGroups, config.Rules.RulesOperator, context.Request);
            return !matched; // If matched -> auth IS needed
        }

        return false;
    }

    private bool IsIpAllowed(IPAddress? remoteIp)
    {
        if (remoteIp is null) return false;

        foreach (ref readonly var entry in _parsedWhitelist.AsSpan())
        {
            logger.LogDebug("Checking IP whitelist entry: {ip} against remote IP: {remoteIp}", entry.Pattern, remoteIp);
            if (entry.Matches(remoteIp)) return true;
        }

        return false;
    }

    private bool EvaluateRulesAndGroups(
        AuthRule[]? rules, AuthRuleGroup[]? ruleGroups,
        RuleGroupOperator op, HttpRequest request)
    {
        bool hasEnabled = false;

        if (rules is { Length: > 0 })
        {
            foreach (var r in rules)
            {
                if (!r.Enabled) continue;
                hasEnabled = true;
                bool result = DoesRulePass(r, request);
                if (op == RuleGroupOperator.Any && result) return true;
                if (op == RuleGroupOperator.All && !result) return false;
            }
        }

        if (ruleGroups is { Length: > 0 })
        {
            foreach (var g in ruleGroups)
            {
                if (!g.Enabled) continue;
                hasEnabled = true;
                bool result = EvaluateRulesAndGroups(g.Rules, g.RuleGroups, g.RulesOperator, request);
                if (op == RuleGroupOperator.Any && result) return true;
                if (op == RuleGroupOperator.All && !result) return false;
            }
        }

        if (!hasEnabled) return false;
        return op == RuleGroupOperator.All; // All passed
    }

    private bool DoesRulePass(AuthRule r, HttpRequest request)
    {
        var url = request.GetDisplayUrl();
        var regex = _regexCache.GetOrAdd(r.Pattern, pattern =>
            new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant));

        var value = r.AppliesTo switch
        {
            AppliesTo.Host => request.Host.ToString(),
            AppliesTo.Query => request.QueryString.ToString(),
            AppliesTo.Path => request.Path.ToString(),
            _ => request.Path.ToString()
        };

        var ruleResult = regex.IsMatch(value);

        if (ruleResult)
        {
            logger.LogRuleValidation(r, url);
        }
        else
        {
            logger.LogRuleValidationFailed(r, url);
        }

        return ruleResult;
    }

    private static bool HasMiddlewareAuthCookie(HttpRequest request)
        => request.Cookies.TryGetValue(RequestProtectCookieName, out var cookieVal) && !string.IsNullOrWhiteSpace(cookieVal);

    private bool HeadersAuthorised(HttpContext context)
    {
        if (_parsedHeaders.Length == 0) return false;

        foreach (ref readonly var header in _parsedHeaders.AsSpan())
        {
            if (header.Matches(context.Request.Headers)) return true;
        }

        return false;
    }
}
