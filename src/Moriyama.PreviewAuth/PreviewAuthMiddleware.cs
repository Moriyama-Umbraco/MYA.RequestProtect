using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;
using Moriyama.PreviewAuth.Options;
using System.Text.RegularExpressions;

namespace Moriyama.PreviewAuth;

public class PreviewAuthMiddleware
{
	private readonly PreviewAuthOptions config;
	private readonly RequestDelegate _next;
	private readonly ILogger logger;

	private const string PreviewAuthCookieName = "MYAPA";

	public PreviewAuthMiddleware(RequestDelegate next, ILogger<PreviewAuthMiddleware> logger, IOptionsMonitor<PreviewAuthOptions> config)
	{
		this.config = config.CurrentValue;
		_next = next;
		this.logger = logger;
	}

	public async Task InvokeAsync(HttpContext context)
	{
		logger.LogDebug("Preview Auth Middleware invoked");
		// Log request information
		if(config.Enabled && !HasMiddlewareAuthCookie(context.Request))
		{
			logger.LogDebug("Preview Auth Middleware: Auth Cookie not found");

			if (RequestIsAuthorised(context, out var setCookie))
			{
				if (setCookie is not null and true)
				{
					context.Response.Cookies.Append(PreviewAuthCookieName, DateTime.Now.Ticks.ToString(),
						new CookieOptions
						{
							Expires = DateTimeOffset.Now.AddMinutes(30),
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
				
		}

		// Call the next middleware in the pipeline
		await _next(context);

		// Log response information
		Console.WriteLine($"Response: {context.Response.StatusCode}");
	}

	private bool RequestIsAuthorised(HttpContext context, out bool? setCookie)
	{
		
		try
		{
			if(AuthNotNeeded(context))
			{
				setCookie = false;
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
		setCookie = false;
		return false;
	}

	private bool ValidateCode(IQueryCollection query)
	{
		var queryCode = query[config.QueryKey];

		return queryCode.Equals(config.Code);
	}

	private bool AuthNotNeeded(HttpContext context)
	{
		if (config.Rules.IpWhitelist != null && config.Rules.IpWhitelist.Any(ip => ip.Equals(context.Connection.RemoteIpAddress))) return true;

		if(config.Rules.Rules != null )
		{
			var rulesResults = config.Rules.Rules.Length > 0 && config.Rules.Rules.Where(r => r.Enabled).All(r => DoesRulePass(r, context.Request));
			if (rulesResults) return true;
		}

		return false;
	}

	private bool DoesRulePass(AuthRule r, HttpRequest request)
	{
		logger.LogDebug("Rule Validation: {rule} - {request}", r.ToString(), request.GetDisplayUrl());

		return r.AppliesTo switch
		{
			AppliesTo.Host => Regex.IsMatch(request.Host.ToString(), r.Pattern),
			AppliesTo.Query => Regex.IsMatch(request.QueryString.ToString(), r.Pattern),
			AppliesTo.Path => Regex.IsMatch(request.Path, r.Pattern),
			_ => Regex.IsMatch(request.Path, r.Pattern)
		};
		
	}

	private bool HasMiddlewareAuthCookie(HttpRequest request)
	{
		if (!request.Cookies.ContainsKey(PreviewAuthCookieName)) return false;

		var cookieVal = request.Cookies[PreviewAuthCookieName];

		return !string.IsNullOrWhiteSpace(cookieVal);
		
	}
}
