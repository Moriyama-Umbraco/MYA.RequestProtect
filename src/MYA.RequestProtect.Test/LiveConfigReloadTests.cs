using Microsoft.Extensions.DependencyInjection;
using MYA.RequestProtect.Options;
using MYA.RequestProtect.Tests.Extensions;
using MYA.RequestProtect.Tests.Setup;

namespace MYA.RequestProtect.Tests;

public class LiveConfigReloadTests
{
    private readonly TestLogger logger = new();

    // NOTE: A completely empty AuthRules() (no rules, no groups, no whitelist, no headers) is
    // treated as fully protected by RequestProtectMiddleware.AuthNotNeeded (it short-circuits to
    // "auth needed" rather than evaluating an empty rule set as "nothing matched"). To exercise
    // "unprotected because no configured rule matches this request" per the documented inverted-logic
    // behavior, this uses one enabled rule that does not match the test path.
    private static RequestProtectOptions UnprotectedOptions => new()
    {
        Enabled = true,
        Code = "valid_code",
        Rules = new AuthRules
        {
            Rules =
            [
                new()
                {
                    Name = "Does Not Match",
                    Pattern = "^/never-matches$",
                    Enabled = true,
                    AppliesTo = AppliesTo.Path
                }
            ]
        }
    };

    private static RequestProtectOptions BlockAllOptions => new()
    {
        Enabled = true,
        Code = "valid_code",
        Rules = new AuthRules
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

    private static RequestProtectOptions WhitelistOptions(string ip) => new()
    {
        Enabled = true,
        Code = "valid_code",
        Rules = new AuthRules
        {
            IpWhitelist = [ip],
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
    public async Task RuleChange_ViaConfigReload_TakesEffectWithoutRestart()
    {
        // Arrange
        using var server = Host.CreateTestServer(logger, UnprotectedOptions);
        var client = server.CreateClient();

        var before = await client.GetAsync("/admin/secret", TestContext.Current.CancellationToken);
        Assert.Equal(System.Net.HttpStatusCode.OK, before.StatusCode);

        // Act: push a new rule set that now blocks everything
        var configProvider = server.Services.GetRequiredService<ReloadableConfigurationProvider>();
        configProvider.Update(Host.FlattenOptionsForTest(BlockAllOptions));

        var after = await client.GetAsync("/admin/secret", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, after.StatusCode);
    }

    [Fact]
    public async Task WhitelistChange_ViaConfigReload_TakesEffectWithoutRestart()
    {
        // Arrange: whitelist a different IP than the test's remote IP, so the request is blocked
        using var server = Host.CreateTestServer(logger, WhitelistOptions("198.51.100.1"));
        var client = server.CreateClient();

        var before = await client.GetAsync("/admin/secret", TestContext.Current.CancellationToken);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, before.StatusCode);

        // Act: push a config update that whitelists the test's actual remote IP
        var configProvider = server.Services.GetRequiredService<ReloadableConfigurationProvider>();
        configProvider.Update(Host.FlattenOptionsForTest(WhitelistOptions(Host.DefaultRemoteIP)));

        var after = await client.GetAsync("/admin/secret", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, after.StatusCode);
    }
}
