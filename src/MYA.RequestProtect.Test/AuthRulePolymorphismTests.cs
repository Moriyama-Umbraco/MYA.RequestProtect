using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MYA.RequestProtect.Options;
using System.Text;

namespace MYA.RequestProtect.Tests;

public class AuthRuleConfigBindingTests
{
    [Fact]
    public void ConfigBinding_SimpleRules_SurviveRoundTrip()
    {
        // Arrange - simulate appsettings.json with Rules only
        var json = """
        {
          "MYA": {
            "RP": {
              "Enabled": true,
              "Code": "secret",
              "Rules": {
                "Rules": [
                  { "Name": "Swagger", "Pattern": "^/swagger/.*", "Enabled": true, "AppliesTo": "Path" },
                  { "Name": "API", "Pattern": "^/api/.*", "Enabled": false, "AppliesTo": "PathAndQuery" }
                ]
              }
            }
          }
        }
        """;

        var options = BindOptionsFromJson(json);

        // Assert
        Assert.NotNull(options.Rules.Rules);
        Assert.Equal(2, options.Rules.Rules.Length);

        Assert.Equal("Swagger", options.Rules.Rules[0].Name);
        Assert.Equal("^/swagger/.*", options.Rules.Rules[0].Pattern);
        Assert.True(options.Rules.Rules[0].Enabled);
        Assert.Equal(AppliesTo.Path, options.Rules.Rules[0].AppliesTo);

        Assert.Equal("API", options.Rules.Rules[1].Name);
        Assert.False(options.Rules.Rules[1].Enabled);
    }

    [Fact]
    public void ConfigBinding_RuleGroups_SurviveRoundTrip()
    {
        // Arrange - simulate appsettings.json with RuleGroups
        var json = """
        {
          "MYA": {
            "RP": {
              "Enabled": true,
              "Code": "secret",
              "Rules": {
                "RulesOperator": "Any",
                "RuleGroups": [
                  {
                    "Name": "Public APIs",
                    "Enabled": true,
                    "RulesOperator": "Any",
                    "Rules": [
                      { "Name": "API v1", "Pattern": "^/api/v1.*", "Enabled": true, "AppliesTo": "Path" }
                    ]
                  }
                ]
              }
            }
          }
        }
        """;

        var options = BindOptionsFromJson(json);

        // Assert
        Assert.NotNull(options.Rules.RuleGroups);
        Assert.Single(options.Rules.RuleGroups);
        Assert.Equal(RuleGroupOperator.Any, options.Rules.RulesOperator);

        var group = options.Rules.RuleGroups[0];
        Assert.Equal("Public APIs", group.Name);
        Assert.True(group.Enabled);
        Assert.Equal(RuleGroupOperator.Any, group.RulesOperator);
        Assert.NotNull(group.Rules);
        Assert.Single(group.Rules);
        Assert.Equal("API v1", group.Rules[0].Name);
        Assert.Equal("^/api/v1.*", group.Rules[0].Pattern);
    }

