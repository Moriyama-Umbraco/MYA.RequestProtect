using MYA.RequestProtect.Options;
using MYA.RequestProtect.Tests.Extensions;
using MYA.RequestProtect.Tests.Setup;

namespace MYA.RequestProtect.Tests;

public class MiddlewareShortCircuitTests
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
    public async Task DisabledMiddleware_AnyPath_PassesThrough()
    {
        // Arrange
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
        var response = await client.GetAsync("/admin/secret", TestContext.Current.CancellationToken);

        // Assert
        await Verify(response);
    }

    [Fact]
    public async Task ValidCookie_SkipsAuth_PassesThrough()
    {
        // Arrange
        using var server = Host.CreateTestServer(logger, BlockingOptions);
        var client = server.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/admin/secret");
        request.Headers.Add("Cookie", "MYAPA=somevalue");

        // Act
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        await Verify(response);
    }

    [Fact]
    public async Task EmptyCookie_DoesNotSkipAuth_Returns400()
    {
        // Arrange
        using var server = Host.CreateTestServer(logger, BlockingOptions);
        var client = server.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/admin/secret");
        request.Headers.Add("Cookie", "MYAPA=");

        // Act
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        await Verify(response);
    }

    [Fact]
    public async Task ValidAuth_SetsCookie_WithCorrectAttributes()
    {
        // Arrange
        using var server = Host.CreateTestServer(logger, BlockingOptions);
        var client = server.CreateClient();

        // Act
        var response = await client.GetAsync("/blog/post?auth=valid_code", TestContext.Current.CancellationToken);

        // Assert
        await Verify(response);
    }
}
