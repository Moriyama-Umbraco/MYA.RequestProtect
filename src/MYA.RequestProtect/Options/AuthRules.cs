namespace MYA.RequestProtect.Options;

public class AuthRules
{
	public const string Key = $"{RequestProtectOptions.Key}:AuthRules";

    public string[]? IpWhitelist { get; set; }	

    public HeaderDetail[]? Headers { get; set; }

    public AuthRule[]? Rules { get; set; } = [];
}
