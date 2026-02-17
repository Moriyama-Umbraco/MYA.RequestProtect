using MYA.RequestProtect.Options;
using System.Collections;

namespace MYA.RequestProtect.Tests.TestCases;

public class OptionsWithAuthRuleGroupTestCases : IEnumerable<TheoryDataRow<RequestProtectOptions, string>>
{
    /// <summary>
    /// Test case 1: Simple rule group with ANY operator - allows if any path matches
    /// Group should PASS if request matches /api or /blog
    /// </summary>
    private readonly RequestProtectOptions RuleGroupWithAnyOperator = new()
    {
        Enabled = true,
        Code = "valid_code",
        Rules = new AuthRules()
        {
            RuleGroups =
            [
                new AuthRuleGroup()
                {
                    Name = "Public Paths Group (ANY)",
                    Enabled = true,
                    RulesOperator = RuleGroupOperator.Any,
                    Rules =
                    [
                        new()
                        {
                            Name = "Allow API",
                            Pattern = "^/api/.*",
                            Enabled = true,
                            AppliesTo = AppliesTo.Path
                        },
                        new()
                        {
                            Name = "Allow Blog",
                            Pattern = "^/blog/.*",
                            Enabled = true,
                            AppliesTo = AppliesTo.Path
                        }
                    ]
                }
            ]
        }
    };

    /// <summary>
    /// Test case 2: Simple rule group with ALL operator - allows only if all conditions match
    /// Group should PASS only if BOTH path AND host match
    /// </summary>
    private readonly RequestProtectOptions RuleGroupWithAllOperator = new()
    {
        Enabled = true,
        Code = "valid_code",
        Rules = new AuthRules()
        {
            RuleGroups =
            [
                new AuthRuleGroup()
                {
                    Name = "Strict Access Group (ALL)",
                    Enabled = true,
                    RulesOperator = RuleGroupOperator.All,
                    Rules =
                    [
                        new()
                        {
                            Name = "Admin Path",
                            Pattern = "^/admin/.*",
                            Enabled = true,
                            AppliesTo = AppliesTo.Path
                        },
                        new()
                        {
                            Name = "Localhost Only",
                            Pattern = "^localhost.*",
                            Enabled = true,
                            AppliesTo = AppliesTo.Host
                        }
                    ]
                }
            ]
        }
    };

    /// <summary>
    /// Test case 3: Mixed rules and rule groups - regular rule + group with ANY
    /// Should allow if either the swagger rule OR the group (graphql/health) matches
    /// </summary>
    private readonly RequestProtectOptions MixedRulesAndGroups = new()
    {
        Enabled = true,
        Code = "valid_code",
        Rules = new AuthRules()
        {
            Rules =
            [
                new()
                {
                    Name = "Swagger Path",
                    Pattern = "^/swagger/.*",
                    Enabled = true,
                    AppliesTo = AppliesTo.Path
                }
            ],
            RuleGroups =
            [
                new AuthRuleGroup()
                {
                    Name = "Public APIs Group (ANY)",
                    Enabled = true,
                    RulesOperator = RuleGroupOperator.Any,
                    Rules =
                    [
                        new()
                        {
                            Name = "GraphQL",
                            Pattern = "^/graphql.*",
                            Enabled = true,
                            AppliesTo = AppliesTo.Path
                        },
                        new()
                        {
                            Name = "Health Check",
                            Pattern = "^/health.*",
                            Enabled = true,
                            AppliesTo = AppliesTo.Path
                        }
                    ]
                }
            ]
        }
    };

    /// <summary>
    /// Test case 4: Nested rule groups - group containing another group
    /// Outer group (ANY): inner group (ALL) OR direct rule
    /// </summary>
    private readonly RequestProtectOptions NestedRuleGroups = new()
    {
        Enabled = true,
        Code = "valid_code",
        Rules = new AuthRules()
        {
            RuleGroups =
            [
                new AuthRuleGroup()
                {
                    Name = "Outer Group (ANY)",
                    Enabled = true,
                    RulesOperator = RuleGroupOperator.Any,
                    Rules =
                    [
                        new()
                        {
                            Name = "Public Swagger",
                            Pattern = "^/swagger/.*",
                            Enabled = true,
                            AppliesTo = AppliesTo.Path
                        }
                    ],
                    RuleGroups =
                    [
                        new AuthRuleGroup()
                        {
                            Name = "Inner Strict Group (ALL)",
                            Enabled = true,
                            RulesOperator = RuleGroupOperator.All,
                            Rules =
                            [
                                new()
                                {
                                    Name = "Admin Path",
                                    Pattern = "^/admin/.*",
                                    Enabled = true,
                                    AppliesTo = AppliesTo.Path
                                },
                                new()
                                {
                                    Name = "API Query",
                                    Pattern = ".*api.*",
                                    Enabled = true,
                                    AppliesTo = AppliesTo.Query
                                }
                            ]
                        }
                    ]
                }
            ]
        }
    };

