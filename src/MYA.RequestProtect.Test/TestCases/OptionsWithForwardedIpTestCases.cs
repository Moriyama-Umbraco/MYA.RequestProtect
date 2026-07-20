using MYA.RequestProtect.Options;
using System.Collections;

namespace MYA.RequestProtect.Tests.TestCases;

/// <summary>
/// Test cases covering client IP resolution from a forwarded header (e.g. Cloudflare's
/// "cf-connecting-ip") when hosted behind a trusted reverse proxy. Columns are
/// (options, url, forwardedHeaderName, forwardedHeaderValue). The test harness always sets the
/// transport connection IP to <see cref="Setup.Host.DefaultRemoteIP"/>.
/// </summary>
public class OptionsWithForwardedIpTestCases : IEnumerable<TheoryDataRow<RequestProtectOptions, string, string?, string?>>
{
    public IEnumerator<TheoryDataRow<RequestProtectOptions, string, string?, string?>> GetEnumerator()
    {
        // Header enabled + carries a whitelisted IP -> allowed, even though the connection IP is not whitelisted.
        yield return new TheoryDataRow<RequestProtectOptions, string, string?, string?>(
            ForwardedIpEnabled, "/", "cf-connecting-ip", "4.180.158.195")
        { TestDisplayName = "ForwardedIp_Enabled_Whitelisted_Returns_200" };

        // Header enabled + carries a non-whitelisted IP -> blocked.
        yield return new TheoryDataRow<RequestProtectOptions, string, string?, string?>(
            ForwardedIpEnabled, "/", "cf-connecting-ip", "8.8.8.8")
        { TestDisplayName = "ForwardedIp_Enabled_NotWhitelisted_Returns_400" };

        // Header disabled but a spoofed whitelisted IP is sent -> ignored, connection IP used -> blocked.
        yield return new TheoryDataRow<RequestProtectOptions, string, string?, string?>(
            ForwardedIpDisabled, "/", "cf-connecting-ip", "4.180.158.195")
        { TestDisplayName = "ForwardedIp_Disabled_Header_Ignored_Returns_400" };

        // Header enabled but absent -> falls back to the (whitelisted) connection IP -> allowed.
        yield return new TheoryDataRow<RequestProtectOptions, string, string?, string?>(
            ForwardedIpEnabledConnectionWhitelisted, "/", null, null)
        { TestDisplayName = "ForwardedIp_Enabled_Header_Absent_FallsBack_Returns_200" };

        // Header enabled + carries an invalid IP -> falls back to connection IP -> blocked.
        yield return new TheoryDataRow<RequestProtectOptions, string, string?, string?>(
            ForwardedIpEnabled, "/", "cf-connecting-ip", "not-an-ip")
        { TestDisplayName = "ForwardedIp_Enabled_Invalid_Header_FallsBack_Returns_400" };

        // Custom (non-Cloudflare) header name is honoured -> allowed.
        yield return new TheoryDataRow<RequestProtectOptions, string, string?, string?>(
            ForwardedIpCustomHeader, "/", "X-Azure-ClientIP", "4.180.158.195")
        { TestDisplayName = "ForwardedIp_CustomHeader_Returns_200" };
    }

    private readonly RequestProtectOptions ForwardedIpEnabled = new()
    {
        Enabled = true,
        Code = "mya",
        QueryKey = "mya",
        ForwardedIp = new ForwardedIpSettings { Enabled = true },
        Rules = new AuthRules()
        {
            IpWhitelist = ["4.180.158.192/28"]
        }
    };

    private readonly RequestProtectOptions ForwardedIpDisabled = new()
    {
        Enabled = true,
        Code = "mya",
        QueryKey = "mya",
        ForwardedIp = new ForwardedIpSettings { Enabled = false },
        Rules = new AuthRules()
        {
            IpWhitelist = ["4.180.158.192/28"]
        }
    };

    private readonly RequestProtectOptions ForwardedIpEnabledConnectionWhitelisted = new()
    {
        Enabled = true,
        Code = "mya",
        QueryKey = "mya",
        ForwardedIp = new ForwardedIpSettings { Enabled = true },
        Rules = new AuthRules()
        {
            IpWhitelist = [Setup.Host.DefaultRemoteIP]
        }
    };

    private readonly RequestProtectOptions ForwardedIpCustomHeader = new()
    {
        Enabled = true,
        Code = "mya",
        QueryKey = "mya",
        ForwardedIp = new ForwardedIpSettings { Enabled = true, HeaderName = "X-Azure-ClientIP" },
        Rules = new AuthRules()
        {
            IpWhitelist = ["4.180.158.192/28"]
        }
    };

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
