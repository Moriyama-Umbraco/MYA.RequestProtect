using System.ComponentModel.DataAnnotations;

namespace MYA.RequestProtect.Options;

public class CookieSettings
{
    /// <summary>
    /// Cookie expiry duration in minutes (default: 30)
    /// </summary>
    [Range(1, int.MaxValue)]
    public int ExpiryMinutes { get; set; } = 30;

    /// <summary>
    /// When true, sets an explicit Expires header on the cookie (survives browser restart).
    /// When false, creates a session cookie (deleted on browser close).
    /// </summary>
    public bool PersistCookie { get; set; } = true;
}
