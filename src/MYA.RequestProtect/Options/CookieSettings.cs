using System.ComponentModel.DataAnnotations;

namespace MYA.RequestProtect.Options;

public class CookieSettings
{
    /// <summary>
    /// Cookie expiry duration in minutes (default: 30)
    /// </summary>
    [Range(1, 525_600)]
    public int ExpiryMinutes { get; set; } = 30;

    /// <summary>
    /// When true, sets an explicit Expires header on the cookie (survives browser restart).
    /// When false, creates a session cookie (deleted on browser close).
    /// </summary>
    public bool PersistCookie { get; set; } = true;

    /// <summary>
    /// When true, and PersistCookie is also true, the auth cookie's expiry is reset to
    /// now + ExpiryMinutes on every request that carries a valid cookie, keeping an
    /// actively-browsing user authenticated indefinitely. No effect when PersistCookie
    /// is false (session cookies carry no server-tracked expiry to extend).
    /// </summary>
    public bool SlidingExpiration { get; set; } = false;
}
