using System.Linq;
using Microsoft.Extensions.Options;
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

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(525_601)]
    public void ExpiryMinutes_InvalidValues_FailsValidation(int expiryMinutes)
    {
        var options = BlockingOptions;
        options.Cookie.ExpiryMinutes = expiryMinutes;

        Assert.Throws<OptionsValidationException>(
            () => Host.CreateTestServer(logger, options));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(525_600)]
    public async Task ExpiryMinutes_BoundaryValues_PassesValidation(int expiryMinutes)
    {
        var options = BlockingOptions;
        options.Cookie.ExpiryMinutes = expiryMinutes;

        using var server = Host.CreateTestServer(logger, options);
        var client = server.CreateClient();

        var response = await client.GetAsync("/blog/post?auth=valid_code", TestContext.Current.CancellationToken);

        Assert.True(response.IsSuccessStatusCode);
    }

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

    [Fact]
    public async Task SlidingExpiration_Enabled_RefreshesCookieOnEachRequest()
    {
        // Arrange
        var options = BlockingOptions;
        options.Cookie.SlidingExpiration = true;
        options.Cookie.PersistCookie = true;
        options.Cookie.ExpiryMinutes = 45;

        using var server = Host.CreateTestServer(logger, options);
        var client = server.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/admin/secret");
        request.Headers.Add("Cookie", "MYAPA=somevalue");

        // Act
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(response.Headers.Contains("Set-Cookie"));
        var setCookie = response.Headers.GetValues("Set-Cookie").Single();
        Assert.Contains("MYAPA=somevalue", setCookie);
        // TestDatetimeProvider is fixed at 2025-06-10 12:00:00 UTC; ExpiryMinutes=45 pins the exact
        // refreshed expiry, so a slide-refresh that ignores ExpiryMinutes (e.g. hardcodes a value) fails this.
        Assert.Contains("expires=Tue, 10 Jun 2025 12:45:00 GMT", setCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SlidingExpiration_Disabled_DoesNotRefreshCookie()
    {
        // Arrange
        var options = BlockingOptions;
        options.Cookie.SlidingExpiration = false;
        options.Cookie.PersistCookie = true;

        using var server = Host.CreateTestServer(logger, options);
        var client = server.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/admin/secret");
        request.Headers.Add("Cookie", "MYAPA=somevalue");

        // Act
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(response.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task SlidingExpiration_Enabled_WithSessionCookie_DoesNotRefreshCookie()
    {
        // Arrange
        var options = BlockingOptions;
        options.Cookie.SlidingExpiration = true;
        options.Cookie.PersistCookie = false;

        using var server = Host.CreateTestServer(logger, options);
        var client = server.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/admin/secret");
        request.Headers.Add("Cookie", "MYAPA=somevalue");

        // Act
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(response.Headers.Contains("Set-Cookie"));
    }
}
