using MYA.RequestProtect.Options;
using MYA.RequestProtect.Tests.Extensions;
using MYA.RequestProtect.Tests.Setup;
using MYA.RequestProtect.Tests.TestCases;

namespace MYA.RequestProtect.Tests;

public class ResponseTypeTests
{
    private readonly TestLogger logger = new();

    [Theory]
    [ClassData(typeof(OptionsWithResponseTypeTestCases))]
    public async Task Auth_ResponseType_Tests(RequestProtectOptions options, string url, string? webRootPath)
    {
        // Arrange
        string? resolvedWebRootPath = webRootPath is not null
            ? Path.Combine(AppContext.BaseDirectory, webRootPath)
            : null;

        using var server = Host.CreateTestServer(logger, options, webRootPath: resolvedWebRootPath);
        var client = server.CreateClient();

        // Act
        var response = await client.GetAsync(url, TestContext.Current.CancellationToken);

        // Assert
        await Verify(response)
            .UseFileName(TestContext.Current.Test.FileSafeTestName());
    }
}
