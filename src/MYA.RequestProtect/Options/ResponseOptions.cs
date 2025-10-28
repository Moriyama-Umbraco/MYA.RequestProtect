using MYA.RequestProtect.Enums;

namespace MYA.RequestProtect.Options;

/// <summary>
/// Configuration for responses when authentication fails
/// </summary>
public class ResponseOptions
{
    /// <summary>
    /// Type of response to send when authentication fails (redirect, json, etc.)
    /// </summary>
    public ResponseTypes? ResponseType { get; set; }

    /// <summary>
    /// Destination URL for redirects or custom response content
    /// </summary>
    public string? Destination { get; set; }

    /// <summary>
    /// HTTP status code to return when authentication fails (default: 400)
    /// </summary>
    public int StatusCode { get; set; } = 400;
}
