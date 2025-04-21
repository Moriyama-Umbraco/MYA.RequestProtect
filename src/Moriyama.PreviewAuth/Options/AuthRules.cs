namespace Moriyama.PreviewAuth.Options;

public class AuthRules
{
	public const string Key = $"{PreviewAuthOptions.Key}:AuthRules";

	public string[]? IpWhitelist { get; set; }
	public AuthRule[]? Rules { get; set; } = [];
}
