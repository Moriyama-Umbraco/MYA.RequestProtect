using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;
using Moriyama.PreviewAuth.Logging;
using Moriyama.PreviewAuth.Options;
using Moriyama.PreviewAuth.Setup;
using System.Text.RegularExpressions;

namespace Moriyama.PreviewAuth;

public sealed class PreviewAuthMiddleware
{
    private readonly PreviewAuthOptions config;
    private readonly RequestDelegate _next;
    private readonly ILogger logger;
    private readonly IDatetimeProvider dateTimeProvider;

    private const string PreviewAuthCookieName = "MYAPA";

    public PreviewAuthMiddleware(RequestDelegate next, 
        ILogger<PreviewAuthMiddleware> logger, 
        IOptionsMonitor<PreviewAuthOptions> config,
        IDatetimeProvider dateTimeProvider)
    {
        this.config = config.CurrentValue;
        _next = next;
        this.logger = logger;
        this.dateTimeProvider = dateTimeProvider;
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
                context.Response.Cookies.Append(PreviewAuthCookieName, dateTimeProvider.Now.Ticks.ToString(),
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
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Bad Request: Unknown path");
            return;
        }

        await _next(context);
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
        if (config.Rules.IpWhitelist != null && config.Rules.IpWhitelist.Any(ip => ip.Equals(context.Connection.RemoteIpAddress?.ToString())))
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

    private bool DoesRulePass(AuthRule r, HttpRequest request)
    {
        var url = request.GetDisplayUrl();
        var ruleResult = r.AppliesTo switch
        {
            AppliesTo.Host => Regex.IsMatch(request.Host.ToString(), r.Pattern),
            AppliesTo.Query => Regex.IsMatch(request.QueryString.ToString(), r.Pattern),
            AppliesTo.Path => Regex.IsMatch(request.Path, r.Pattern),
            _ => Regex.IsMatch(request.Path, r.Pattern)
        };

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
        => request.Cookies.TryGetValue(PreviewAuthCookieName, out var cookieVal) && !string.IsNullOrWhiteSpace(cookieVal);
}
