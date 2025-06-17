using MYA.RequestProtect.Options;
using System.Collections;

namespace MYA.RequestProtect.Tests.TestCases;

public class RequestProtectOptionsWithRegexTestCases : IEnumerable<TheoryDataRow<RequestProtectOptions, string>>
{
    private readonly RequestProtectOptions SingleAuthRuleTest = new()
    {
        Enabled = true,
        Code = "valid_code",
        Rules = new AuthRules()
        {
            Rules =
            [
                new()
                {
                    Name = "Blog Rule",
                    Pattern = "^/blog/.*",
                    Enabled = true,
                    AppliesTo = AppliesTo.Path
                }
            ]
        }
    };

    private readonly RequestProtectOptions MultipleAuthRulesTest = new()
    {
        Enabled = true,
        Code = "valid_code",
        Rules = new AuthRules()
        {
            Rules =
            [
                new()
                {
                    Name = "API Block",
                    Pattern = "^/api/.*",
                    Enabled = true,
                    AppliesTo = AppliesTo.Path
                },
                new()
                {
                    Name = "Admin Access",
                    Pattern = "^/admin/.*",
                    Enabled = true,
                    AppliesTo = AppliesTo.Path
                }
            ]
        }
    };

    public IEnumerator<TheoryDataRow<RequestProtectOptions, string>> GetEnumerator()
    {  
        yield return new TheoryDataRow<RequestProtectOptions, string>(SingleAuthRuleTest, "/blog/post-1") 
            { TestDisplayName = "Single_AuthRule_Pattern_Match_NoQueryString_Block" };
        yield return new TheoryDataRow<RequestProtectOptions, string>(SingleAuthRuleTest, "/blog/post-1?auth=valid_code")
        { TestDisplayName = "Single_AuthRule_Pattern_Match_WithQueryString_Allow" };

        yield return new TheoryDataRow<RequestProtectOptions, string>(MultipleAuthRulesTest, "/api/users?auth=valid_code") 
            { TestDisplayName = "Multiple_AuthRules_API_Pattern" };

        yield return new TheoryDataRow<RequestProtectOptions, string>(MultipleAuthRulesTest, "/admin/dashboard?auth=valid_code") 
            { TestDisplayName = "Multiple_AuthRules_Admin_Pattern" };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
