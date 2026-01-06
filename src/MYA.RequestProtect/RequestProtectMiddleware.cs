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

        if (config.Rules.Rules?.Length > 0)
        {
            var rulesResults = config.Rules.Rules.Any(r => r.Enabled && DoesRulePass(r, context.Request));
            return !rulesResults; //If any rules pass the we need to verify the code
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
