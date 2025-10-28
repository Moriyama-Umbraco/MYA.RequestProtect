namespace MYA.RequestProtect.Options;

/// <summary>
/// Authentication rules configuration
/// </summary>
public class AuthRules
{
	public const string Key = $"{RequestProtectOptions.Key}:AuthRules";

    /// <summary>
    /// List of IP addresses that are whitelisted and bypass authentication
    /// </summary>
    public string[]? IpWhitelist { get; set; }	

    /// <summary>
    /// List of HTTP headers to check for authentication
    /// </summary>
    public HeaderDetail[]? Headers { get; set; }

    /// <summary>
    /// Collection of custom authentication rules with regex patterns
    /// </summary>
    public AuthRule[]? Rules { get; set; } = [];
}
