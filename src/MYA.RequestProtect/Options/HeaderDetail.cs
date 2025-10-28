namespace MYA.RequestProtect.Options;

/// <summary>
/// Represents an HTTP header used for authentication
/// </summary>
public class HeaderDetail
{
    /// <summary>
    /// The name of the HTTP header to check
    /// </summary>
    public required string Header { get; set; }
    
    /// <summary>
    /// The expected value of the header (if null, only checks for presence)
    /// </summary>
    public string? Value { get; set; }
}
