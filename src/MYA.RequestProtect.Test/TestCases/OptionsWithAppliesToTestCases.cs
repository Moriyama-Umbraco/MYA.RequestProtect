using MYA.RequestProtect.Options;
using System.Collections;

namespace MYA.RequestProtect.Tests.TestCases;

public class OptionsWithAppliesToTestCases : IEnumerable<TheoryDataRow<RequestProtectOptions, string>>
{
    private readonly RequestProtectOptions QueryOnlyRule = new()
    {
        Enabled = true,
        Code = "valid_code",
        Rules = new AuthRules()
        {
            Rules =
            [
                new()
                {
                    Name = "Query Debug Rule",
                    Pattern = ".*debug.*",
                    Enabled = true,
                    AppliesTo = AppliesTo.Query
                }
            ]
        }
    };

    /// <summary>
    /// PathAndQuery is the default AppliesTo value.
    /// Documents bug: it falls through to Path-only matching in the switch default case.
    /// </summary>
    private readonly RequestProtectOptions PathAndQueryRule = new()
    {
        Enabled = true,
        Code = "valid_code",
        Rules = new AuthRules()
        {
            Rules =
            [
                new()
                {
                    Name = "PathAndQuery Debug Rule",
                    Pattern = ".*debug.*",
                    Enabled = true,
                    AppliesTo = AppliesTo.PathAndQuery
                }
            ]
        }
    };

    public IEnumerator<TheoryDataRow<RequestProtectOptions, string>> GetEnumerator()
    {
        // Query-only tests
        yield return new TheoryDataRow<RequestProtectOptions, string>(QueryOnlyRule, "/page?debug=true")
            { TestDisplayName = "QueryOnly_MatchingParam_Block" };
        yield return new TheoryDataRow<RequestProtectOptions, string>(QueryOnlyRule, "/page")
            { TestDisplayName = "QueryOnly_NoQueryString_Allow" };
        yield return new TheoryDataRow<RequestProtectOptions, string>(QueryOnlyRule, "/page?mode=test")
            { TestDisplayName = "QueryOnly_DifferentParam_Allow" };
        yield return new TheoryDataRow<RequestProtectOptions, string>(QueryOnlyRule, "/page?debug=true&auth=valid_code")
            { TestDisplayName = "QueryOnly_WithAuthCode_Allow" };

        // PathAndQuery tests - documents that PathAndQuery only checks Path (bug)
        yield return new TheoryDataRow<RequestProtectOptions, string>(PathAndQueryRule, "/page?debug=true")
            { TestDisplayName = "PathAndQuery_MatchesPathOnly_QueryIgnored" };
        yield return new TheoryDataRow<RequestProtectOptions, string>(PathAndQueryRule, "/debug/info")
            { TestDisplayName = "PathAndQuery_MatchesPath_Block" };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
