using System.Diagnostics.CodeAnalysis;

namespace Moriyama.PreviewAuth.Options;

public class AuthRule
{
    public required string Name { get; set; }

    [StringSyntax(StringSyntaxAttribute.Regex)]
    public required string Pattern { get; set; }

    public bool Enabled { get; set; }

    public AppliesTo AppliesTo { get; set; } = AppliesTo.PathAndQuery;

    public override string ToString() => $"Rule ({Name}) {(Enabled ? 'Y' : 'N')}: {AppliesTo} - {Pattern}";
}

