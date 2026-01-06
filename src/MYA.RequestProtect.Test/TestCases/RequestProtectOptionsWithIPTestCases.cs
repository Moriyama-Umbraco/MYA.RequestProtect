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
        yield return new TheoryDataRow<RequestProtectOptions, string>(CheckAllWhitelistEntriesTest, "/")
        { TestDisplayName = "Should_Check_All_Whitelist_Entries" };
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
            IpWhitelist = ["192.168.1.0/24", Host.RemoteIP]
        }
    };

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
