using System.Diagnostics.CodeAnalysis;

namespace MYA.RequestProtect.Options;

/// <summary>
/// Represents a custom authentication rule with regex pattern matching
/// </summary>
public class AuthRule
{
    /// <summary>
    /// The name of the authentication rule
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The regex pattern to match against the request
    /// </summary>
    [StringSyntax(StringSyntaxAttribute.Regex)]
    public required string Pattern { get; set; }

    /// <summary>
    /// Whether this rule is enabled
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// What part of the request this rule applies to (default: PathAndQuery)
    /// </summary>
    public AppliesTo AppliesTo { get; set; } = AppliesTo.PathAndQuery;

    public override string ToString() => $"Rule ({Name}) {(Enabled ? 'Y' : 'N')}: {AppliesTo} - {Pattern}";
}

