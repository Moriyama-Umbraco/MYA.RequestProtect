using Xunit.Sdk;

namespace Moriyama.PreviewAuth.Tests.Extensions;

internal static class ITestExtensions
{
    public static string FileSafeTestName(this ITest? test)
    {
        var displayName = test?.TestDisplayName ?? "";
        var parenIndex = displayName.IndexOf('(');
        return parenIndex >= 0
            ? displayName[..parenIndex]
            : displayName;
    }
}
