namespace MYA.RequestProtect.Options;

public class RequestProtectOptions
{
    public const string Key = "MYA:RP";

    public bool Enabled { get; set; }

    public string QueryKey { get; set; } = "auth";

    public required string Code { get; set; }

    public AuthRules Rules { get; set; } = new();
}
