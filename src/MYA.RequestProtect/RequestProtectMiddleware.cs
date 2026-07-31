using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;
using MYA.RequestProtect.Enums;
using MYA.RequestProtect.Logging;
using MYA.RequestProtect.Models;
using MYA.RequestProtect.Options;
using MYA.RequestProtect.Setup;
using System.Net;
using System.Text.RegularExpressions;
using System.Collections.Frozen;
using System.Runtime.InteropServices;

namespace MYA.RequestProtect;

public sealed class RequestProtectMiddleware
{
    private volatile CompiledConfig _compiled;

    private RequestProtectOptions config => _compiled.Options;

    private sealed class CompiledConfig
    {
        public required RequestProtectOptions Options { get; init; }
        public required FrozenDictionary<string, Regex> RegexCache { get; init; }
        public required List<WhitelistEntry> Whitelist { get; init; }
        public required List<HeaderEntry> Headers { get; init; }
    }

    private static CompiledConfig Compile(RequestProtectOptions options) => new()
    {
        Options = options,
        RegexCache = BuildRegexCache(options.Rules),
        Whitelist = ParseWhitelist(options.Rules.IpWhitelist),
        Headers = ParseHeaders(options.Rules.Headers)
    };

    private readonly RequestDelegate _next;
    private readonly ILogger logger;
    private readonly IDatetimeProvider dateTimeProvider;
    private readonly IWebHostEnvironment hostingEnvironment;
    private const string RequestProtectCookieName = "MYAPA";

    public RequestProtectMiddleware(RequestDelegate next,
        ILogger<RequestProtectMiddleware> logger,
        IOptionsMonitor<RequestProtectOptions> config,
        IDatetimeProvider dateTimeProvider,
    IWebHostEnvironment hostingEnvironment)
    {
        _next = next;
        this.logger = logger;
        this.dateTimeProvider = dateTimeProvider;
        this.hostingEnvironment = hostingEnvironment;
        _compiled = Compile(config.CurrentValue);
        config.OnChange(newOptions => _compiled = Compile(newOptions));
    }

    private static List<HeaderEntry> ParseHeaders(HeaderDetail[]? headers)
    {
        if (headers == null || headers.Length == 0)
            return [];

        var result = new List<HeaderEntry>(headers.Length);
        for (var i = 0; i < headers.Length; i++)
        {
            if (string.IsNullOrEmpty(headers[i].Header))
                continue;

            result.Add(new HeaderEntry(headers[i].Header, headers[i].Value));
        }
        return result;
    }

    private static List<WhitelistEntry> ParseWhitelist(string[]? whitelist)
    {
        if (whitelist == null || whitelist.Length == 0)
            return [];

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

        return result;
    }

    private static FrozenDictionary<string, Regex> BuildRegexCache(AuthRules rules)
    {
        var cache = new Dictionary<string, Regex>(StringComparer.Ordinal);
        AddPatternsToCache(rules.Rules, cache);
        AddGroupPatternsToCache(rules.RuleGroups, cache);
        return cache.ToFrozenDictionary(StringComparer.Ordinal);
    }

    private static void AddPatternsToCache(AuthRule[]? rules, Dictionary<string, Regex> cache)
    {
        if (rules is not { Length: > 0 }) return;
        foreach (var r in rules)
        {
            if (!string.IsNullOrEmpty(r.Pattern))
                cache.TryAdd(r.Pattern, new Regex(r.Pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant));
        }
    }

