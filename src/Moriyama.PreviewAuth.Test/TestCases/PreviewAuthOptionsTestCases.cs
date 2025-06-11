using Moriyama.PreviewAuth.Options;
using System.Collections;

namespace Moriyama.PreviewAuth.Tests.TestCases;

public class PreviewAuthOptionsWithRegexTestCases : IEnumerable<TheoryDataRow<PreviewAuthOptions, string>>
{
    private readonly PreviewAuthOptions SingleAuthRuleTest = new()
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

    private readonly PreviewAuthOptions MultipleAuthRulesTest = new()
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

    public IEnumerator<TheoryDataRow<PreviewAuthOptions, string>> GetEnumerator()
    {  
        yield return new TheoryDataRow<PreviewAuthOptions, string>(SingleAuthRuleTest, "/blog/post-1") 
            { TestDisplayName = "Single_AuthRule_Pattern_Match_NoQueryString_Block" };
        yield return new TheoryDataRow<PreviewAuthOptions, string>(SingleAuthRuleTest, "/blog/post-1?auth=valid_code")
        { TestDisplayName = "Single_AuthRule_Pattern_Match_WithQueryString_Allow" };

        yield return new TheoryDataRow<PreviewAuthOptions, string>(MultipleAuthRulesTest, "/api/users?auth=valid_code") 
            { TestDisplayName = "Multiple_AuthRules_API_Pattern" };

        yield return new TheoryDataRow<PreviewAuthOptions, string>(MultipleAuthRulesTest, "/admin/dashboard?auth=valid_code") 
            { TestDisplayName = "Multiple_AuthRules_Admin_Pattern" };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}