using MYA.RequestProtect.Options;
using System.Collections;

namespace MYA.RequestProtect.Tests.TestCases;

public class OptionsWithPathEdgeCaseTestCases : IEnumerable<TheoryDataRow<RequestProtectOptions, string>>
{
    private readonly RequestProtectOptions AdminPathRule = new()
    {
        Enabled = true,
        Code = "valid_code",
        Rules = new AuthRules()
        {
            Rules =
            [
                new()
                {
                    Name = "Admin Path Rule",
                    Pattern = @"^/admin/.*",
                    Enabled = true,
                    AppliesTo = AppliesTo.Path
                }
            ]
        }
    };

    public IEnumerator<TheoryDataRow<RequestProtectOptions, string>> GetEnumerator()
    {
        yield return new TheoryDataRow<RequestProtectOptions, string>(AdminPathRule, "/admin")
            { TestDisplayName = "PathEdge_NoTrailingSlash_Allow" };
        yield return new TheoryDataRow<RequestProtectOptions, string>(AdminPathRule, "//admin/page")
            { TestDisplayName = "PathEdge_DoubleSlash_Allow" };
        yield return new TheoryDataRow<RequestProtectOptions, string>(AdminPathRule, "/admin/%2e%2e/secret")
            { TestDisplayName = "PathEdge_EncodedCharacters" };
        yield return new TheoryDataRow<RequestProtectOptions, string>(AdminPathRule, "/Admin/page")
            { TestDisplayName = "PathEdge_CaseSensitive_Allow" };
        yield return new TheoryDataRow<RequestProtectOptions, string>(AdminPathRule, "/")
            { TestDisplayName = "PathEdge_RootPath_Allow" };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
