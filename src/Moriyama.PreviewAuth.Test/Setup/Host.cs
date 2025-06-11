using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Moriyama.PreviewAuth.Options;
using Moriyama.PreviewAuth.Setup;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Moriyama.PreviewAuth.Tests.Setup;

internal static class Host
{
    public static string RemoteIP = "203.0.113.42";

    public static TestServer CreateTestServer(
        ILogger logger,
        PreviewAuthOptions? options = null,
        Uri? baseAddress = null)
    {
        var factory = new WebHostBuilder()
            .UseTestServer()
            .ConfigureAppConfiguration((context, config) =>
            {
                if (options is not null)
                {
                    var json = JsonSerializer.Serialize(new Dictionary<string, object?>
                    {
                        [PreviewAuthOptions.Key] = options
                    });

                    var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
                    config.AddJsonStream(stream);
                }
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Debug);
                logging.AddFilter("Moriyama.PreviewAuth", LogLevel.Debug);
                logging.AddProvider(new TestLoggerProvider(logger));
            })

            .AddPreviewAuth()

            .ConfigureServices(services =>
            {
                services.RemoveAll<IDatetimeProvider>();
                services.AddSingleton<IDatetimeProvider, TestDatetimeProvider>();
            })
            .Configure(app =>
            {

                app.Use(async (context, next) =>
                {
                    context.Connection.RemoteIpAddress = IPAddress.Parse(RemoteIP);
                    await next();
                });

                app.UseMiddleware<PreviewAuthMiddleware>();

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
}
