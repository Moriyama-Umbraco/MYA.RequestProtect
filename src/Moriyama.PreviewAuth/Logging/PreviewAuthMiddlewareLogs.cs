using Moriyama.PreviewAuth.Options;
using System.Diagnostics.CodeAnalysis;

namespace Moriyama.PreviewAuth.Logging;

internal static class PreviewAuthMiddlewareLogs
{
    private static readonly Action<ILogger, AuthRule, string, Exception?> _debugRuleValidation
        = LoggerMessage.Define<AuthRule, string>(
            LogLevel.Debug,
            new EventId(1003, nameof(LogRuleValidation)),
            "Rule Validation: {AuthRule} - {Url}");

    private static readonly Action<ILogger, AuthRule, string, Exception?> _warnRuleValidationFailed
        = LoggerMessage.Define<AuthRule, string>(
            LogLevel.Warning,
            new EventId(1004, nameof(LogRuleValidationFailed)),
            "Rule Validation Failed: {AuthRule} - {Url}");

    private static readonly Action<ILogger, Exception?> _debugAuthCookieNotFound
        = LoggerMessage.Define(
            LogLevel.Debug,
            new EventId(1001, nameof(LogAuthCookieNotFound)),
            "Preview Auth cookie not found in request");

    private static readonly Action<ILogger, Exception?> _warnAuthCookieInvalid
        = LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(1002, nameof(LogAuthCookieInvalid)),
            "Invalid Preview Auth cookie detected in request");

    public static void LogRuleValidation(this ILogger logger, AuthRule authRule, [StringSyntax(StringSyntaxAttribute.Uri)] string url)
        => _debugRuleValidation(logger, authRule, url, default);

    public static void LogRuleValidationFailed(this ILogger logger, AuthRule authRule, [StringSyntax(StringSyntaxAttribute.Uri)] string url)
        => _warnRuleValidationFailed(logger, authRule, url, default);

    public static void LogAuthCookieNotFound(this ILogger logger)
        => _debugAuthCookieNotFound(logger, null);

    public static void LogAuthCookieInvalid(this ILogger logger)
        => _warnAuthCookieInvalid(logger, null);
}