    /// <summary>
    /// Test case 5: Disabled rule group - should not affect authorization
    /// Even though group matches, it's disabled so should require auth code
    /// </summary>
    private readonly RequestProtectOptions DisabledRuleGroup = new()
    {
        Enabled = true,
        Code = "valid_code",
        Rules = new AuthRules()
        {
            RuleGroups =
            [
                new AuthRuleGroup()
                {
                    Name = "Disabled Public Group",
                    Enabled = false,
                    RulesOperator = RuleGroupOperator.Any,
                    Rules =
                    [
                        new()
                        {
                            Name = "API",
                            Pattern = "^/api/.*",
                            Enabled = true,
                            AppliesTo = AppliesTo.Path
                        }
                    ]
                }
            ]
        }
    };

    /// <summary>
    /// Test case 6: Rule group with disabled child rule - partial group evaluation
    /// Group (ANY) with one enabled and one disabled rule
    /// </summary>
    private readonly RequestProtectOptions RuleGroupWithDisabledChild = new()
    {
        Enabled = true,
        Code = "valid_code",
        Rules = new AuthRules()
        {
            RuleGroups =
            [
                new AuthRuleGroup()
                {
                    Name = "Mixed Enable Group (ANY)",
                    Enabled = true,
                    RulesOperator = RuleGroupOperator.Any,
                    Rules =
                    [
                        new()
                        {
                            Name = "Enabled API",
                            Pattern = "^/api/.*",
                            Enabled = true,
                            AppliesTo = AppliesTo.Path
                        },
                        new()
                        {
                            Name = "Disabled Admin",
                            Pattern = "^/admin/.*",
                            Enabled = false,
                            AppliesTo = AppliesTo.Path
                        }
                    ]
                }
            ]
        }
    };

    /// <summary>
    /// Test case 7: ALL operator group where only some rules match
    /// Should require auth since not ALL rules match
    /// </summary>
    private readonly RequestProtectOptions AllOperatorPartialMatch = new()
    {
        Enabled = true,
        Code = "valid_code",
        Rules = new AuthRules()
        {
            RuleGroups =
            [
                new AuthRuleGroup()
                {
                    Name = "Strict Multiple Conditions (ALL)",
                    Enabled = true,
                    RulesOperator = RuleGroupOperator.All,
                    Rules =
                    [
                        new()
                        {
                            Name = "Must be API",
                            Pattern = "^/api/.*",
                            Enabled = true,
                            AppliesTo = AppliesTo.Path
                        },
                        new()
                        {
                            Name = "Must be admin host",
                            Pattern = "^admin\\..*",
                            Enabled = true,
                            AppliesTo = AppliesTo.Host
                        },
                        new()
                        {
                            Name = "Must have version query",
                            Pattern = ".*version=2.*",
                            Enabled = true,
                            AppliesTo = AppliesTo.Query
                        }
                    ]
                }
            ]
        }
    };

