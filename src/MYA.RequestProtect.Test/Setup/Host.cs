using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using MYA.RequestProtect.Options;
using MYA.RequestProtect.Setup;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;

namespace MYA.RequestProtect.Tests.Setup;

internal static class Host
{
    public const string DefaultRemoteIP = "203.0.113.42";

    public static TestServer CreateTestServer(
        ILogger logger,
        RequestProtectOptions? options = null,
        Uri? baseAddress = null,
        string? webRootPath = null, string? remoteIp = null,
        string? forwardedHeaderName = null, string? forwardedHeaderValue = null)
    {
        ReloadableConfigurationProvider? configProvider = null;

        var builder = new WebHostBuilder()
            .UseTestServer();

        if (webRootPath is not null)
        {
            builder.UseWebRoot(webRootPath);
        }

        var factory = builder
            .ConfigureAppConfiguration((context, config) =>
            {
                if (options is not null)
                {
                    var source = new ReloadableConfigurationSource();
                    source.Provider.Update(FlattenOptionsForTest(options));
                    configProvider = source.Provider;
                    config.Add(source);
                }
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Debug);
                logging.AddFilter("MYA.RequestProtect", LogLevel.Debug);
                logging.AddProvider(new TestLoggerProvider(logger));
            })

            .AddRequestProtect()

            .ConfigureServices(services =>
            {
                services.RemoveAll<IDatetimeProvider>();
                services.AddSingleton<IDatetimeProvider, TestDatetimeProvider>();
                if (configProvider is not null)
                {
                    services.AddSingleton(configProvider);
                }
            })
            .Configure(app =>
            {

                app.Use(async (context, next) =>
                {
                    context.Connection.RemoteIpAddress = IPAddress.Parse(string.IsNullOrWhiteSpace(remoteIp) ? DefaultRemoteIP : remoteIp);

                    if (!string.IsNullOrWhiteSpace(forwardedHeaderName) && forwardedHeaderValue is not null)
                    {
                        context.Request.Headers[forwardedHeaderName] = forwardedHeaderValue;
                    }

                    context.Request.Headers["singleHeader"] = "singleHeader";
                    context.Request.Headers["wildHeader"] = "wildHeader";
                    context.Request.Headers["headerTwo"] = "2";
                    await next();
                });

                app.UseMiddleware<RequestProtectMiddleware>();

                app.Use(async (HttpContext ctx, Func<Task> task) =>
                {
                    // This is a test endpoint to verify the middleware works
                    ctx.Response.StatusCode = 200;
                    await ctx.Response.WriteAsync("Test endpoint reached");
                });
            });

        return new TestServer(factory)
        {
            BaseAddress = baseAddress ?? new Uri("https://localhost/")
        };
    }

    internal static Dictionary<string, string?> FlattenOptionsForTest(RequestProtectOptions options)
    {
        var json = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            [RequestProtectOptions.Key] = options
        });

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var flat = new ConfigurationBuilder().AddJsonStream(stream).Build();
        return flat.AsEnumerable().ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);
    }
}
