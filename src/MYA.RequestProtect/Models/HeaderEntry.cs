using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;

namespace MYA.RequestProtect.Models;

internal readonly struct HeaderEntry
{
    public readonly string Name;
    public readonly string? Value;
    public readonly bool IsWildcard;

    public HeaderEntry(string name, string? value)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Value = value;
        IsWildcard = value == "*";
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Matches(IHeaderDictionary headers)
    {
        if (!headers.TryGetValue(Name, out var headerValue))
            return false;

        if (IsWildcard)
            return true;

        return !string.IsNullOrEmpty(Value) &&
             headerValue.ToString().Equals(Value, StringComparison.Ordinal);
    }
}