    /// <summary>
    /// Test case 8: Complex mixed scenario with multiple groups and rules
    /// Public + Private zones with different operators
    /// </summary>
    private readonly RequestProtectOptions ComplexMixedScenario = new()
    {
        Enabled = true,
        Code = "valid_code",
        Rules = new AuthRules()
        {
            Rules =
            [
                new()
                {
                    Name = "Always Allow Health",
                    Pattern = "^/health.*",
                    Enabled = true,
                    AppliesTo = AppliesTo.Path
                }
            ],
            RuleGroups =
            [
                new AuthRuleGroup()
                {
                    Name = "Public APIs (ANY)",
                    Enabled = true,
                    RulesOperator = RuleGroupOperator.Any,
                    Rules =
                    [
                        new()
                        {
                            Name = "Public API v1",
                            Pattern = "^/api/v1/public/.*",
                            Enabled = true,
                            AppliesTo = AppliesTo.Path
                        },
                        new()
                        {
                            Name = "OpenAPI Docs",
                            Pattern = "^/swagger/.*",
                            Enabled = true,
                            AppliesTo = AppliesTo.Path
                        }
                    ]
                },
                new AuthRuleGroup()
                {
                    Name = "Internal Access (ALL)",
                    Enabled = true,
                    RulesOperator = RuleGroupOperator.All,
                    Rules =
                    [
                        new()
                        {
                            Name = "Internal API Path",
                            Pattern = "^/api/internal/.*",
                            Enabled = true,
                            AppliesTo = AppliesTo.Path
                        },
                        new()
                        {
                            Name = "Internal Network",
                            Pattern = "^internal\\.local.*",
                            Enabled = true,
                            AppliesTo = AppliesTo.Host
                        }
                    ]
                }
            ]
        }
    };

    /// <summary>
    /// Test case 9: Empty rule group - no child rules
    /// Should not match and require auth
    /// </summary>
    private readonly RequestProtectOptions EmptyRuleGroup = new()
    {
        Enabled = true,
        Code = "valid_code",
        Rules = new AuthRules()
        {
            RuleGroups =
            [
                new AuthRuleGroup()
                {
                    Name = "Empty Group",
                    Enabled = true,
                    RulesOperator = RuleGroupOperator.Any,
                    Rules = []
                }
            ]
        }
    };

    /// <summary>
    /// Test case 10: Multiple groups with different operators in sequence
    /// Tests evaluation order and logic combination
    /// </summary>
    private readonly RequestProtectOptions MultipleGroupsSequence = new()
    {
        Enabled = true,
        Code = "valid_code",
        Rules = new AuthRules()
        {
            RuleGroups =
            [
                new AuthRuleGroup()
                {
                    Name = "Premium Group (ANY)",
                    Enabled = true,
                    RulesOperator = RuleGroupOperator.Any,
                    Rules =
                    [
                        new()
                        {
                            Name = "Premium API",
                            Pattern = "^/premium/.*",
                            Enabled = true,
                            AppliesTo = AppliesTo.Path
                        },
                        new()
                        {
                            Name = "Enterprise Admin",
                            Pattern = "^/enterprise/.*",
                            Enabled = true,
                            AppliesTo = AppliesTo.Path
                        }
                    ]
                },
                new AuthRuleGroup()
                {
                    Name = "Standard Group (ALL)",
                    Enabled = true,
                    RulesOperator = RuleGroupOperator.All,
                    Rules =
                    [
                        new()
                        {
                            Name = "Standard API",
                            Pattern = "^/api/.*",
                            Enabled = true,
                            AppliesTo = AppliesTo.Path
                        },
                        new()
                        {
                            Name = "Staging Only",
                            Pattern = "^staging\\..*",
                            Enabled = true,
                            AppliesTo = AppliesTo.Host
                        }
                    ]
                }
            ]
        }
    };

