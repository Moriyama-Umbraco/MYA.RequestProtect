using MYA.RequestProtect.Options;
using MYA.RequestProtect.Tests.Setup;

namespace MYA.RequestProtect.Tests;

public class CookieConfigurationTests
{
    private readonly TestLogger logger = new();

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
                    Name = "Block All",
                    Pattern = ".*",
                    Enabled = true,
                    AppliesTo = AppliesTo.Path
                }
            ]
        }
    };

    [Fact]
    public async Task ValidAuth_SetsCookie_WithCustomExpiry()
    {
        // Arrange
        var options = BlockingOptions;
        options.Cookie.ExpiryMinutes = 60;

        using var server = Host.CreateTestServer(logger, options);
        var client = server.CreateClient();

        // Act
        var response = await client.GetAsync("/blog/post?auth=valid_code", TestContext.Current.CancellationToken);

        // Assert
        await Verify(response);
    }

    [Fact]
    public async Task ValidAuth_SessionCookie_WhenPersistCookieFalse()
    {
        // Arrange
        var options = BlockingOptions;
        options.Cookie.PersistCookie = false;

        using var server = Host.CreateTestServer(logger, options);
        var client = server.CreateClient();

        // Act
        var response = await client.GetAsync("/blog/post?auth=valid_code", TestContext.Current.CancellationToken);

        // Assert
        await Verify(response);
    }

    [Fact]
    public async Task ValidAuth_SessionCookie_ExpiryMinutesIgnored()
    {
        // Arrange
        var options = BlockingOptions;
        options.Cookie.PersistCookie = false;
        options.Cookie.ExpiryMinutes = 120;

        using var server = Host.CreateTestServer(logger, options);
        var client = server.CreateClient();

        // Act
        var response = await client.GetAsync("/blog/post?auth=valid_code", TestContext.Current.CancellationToken);

        // Assert
        await Verify(response);
    }
}
