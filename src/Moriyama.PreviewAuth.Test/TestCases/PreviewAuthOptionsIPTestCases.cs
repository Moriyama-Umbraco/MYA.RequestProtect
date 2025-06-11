using Moriyama.PreviewAuth.Options;
using Moriyama.PreviewAuth.Tests.Setup;
using System.Collections;

namespace Moriyama.PreviewAuth.Tests.TestCases;

public class PreviewAuthOptionsWithIPTestCases : IEnumerable<TheoryDataRow<PreviewAuthOptions, string>>
{

    public IEnumerator<TheoryDataRow<PreviewAuthOptions, string>> GetEnumerator()
    {
        yield return new TheoryDataRow<PreviewAuthOptions, string>(IpWhiteListTest, "/") 
        { TestDisplayName = "Valid_IP_Returns_200" };
        yield return new TheoryDataRow<PreviewAuthOptions, string>(MultipleIpsTest, "/contact")
        { TestDisplayName = "Multiple_IPs_In_Whitelist" };

        yield return new TheoryDataRow<PreviewAuthOptions, string>(IpWhiteListTest, "/complex/nested/path")
        { TestDisplayName = "Complex_URL_Path_With_Valid_IP" };

        yield return new TheoryDataRow<PreviewAuthOptions, string>(BlockByIpTest, "/")
        { TestDisplayName = "InValid_IP_Returns_400" };
    }

    private readonly PreviewAuthOptions IpWhiteListTest = new()
    {
        Enabled = true,
        Code = "valid_code",
        Rules = new AuthRules()
        {
            IpWhitelist = [Host.RemoteIP]
        }
    };

    private readonly PreviewAuthOptions MultipleIpsTest = new()
    {
        Enabled = true,
        Code = "valid_code",
        Rules = new AuthRules()
        {
            IpWhitelist = [Host.RemoteIP, "127.0.0.1", "::1"]
        }
    };

    private readonly PreviewAuthOptions BlockByIpTest = new()
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
