namespace MYA.RequestProtect.Options;

/// <summary>
/// Operator for combining rule results in a group
/// </summary>
public enum RuleGroupOperator
{
    /// <summary>
    /// All rules in the group must pass
    /// </summary>
    All = 0,

    /// <summary>
    /// At least one rule in the group must pass
    /// </summary>
    Any = 1
}
