using MYA.RequestProtect.Options;
using MYA.RequestProtect.Tests.Setup;
using System.Collections;

namespace MYA.RequestProtect.Tests.TestCases;

public class OptionsWithIPTestCases : IEnumerable<TheoryDataRow<RequestProtectOptions, string, string?>>
{
    public IEnumerator<TheoryDataRow<RequestProtectOptions, string, string?>> GetEnumerator()
    {
        yield return new TheoryDataRow<RequestProtectOptions, string, string?>(IpWhiteListTest, "/", null)   
        { TestDisplayName = "Valid_IP_Returns_200" };
        yield return new TheoryDataRow<RequestProtectOptions, string, string?>(MultipleIpsTest, "/contact", null)
        { TestDisplayName = "Multiple_IPs_In_Whitelist" };
        yield return new TheoryDataRow<RequestProtectOptions, string, string?>(IpWhiteListTest, "/complex/nested/path", null)
        { TestDisplayName = "Complex_URL_Path_With_Valid_IP" };
        yield return new TheoryDataRow<RequestProtectOptions, string, string?>(BlockByIpTest, "/", null)
        { TestDisplayName = "InValid_IP_Returns_400" };
        yield return new TheoryDataRow<RequestProtectOptions, string, string?>(CheckAllWhitelistEntriesTest, "/", null)
        { TestDisplayName = "Should_Check_All_Whitelist_Entries" };
        yield return new TheoryDataRow<RequestProtectOptions, string, string?>(UmbracoCloudIpTest, "/", "4.180.158.195")
        { TestDisplayName = "Umbraco_Cloud_IP_Test" };
        yield return new TheoryDataRow<RequestProtectOptions, string, string?>(UmbracoCloudIpTest, "/", null)
        { TestDisplayName = "Umbraco_Cloud_IP_Test_Outside_Of_Range" };
    }

    private readonly RequestProtectOptions IpWhiteListTest = new()
    {
        Enabled = true,
        Code = "valid_code",
        Rules = new AuthRules()
        {
            IpWhitelist = [Host.DefaultRemoteIP]
        }
    };

    private readonly RequestProtectOptions MultipleIpsTest = new()
    {
        Enabled = true,
        Code = "valid_code",
        Rules = new AuthRules()
        {
            IpWhitelist = ["203.0.113.32/28", "127.0.0.1", "::1"]
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

    // This test demonstrates that the middleware should check all entries in the whitelist
    private readonly RequestProtectOptions CheckAllWhitelistEntriesTest = new()
    {
        Enabled = true,
        Code = "valid_code",
        Rules = new AuthRules()
        {
            // First entry won't match, but second entry should match and allow access
            IpWhitelist = ["192.168.1.0/24", Host.DefaultRemoteIP]
        }
    };
    private readonly RequestProtectOptions UmbracoCloudIpTest = new()
    {
        Enabled = true,
        Code = "mya",
        QueryKey = "mya",
        Rules = new AuthRules()
        {
            IpWhitelist = [
                "4.180.158.192/28",
                "4.180.157.208/28",
                "4.197.15.208/28",
                "20.220.219.208/28",
                "20.68.233.144/28",
                "4.227.135.208/28"
            ],
            RuleGroups =
            [
                new AuthRuleGroup()
                {
                    Name = "Host Block",
                    Enabled = true,
                    Rules = [
                        new AuthRule()
                        {
                            Name = "Block all hosts",
                            Enabled = true,
                            Pattern = "(.*)",
                            AppliesTo = AppliesTo.Host
                        }
                    ]
                }
            ]
        }
    };

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
