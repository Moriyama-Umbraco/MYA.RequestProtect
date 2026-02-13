using MYA.RequestProtect.Options;
using System.Collections;

namespace MYA.RequestProtect.Tests.TestCases;

public class OptionsWithHeaderTestCases : IEnumerable<TheoryDataRow<RequestProtectOptions, string>>
{
    public IEnumerator<TheoryDataRow<RequestProtectOptions, string>> GetEnumerator()
    {
        yield return new TheoryDataRow<RequestProtectOptions, string>(SingleHeaderTest, "/")
        {
            TestDisplayName = "Valid_Header_Returns_200"
        };

        yield return new TheoryDataRow<RequestProtectOptions, string>(WildcardHeaderTest, "/contact")
        {
            TestDisplayName = "Wildcard_Header_Returns_200"
        };

        yield return new TheoryDataRow<RequestProtectOptions, string>(MultipleHeaderOptionsTest, "/contact")
        {
            TestDisplayName = "Multiple_Headers_In_List"
        };

        yield return new TheoryDataRow<RequestProtectOptions, string>(InvalidHeaderOptionsTest, "/")
        {
            TestDisplayName = "Invalid_Header_Returns_400"
        };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private readonly RequestProtectOptions SingleHeaderTest = new()
    {
        Enabled = true,
        Code = "valid_code",
        Rules = new AuthRules()
        {
            Headers = [new() { Header = "singleHeader", Value = "singleHeader"}]
        }
    };

    private readonly RequestProtectOptions WildcardHeaderTest = new()
    {
        Enabled = true,
        Code = "valid_code",
        Rules = new AuthRules()
        {
            Headers = [new() { Header = "wildHeader", Value = "*" }]
        }
    };

    private readonly RequestProtectOptions MultipleHeaderOptionsTest = new()
    {
        Enabled = true,
        Code = "valid_code",
        Rules = new AuthRules()
        {
            Headers = [new() { Header = "headerOne", Value = "1" }, new() { Header = "headerTwo", Value = "2" }]
        }
    };

    private readonly RequestProtectOptions InvalidHeaderOptionsTest = new()
    {
        Enabled = true,
        Code = "valid_code",
        Rules = new AuthRules()
        {
            Headers = [new() { Header = "invalidHeader", Value = "invalidHeader" }]
        }
    };

    
}
