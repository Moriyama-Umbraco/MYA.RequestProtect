using MYA.RequestProtect.Logging;
using MYA.RequestProtect.Options;
using MYA.RequestProtect.Tests.Setup;

namespace MYA.RequestProtect.Tests;

public class LoggingValidationTests
{
    private static RequestProtectOptions BlockingOptions => new()
    {
        Enabled = true,
        Code = "valid_code",
        Rules = new AuthRules()
        {
            Rules =
            [
                new()
                {
                    Name = "Block Admin",
                    Pattern = "^/admin/.*",
                    Enabled = true,
                    AppliesTo = AppliesTo.Path
                }
            ]
        }
    };

    [Fact]
    public async Task AuthRequired_LogsCookieNotFound()
    {
        // Arrange
        var logger = new TestLogger();
        using var server = Host.CreateTestServer(logger, BlockingOptions);
        var client = server.CreateClient();

        // Act
        await client.GetAsync("/admin/page", TestContext.Current.CancellationToken);

        // Assert
        Assert.True(logger.WasLogMethodCalled(nameof(RequestProtectMiddlewareLogs.LogAuthCookieNotFound)));
    }

    [Fact]
    public async Task RuleMatches_LogsRuleValidation()
    {
        // Arrange
        var logger = new TestLogger();
        using var server = Host.CreateTestServer(logger, BlockingOptions);
        var client = server.CreateClient();

        // Act
        await client.GetAsync("/admin/page", TestContext.Current.CancellationToken);

        // Assert
        Assert.True(logger.WasLogMethodCalled(nameof(RequestProtectMiddlewareLogs.LogRuleValidation)));
    }

    [Fact]
    public async Task RuleDoesNotMatch_LogsRuleValidationFailed()
    {
        // Arrange
        var logger = new TestLogger();
        using var server = Host.CreateTestServer(logger, BlockingOptions);
        var client = server.CreateClient();

        // Act
        await client.GetAsync("/public/page", TestContext.Current.CancellationToken);

        // Assert
        Assert.True(logger.WasLogMethodCalled(nameof(RequestProtectMiddlewareLogs.LogRuleValidationFailed)));
    }

    [Fact]
    public async Task DisabledMiddleware_NoLogs()
    {
        // Arrange
        var logger = new TestLogger();
        var options = new RequestProtectOptions
        {
            Enabled = false,
            Code = "valid_code",
            Rules = new AuthRules()
            {
                Rules =
                [
                    new()
                    {
                        Name = "Block All",
                        Pattern = ".*",
                        Enabled = true,
                        AppliesTo = AppliesTo.Path
                    }
                ]
            }
        };

        using var server = Host.CreateTestServer(logger, options);
        var client = server.CreateClient();

        // Act
        await client.GetAsync("/admin/page", TestContext.Current.CancellationToken);

        // Assert - only check for middleware-specific logs (hosting logs are expected)
        Assert.False(logger.WasLogMethodCalled(nameof(RequestProtectMiddlewareLogs.LogAuthCookieNotFound)));
        Assert.False(logger.WasLogMethodCalled(nameof(RequestProtectMiddlewareLogs.LogAuthCookieInvalid)));
        Assert.False(logger.WasLogMethodCalled(nameof(RequestProtectMiddlewareLogs.LogRuleValidation)));
        Assert.False(logger.WasLogMethodCalled(nameof(RequestProtectMiddlewareLogs.LogRuleValidationFailed)));
    }

    [Fact]
    public async Task ValidCookie_SkipsCookieNotFoundLog()
    {
        // Arrange
        var logger = new TestLogger();
        using var server = Host.CreateTestServer(logger, BlockingOptions);
        var client = server.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/admin/page");
        request.Headers.Add("Cookie", "MYAPA=somevalue");

        // Act
        await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(logger.WasLogMethodCalled(nameof(RequestProtectMiddlewareLogs.LogAuthCookieNotFound)));
    }
}