    [Fact]
    public void ConfigBinding_NestedRuleGroups_SurviveRoundTrip()
    {
        // Arrange - simulate appsettings.json with nested RuleGroups
        var json = """
        {
          "MYA": {
            "RP": {
              "Enabled": true,
              "Code": "secret",
              "Rules": {
                "RuleGroups": [
                  {
                    "Name": "Outer",
                    "Enabled": true,
                    "RulesOperator": "Any",
                    "Rules": [
                      { "Name": "Direct Rule", "Pattern": "^/public/.*", "Enabled": true, "AppliesTo": "Path" }
                    ],
                    "RuleGroups": [
                      {
                        "Name": "Inner",
                        "Enabled": true,
                        "RulesOperator": "All",
                        "Rules": [
                          { "Name": "Admin Path", "Pattern": "^/admin/.*", "Enabled": true, "AppliesTo": "Path" }
                        ]
                      }
                    ]
                  }
                ]
              }
            }
          }
        }
        """;

        var options = BindOptionsFromJson(json);

        // Assert
        Assert.NotNull(options.Rules.RuleGroups);
        var outer = options.Rules.RuleGroups[0];
        Assert.Equal("Outer", outer.Name);
        Assert.Equal(RuleGroupOperator.Any, outer.RulesOperator);

        Assert.NotNull(outer.Rules);
        Assert.Single(outer.Rules);
        Assert.Equal("Direct Rule", outer.Rules[0].Name);

        Assert.NotNull(outer.RuleGroups);
        Assert.Single(outer.RuleGroups);
        var inner = outer.RuleGroups[0];
        Assert.Equal("Inner", inner.Name);
        Assert.Equal(RuleGroupOperator.All, inner.RulesOperator);
        Assert.NotNull(inner.Rules);
        Assert.Single(inner.Rules);
        Assert.Equal("Admin Path", inner.Rules[0].Name);
    }

    [Fact]
    public void ConfigBinding_MixedRulesAndGroups_SurviveRoundTrip()
    {
        // Arrange - simulate appsettings.json with both Rules and RuleGroups
        var json = """
        {
          "MYA": {
            "RP": {
              "Enabled": true,
              "Code": "secret",
              "Rules": {
                "RulesOperator": "Any",
                "IpWhitelist": ["127.0.0.1"],
                "Rules": [
                  { "Name": "Swagger", "Pattern": "^/swagger/.*", "Enabled": true, "AppliesTo": "Path" }
                ],
                "RuleGroups": [
                  {
                    "Name": "Public APIs",
                    "Enabled": true,
                    "RulesOperator": "Any",
                    "Rules": [
                      { "Name": "API v1", "Pattern": "^/api/v1.*", "Enabled": true, "AppliesTo": "Path" }
                    ],
                    "RuleGroups": []
                  }
                ]
              }
            }
          }
        }
        """;

        var options = BindOptionsFromJson(json);

        // Assert - both Rules and RuleGroups survive
        Assert.NotNull(options.Rules.Rules);
        Assert.Single(options.Rules.Rules);
        Assert.Equal("Swagger", options.Rules.Rules[0].Name);

        Assert.NotNull(options.Rules.RuleGroups);
        Assert.Single(options.Rules.RuleGroups);
        Assert.Equal("Public APIs", options.Rules.RuleGroups[0].Name);

        Assert.NotNull(options.Rules.IpWhitelist);
        Assert.Single(options.Rules.IpWhitelist);
        Assert.Equal("127.0.0.1", options.Rules.IpWhitelist[0]);

        Assert.Equal(RuleGroupOperator.Any, options.Rules.RulesOperator);
    }

    [Fact]
    public void ConfigBinding_DefaultRulesOperator_IsAny()
    {
        // Arrange - no RulesOperator specified, should default to Any
        var json = """
        {
          "MYA": {
            "RP": {
              "Enabled": true,
              "Code": "secret",
              "Rules": {
                "Rules": [
                  { "Name": "Test", "Pattern": "^/test.*", "Enabled": true, "AppliesTo": "Path" }
                ]
              }
            }
          }
        }
        """;

        var options = BindOptionsFromJson(json);

        // Assert - default operator is Any (preserves existing behaviour)
        Assert.Equal(RuleGroupOperator.Any, options.Rules.RulesOperator);
    }

    private static RequestProtectOptions BindOptionsFromJson(string json)
    {
        var config = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(json)))
            .Build();

        var services = new ServiceCollection();
        services.AddOptions<RequestProtectOptions>()
            .BindConfiguration(RequestProtectOptions.Key);
        services.AddSingleton<IConfiguration>(config);

        var sp = services.BuildServiceProvider();
        return sp.GetRequiredService<IOptions<RequestProtectOptions>>().Value;
    }
}
