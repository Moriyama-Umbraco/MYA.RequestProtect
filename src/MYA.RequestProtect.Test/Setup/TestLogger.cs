using Microsoft.Extensions.Logging;
using MYA.RequestProtect.Logging;

namespace MYA.RequestProtect.Tests.Setup;

public class TestLogger : ILogger
{
    public List<(LogLevel LogLevel, EventId EventId, string Message)> LogMessages { get; } = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        LogMessages.Add((logLevel, eventId, formatter(state, exception)));
    }

    public bool WasLogMethodCalled(string methodName)
    {
        return methodName switch
        {
            nameof(RequestProtectMiddlewareLogs.LogAuthCookieNotFound) => LogMessages.Any(m => m.EventId.Name == nameof(RequestProtectMiddlewareLogs.LogAuthCookieNotFound)),
            nameof(RequestProtectMiddlewareLogs.LogAuthCookieInvalid) => LogMessages.Any(m => m.EventId.Name == nameof(RequestProtectMiddlewareLogs.LogAuthCookieInvalid)),
            nameof(RequestProtectMiddlewareLogs.LogRuleValidation) => LogMessages.Any(m => m.EventId.Name == nameof(RequestProtectMiddlewareLogs.LogRuleValidation)),
            nameof(RequestProtectMiddlewareLogs.LogRuleValidationFailed) => LogMessages.Any(m => m.EventId.Name == nameof(RequestProtectMiddlewareLogs.LogRuleValidationFailed)),
            _ => false
        };
    }
}

internal class TestLoggerProvider : ILoggerProvider
{
    private readonly ILogger _logger;

    public TestLoggerProvider(ILogger logger) => _logger = logger;

    public ILogger CreateLogger(string categoryName)
    {
        return _logger;
    }

    public void Dispose()
    {
        // No resources to dispose
    }
}