    private static void AddGroupPatternsToCache(AuthRuleGroup[]? groups, Dictionary<string, Regex> cache)
    {
        if (groups is not { Length: > 0 }) return;
        foreach (var g in groups)
        {
            AddPatternsToCache(g.Rules, cache);
            AddGroupPatternsToCache(g.RuleGroups, cache);
        }
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
                var cookieOpts = new CookieOptions
                {
                    HttpOnly = true,
                    SameSite = SameSiteMode.Strict,
                    IsEssential = true,
                    Secure = true
                };

                if (config.Cookie.PersistCookie)
                {
                    cookieOpts.Expires = dateTimeProvider.NowOffSet.AddMinutes(config.Cookie.ExpiryMinutes);
                }

                context.Response.Cookies.Append(RequestProtectCookieName, dateTimeProvider.Now.Ticks.ToString(), cookieOpts);
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
        if (config.Rules.IpWhitelist is not null && config.Rules.IpWhitelist.Length > 0 && IsIpAllowed(ResolveClientIp(context)))
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

    /// <summary>
    /// Resolves the client IP address, preferring a configured forwarded header (e.g. Cloudflare's
    /// "cf-connecting-ip") when <see cref="ForwardedIpSettings.Enabled"/> is set, and falling back to the
    /// transport connection IP otherwise. The header is only trusted when explicitly enabled and, when
    /// <see cref="ForwardedIpSettings.TrustedHosts"/> is configured, only for those hosts. This prevents
    /// clients spoofing it (e.g. via a non-proxied raw domain) to bypass the IP whitelist.
    /// </summary>
    private IPAddress? ResolveClientIp(HttpContext context)
    {
        var forwarded = config.ForwardedIp;

        if (forwarded is { Enabled: true } && !string.IsNullOrWhiteSpace(forwarded.HeaderName))
        {
            if (!IsTrustedHost(context.Request.Host, forwarded.TrustedHosts))
            {
                logger.LogDebug("Forwarded header {header} ignored for untrusted host {host}", forwarded.HeaderName, context.Request.Host.Host);
            }
            else if (context.Request.Headers.TryGetValue(forwarded.HeaderName, out var headerValues))
            {
                var raw = headerValues.ToString();
                if (!string.IsNullOrWhiteSpace(raw))
                {
                    // cf-connecting-ip carries a single IP; X-Forwarded-For style headers carry a comma
                    // separated list where the originating client is the first entry.
                    var commaIndex = raw.IndexOf(',', StringComparison.Ordinal);
                    var candidate = commaIndex >= 0 ? raw.AsSpan(0, commaIndex) : raw.AsSpan();
                    candidate = candidate.Trim();

                    if (IPAddress.TryParse(candidate, out var forwardedIp))
                    {
                        logger.LogDebug("Resolved client IP {ip} from forwarded header {header}", forwardedIp, forwarded.HeaderName);
                        return forwardedIp;
                    }

                    logger.LogWarning("Forwarded header {header} present but could not be parsed as an IP address", forwarded.HeaderName);
                }
            }
        }

        return context.Connection.RemoteIpAddress;
    }

    /// <summary>
    /// Determines whether the forwarded header should be trusted for the request host. When no trusted hosts
    /// are configured the header is trusted on all hosts; otherwise only on a case-insensitive exact match of
    /// the request host (without port).
    /// </summary>
    private static bool IsTrustedHost(HostString host, string[]? trustedHosts)
    {
        if (trustedHosts is not { Length: > 0 }) return true;

        var requestHost = host.Host;
        if (string.IsNullOrEmpty(requestHost)) return false;

        foreach (var trusted in trustedHosts)
        {
            if (!string.IsNullOrWhiteSpace(trusted)
                && string.Equals(trusted.Trim(), requestHost, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsIpAllowed(IPAddress? remoteIp)
    {
        if (remoteIp is null) return false;

        var whitelist = _compiled.Whitelist;
        foreach (ref readonly var entry in CollectionsMarshal.AsSpan(whitelist))
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
        if (!_compiled.RegexCache.TryGetValue(r.Pattern, out var regex))
        {
            regex = new Regex(r.Pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);
        }

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
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogRuleValidation(r, request.GetDisplayUrl());
        }
        else
        {
            if (logger.IsEnabled(LogLevel.Warning))
                logger.LogRuleValidationFailed(r, request.GetDisplayUrl());
        }

        return ruleResult;
    }

    private static bool HasMiddlewareAuthCookie(HttpRequest request)
        => request.Cookies.TryGetValue(RequestProtectCookieName, out var cookieVal) && !string.IsNullOrWhiteSpace(cookieVal);

    private bool HeadersAuthorised(HttpContext context)
    {
        var headers = _compiled.Headers;
        if (headers.Count == 0) return false;

        foreach (ref readonly var header in CollectionsMarshal.AsSpan(headers))
        {
            if (header.Matches(context.Request.Headers)) return true;
        }

        return false;
    }
}
