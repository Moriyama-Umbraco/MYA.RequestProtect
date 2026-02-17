using Xunit.Sdk;

namespace MYA.RequestProtect.Tests.Extensions;

internal static class ITestExtensions
{
    public static string FileSafeTestName(this ITest? test)
    {
        var displayName = test?.TestDisplayName ?? "";
        var parenIndex = displayName.IndexOf('(');
        var testName = parenIndex >= 0
            ? displayName[..parenIndex]
            : displayName;

        var className = test?.TestCase?.TestMethod?.TestClass?.TestClassName;
        if (className is not null)
        {
            var lastDot = className.LastIndexOf('.');
            if (lastDot >= 0)
                className = className[(lastDot + 1)..];

            return $"{className}.{testName}";
        }

        return testName;
    }
}