    public IEnumerator<TheoryDataRow<RequestProtectOptions, string>> GetEnumerator()
    {
        // Test case 1: RuleGroup with ANY operator
        yield return new TheoryDataRow<RequestProtectOptions, string>(RuleGroupWithAnyOperator, "/api/users")
            { TestDisplayName = "RuleGroup_AnyOperator_APIMatch_Block" };
        yield return new TheoryDataRow<RequestProtectOptions, string>(RuleGroupWithAnyOperator, "/api/users?auth=valid_code")
            { TestDisplayName = "RuleGroup_AnyOperator_APIMatch_Allow" };
        yield return new TheoryDataRow<RequestProtectOptions, string>(RuleGroupWithAnyOperator, "/blog/post-1")
            { TestDisplayName = "RuleGroup_AnyOperator_BlogMatch_Block" };
        yield return new TheoryDataRow<RequestProtectOptions, string>(RuleGroupWithAnyOperator, "/other/path")
            { TestDisplayName = "RuleGroup_AnyOperator_NoMatch_Allow" };

        // Test case 2: RuleGroup with ALL operator
        yield return new TheoryDataRow<RequestProtectOptions, string>(RuleGroupWithAllOperator, "/admin/dashboard")
            { TestDisplayName = "RuleGroup_AllOperator_BothMatch_Block" };
        yield return new TheoryDataRow<RequestProtectOptions, string>(RuleGroupWithAllOperator, "/api/users")
            { TestDisplayName = "RuleGroup_AllOperator_HostMatchOnly_Allow" };

        // Test case 3: Mixed rules and groups
        yield return new TheoryDataRow<RequestProtectOptions, string>(MixedRulesAndGroups, "/swagger/index.html")
            { TestDisplayName = "MixedRulesGroups_SwaggerRule_Block" };
        yield return new TheoryDataRow<RequestProtectOptions, string>(MixedRulesAndGroups, "/graphql")
            { TestDisplayName = "MixedRulesGroups_GraphQLInGroup_Block" };
        yield return new TheoryDataRow<RequestProtectOptions, string>(MixedRulesAndGroups, "/health")
            { TestDisplayName = "MixedRulesGroups_HealthInGroup_Block" };

        // Test case 4: Nested rule groups
        yield return new TheoryDataRow<RequestProtectOptions, string>(NestedRuleGroups, "/admin/users?api=true")
            { TestDisplayName = "NestedRuleGroups_InnerGroupMatch_Block" };
        yield return new TheoryDataRow<RequestProtectOptions, string>(NestedRuleGroups, "/swagger/index.html")
            { TestDisplayName = "NestedRuleGroups_OuterDirectRule_Block" };
        yield return new TheoryDataRow<RequestProtectOptions, string>(NestedRuleGroups, "/other/path")
            { TestDisplayName = "NestedRuleGroups_NoMatch_Allow" };

        // Test case 5: Disabled rule group
        yield return new TheoryDataRow<RequestProtectOptions, string>(DisabledRuleGroup, "/api/users")
            { TestDisplayName = "DisabledRuleGroup_PathMatches_Allow" };

        // Test case 6: Rule group with disabled child
        yield return new TheoryDataRow<RequestProtectOptions, string>(RuleGroupWithDisabledChild, "/api/users")
            { TestDisplayName = "RuleGroupDisabledChild_EnabledMatch_Block" };
        yield return new TheoryDataRow<RequestProtectOptions, string>(RuleGroupWithDisabledChild, "/admin/dashboard")
            { TestDisplayName = "RuleGroupDisabledChild_DisabledMatch_Allow" };

        // Test case 7: ALL operator partial match
        yield return new TheoryDataRow<RequestProtectOptions, string>(AllOperatorPartialMatch, "/api/users?version=2")
            { TestDisplayName = "AllOperatorPartial_AllMatch_Allow" };
        yield return new TheoryDataRow<RequestProtectOptions, string>(AllOperatorPartialMatch, "/api/users?version=1")
            { TestDisplayName = "AllOperatorPartial_PartialMatch_Allow" };

        // Test case 8: Complex mixed scenario
        yield return new TheoryDataRow<RequestProtectOptions, string>(ComplexMixedScenario, "/health/check")
            { TestDisplayName = "ComplexMixed_HealthMatch_Block" };
        yield return new TheoryDataRow<RequestProtectOptions, string>(ComplexMixedScenario, "/api/v1/public/users")
            { TestDisplayName = "ComplexMixed_PublicAPI_Block" };
        yield return new TheoryDataRow<RequestProtectOptions, string>(ComplexMixedScenario, "/api/internal/admin")
            { TestDisplayName = "ComplexMixed_InternalHostMismatch_Allow" };

        // Test case 9: Empty rule group
        yield return new TheoryDataRow<RequestProtectOptions, string>(EmptyRuleGroup, "/any/path")
            { TestDisplayName = "EmptyRuleGroup_AnyPath_Allow" };

        // Test case 10: Multiple groups sequence
        yield return new TheoryDataRow<RequestProtectOptions, string>(MultipleGroupsSequence, "/premium/feature")
            { TestDisplayName = "MultipleGroups_PremiumMatch_Block" };
        yield return new TheoryDataRow<RequestProtectOptions, string>(MultipleGroupsSequence, "/api/data")
            { TestDisplayName = "MultipleGroups_StandardPartialMatch_Allow" };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
