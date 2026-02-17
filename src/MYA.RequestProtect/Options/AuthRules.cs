namespace MYA.RequestProtect.Options;

/// <summary>
/// Authentication rules configuration
/// </summary>
public class AuthRules
{
	public const string Key = $"{RequestProtectOptions.Key}:AuthRules";

    /// <summary>
    /// List of IP addresses that are whitelisted and bypass authentication
    /// </summary>
    public string[]? IpWhitelist { get; set; }	

    /// <summary>
    /// List of HTTP headers to check for authentication
    /// </summary>
    public HeaderDetail[]? Headers { get; set; }

    /// <summary>
    /// Collection of custom authentication rules with regex patterns
    /// </summary>
    public AuthRule[]? Rules { get; set; } = [];

    /// <summary>
    /// Collection of rule groups for hierarchical rule evaluation
    /// </summary>
    public AuthRuleGroup[]? RuleGroups { get; set; }

    /// <summary>
    /// Logical operator for combining top-level rules and rule groups (default: Any preserves existing behaviour)
    /// </summary>
    public RuleGroupOperator RulesOperator { get; set; } = RuleGroupOperator.Any;
}
