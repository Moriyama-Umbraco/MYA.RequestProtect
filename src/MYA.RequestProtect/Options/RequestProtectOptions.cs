using System.ComponentModel.DataAnnotations;

namespace MYA.RequestProtect.Options;

public class RequestProtectOptions : IValidatableObject
{
    public const string Key = "MYA:RP";

    /// <summary>
    /// Enables or Disables MYA.RequestProtect
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Query string key value to check, defaults to 'auth'
    /// </summary>
    public string QueryKey { get; set; } = "auth";

    /// <summary>
    /// Authorisation code for the QueryString
    /// </summary>
    public required string Code { get; set; }

    /// <summary>
    /// Authentication rules configuration including IP whitelist, headers, and custom rules
    /// </summary>
    public AuthRules Rules { get; set; } = new();

    /// <summary>
    /// Response configuration for unauthorized requests including response type, destination, and status code
    /// </summary>
    public ResponseOptions Response { get; set; } = new();

    /// <summary>
    /// Cookie configuration for authentication cookie behavior
    /// </summary>
    public CookieSettings Cookie { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(Cookie, new ValidationContext(Cookie), results, validateAllProperties: true);
        return results;
    }
}
