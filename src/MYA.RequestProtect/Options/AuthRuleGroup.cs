namespace MYA.RequestProtect.Options;

/// <summary>
/// Represents a group of authentication rules combined with a logical operator
/// </summary>
public class AuthRuleGroup
{
    /// <summary>
    /// The name of the rule group
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Whether this rule group is enabled
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Logical operator for combining rule results (All/Any)
    /// </summary>
    public RuleGroupOperator RulesOperator { get; set; } = RuleGroupOperator.All;

    /// <summary>
    /// Collection of leaf rules in this group
    /// </summary>
    public AuthRule[]? Rules { get; set; }

    /// <summary>
    /// Collection of nested rule groups (recursive)
    /// </summary>
    public AuthRuleGroup[]? RuleGroups { get; set; }

    public override string ToString() =>
        $"RuleGroup ({Name}) {(Enabled ? 'Y' : 'N')}: {RulesOperator} - {Rules?.Length ?? 0} rules, {RuleGroups?.Length ?? 0} groups";
}
