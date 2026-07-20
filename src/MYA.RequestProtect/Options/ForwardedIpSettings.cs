namespace MYA.RequestProtect.Options;

/// <summary>
/// Configuration for resolving the client IP address from a forwarded header when the
/// application is hosted behind a trusted reverse proxy or CDN (e.g. Cloudflare, Azure Front Door).
/// </summary>
public class ForwardedIpSettings
{
    public const string Key = $"{RequestProtectOptions.Key}:ForwardedIp";

    /// <summary>
    /// When true, the client IP is read from <see cref="HeaderName"/> before falling back to the
    /// transport connection IP. Only enable this when the application is guaranteed to receive traffic
    /// exclusively via the named trusted proxy; otherwise a client could spoof the header and bypass the
    /// IP whitelist. Default: false.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Name of the header carrying the originating client IP address. Defaults to Cloudflare's
    /// "cf-connecting-ip". Other proxies use different headers (e.g. Azure Front Door uses
    /// "X-Azure-ClientIP"). If the header carries a comma-separated list, the first entry is used.
    /// </summary>
    public string HeaderName { get; set; } = "cf-connecting-ip";
}
