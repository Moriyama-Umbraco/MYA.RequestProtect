using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Hosting;
using MYA.RequestProtect.Options;
using MYA.RequestProtect.Setup;
using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;

namespace MYA.RequestProtect.Benchmarks
{
    [MemoryDiagnoser]
    public class RequestProtectMiddlewareBenchmark
    {
        private RequestProtectMiddleware _middleware;
        private HttpContext _context;
        private RequestDelegate _next;
        private IOptionsMonitor<RequestProtectOptions> _optionsMonitor;

        [GlobalSetup]
        public void Setup()
        {
            _next = (ctx) => Task.CompletedTask;
            var logger = LoggerFactory.Create(builder => builder.AddConsole())
      .CreateLogger<RequestProtectMiddleware>();

            var options = new RequestProtectOptions
            {
                Enabled = true,
                Code = "testcode",
                QueryKey = "auth",
                Rules = new AuthRules
                {
                    IpWhitelist = new[] { "127.0.0.1", "192.168.1.0/24" },
                    Rules = new[]
                    {
                        new AuthRule
                        {
                            Name = "Test Rule",
                            Pattern = @"^/test/.*$",
                            Enabled = true
                        }
                    }
                }
            };

            _optionsMonitor = new OptionsMonitorWrapper<RequestProtectOptions>(options);
            var dateTimeProvider = new DefaultDatetimeProvider();
            var env = new TestHostingEnvironment();

            _middleware = new RequestProtectMiddleware(_next, logger, _optionsMonitor,
                dateTimeProvider, env);

            _context = new DefaultHttpContext();
            _context.Request.Path = "/test/path";
            _context.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        }

        [Benchmark]
        public Task InvokeAsync_WithIPWhitelist()
        {
            return _middleware.InvokeAsync(_context);
        }

        [Benchmark]
        public Task InvokeAsync_WithRuleMatch()
        {
            _context.Connection.RemoteIpAddress = IPAddress.Parse("1.1.1.1"); // Non-whitelisted IP
            return _middleware.InvokeAsync(_context);
        }

        [Benchmark]
        public Task InvokeAsync_WithAuthCode()
        {
            _context.Connection.RemoteIpAddress = IPAddress.Parse("1.1.1.1");
            _context.Request.QueryString = new QueryString("?auth=testcode");
            return _middleware.InvokeAsync(_context);
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
}