using MYA.RequestProtect.Options;
using MYA.RequestProtect.Tests.Setup;
using System.Collections;

namespace MYA.RequestProtect.Tests.TestCases;

public class RequestProtectOptionsWithIPTestCases : IEnumerable<TheoryDataRow<RequestProtectOptions, string>>
{

    public IEnumerator<TheoryDataRow<RequestProtectOptions, string>> GetEnumerator()
    {
        yield return new TheoryDataRow<RequestProtectOptions, string>(IpWhiteListTest, "/") 
        { TestDisplayName = "Valid_IP_Returns_200" };
        yield return new TheoryDataRow<RequestProtectOptions, string>(MultipleIpsTest, "/contact")
        { TestDisplayName = "Multiple_IPs_In_Whitelist" };

        yield return new TheoryDataRow<RequestProtectOptions, string>(IpWhiteListTest, "/complex/nested/path")
        { TestDisplayName = "Complex_URL_Path_With_Valid_IP" };

        yield return new TheoryDataRow<RequestProtectOptions, string>(BlockByIpTest, "/")
        { TestDisplayName = "InValid_IP_Returns_400" };
    }

    private readonly RequestProtectOptions IpWhiteListTest = new()
    {
        Enabled = true,
        Code = "valid_code",
        Rules = new AuthRules()
        {
            IpWhitelist = [Host.RemoteIP]
        }
    };

    private readonly RequestProtectOptions MultipleIpsTest = new()
    {
        Enabled = true,
        Code = "valid_code",
        Rules = new AuthRules()
        {
            IpWhitelist = [Host.RemoteIP, "127.0.0.1", "::1"]
        }
    };

    private readonly RequestProtectOptions BlockByIpTest = new()
    {
        Enabled = true,
        Code = "valid_code",
        Rules = new AuthRules()
        {
            IpWhitelist = ["127.0.0.1", "::1"]
        }
    };

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
