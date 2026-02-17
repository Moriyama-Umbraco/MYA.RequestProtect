using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Hosting;
using MYA.RequestProtect.Options;
using MYA.RequestProtect.Setup;
using System.Net;
using Microsoft.Extensions.FileProviders;

namespace MYA.RequestProtect.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
[SimpleJob(RuntimeMoniker.Net90)]
[SimpleJob(RuntimeMoniker.Net10_0)]
public class RequestProtectMiddlewareBenchmark
{
    private RequestProtectMiddleware _middleware = null!;
    private RequestDelegate _next = null!;
    private IOptionsMonitor<RequestProtectOptions> _optionsMonitor = null!;

    [GlobalSetup]
    public void Setup()
    {
        _next = (ctx) => Task.CompletedTask;
        var logger = NullLoggerFactory.Instance.CreateLogger<RequestProtectMiddleware>();

        var options = new RequestProtectOptions
        {
            Enabled = true,
            Code = "testcode",
            QueryKey = "auth",
            Rules = new AuthRules
            {
                IpWhitelist = ["127.0.0.1", "192.168.1.0/24"],
                Rules =
                [
                    new AuthRule
                    {
                        Name = "Test Rule",
                        Pattern = @"^/test/.*$",
                        Enabled = true
                    }
                ]
            }
        };

        _optionsMonitor = new OptionsMonitorWrapper<RequestProtectOptions>(options);
        var dateTimeProvider = new DefaultDatetimeProvider();
        var env = new TestHostingEnvironment();

        _middleware = new RequestProtectMiddleware(_next, logger, _optionsMonitor,
            dateTimeProvider, env);
    }

    private static DefaultHttpContext CreateContext(string remoteIp = "127.0.0.1")
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/test/path";
        context.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);
        return context;
    }

    [Benchmark]
    public Task InvokeAsync_WithIPWhitelist()
    {
        var context = CreateContext();
        return _middleware.InvokeAsync(context);
    }

    [Benchmark]
    public Task InvokeAsync_WithRuleMatch()
    {
        var context = CreateContext("1.1.1.1");
        return _middleware.InvokeAsync(context);
    }

    [Benchmark]
    public Task InvokeAsync_WithAuthCode()
    {
        var context = CreateContext("1.1.1.1");
        context.Request.QueryString = new QueryString("?auth=testcode");
        return _middleware.InvokeAsync(context);
    }
}

public class TestHostingEnvironment : IWebHostEnvironment
{
    public string WebRootPath { get; set; } = "";
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public string ApplicationName { get; set; } = "";
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    public string ContentRootPath { get; set; } = "";
    public string EnvironmentName { get; set; } = "Development";
}

public class OptionsMonitorWrapper<T> : IOptionsMonitor<T>
{
    private readonly T _currentValue;

    public OptionsMonitorWrapper(T currentValue)
    {
        _currentValue = currentValue;
    }

    public T CurrentValue => _currentValue;

    public T Get(string? name) => _currentValue;

    public IDisposable OnChange(Action<T, string?> listener) => new DummyDisposable();
}

public class DummyDisposable : IDisposable
{
    public void Dispose() { }
}

public class DefaultDatetimeProvider : IDatetimeProvider
{
    public DateTime Now => DateTime.Now;
    public DateTimeOffset NowOffSet => DateTimeOffset.Now;
}