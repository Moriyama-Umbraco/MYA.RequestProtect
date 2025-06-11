namespace Moriyama.PreviewAuth.Options;

public class PreviewAuthOptions
{
    public const string Key = "MYA:PA";

    public bool Enabled { get; set; }

    public string QueryKey { get; set; } = "auth";

    public required string Code { get; set; }

    public AuthRules Rules { get; set; } = new();
}
