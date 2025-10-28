using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;
using MYA.RequestProtect.Enums;
using MYA.RequestProtect.Logging;
using MYA.RequestProtect.Options;
using MYA.RequestProtect.Setup;
using System.Text.RegularExpressions;

namespace MYA.RequestProtect;

public sealed class RequestProtectMiddleware
{
    private readonly RequestProtectOptions config;
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
        this.config = config.CurrentValue;
        _next = next;
        this.logger = logger;
        this.dateTimeProvider = dateTimeProvider;
        this.hostingEnvironment = hostingEnvironment;
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
        if (config.Rules.IpWhitelist != null && config.Rules.IpWhitelist.Any(ip => ip.Equals(context.Connection.RemoteIpAddress?.ToString())))
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

    private bool HeadersAuthorised(HttpContext context)
    {
        if (config.Rules.Headers is null || config.Rules.Headers.Length == 0) return false;

        foreach (var header in config.Rules.Headers)
        {
            if (context.Request.Headers.ContainsKey(header.Header))
            {
                if (header.Value == "*") return true;

                if (context.Request.Headers[header.Header] == header.Value) return true;
            }
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
        => request.Cookies.TryGetValue(RequestProtectCookieName, out var cookieVal) && !string.IsNullOrWhiteSpace(cookieVal);
}
