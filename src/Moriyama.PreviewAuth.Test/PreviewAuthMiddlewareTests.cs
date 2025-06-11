using Moriyama.PreviewAuth.Logging;
using Moriyama.PreviewAuth.Options;
using Moriyama.PreviewAuth.Tests.Extensions;
using Moriyama.PreviewAuth.Tests.Setup;
using Moriyama.PreviewAuth.Tests.TestCases;

namespace Moriyama.PreviewAuth.Tests;

public class PreviewAuthMiddlewareTests
{

    private readonly TestLogger logger = new();


    [Fact]
    public async Task TestMiddleware_DoesNothingIFNoConfiguration()
    {
        // Arrange
        var server = Host.CreateTestServer(logger);
        var client = server.CreateClient();

        // Act
        var response = await client.GetAsync("/?auth=valid_code", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equivalent(200, (int)response.StatusCode);
        Assert.False(response.Headers.Contains("Set-Cookie"));
    }

    [Theory]
    [InlineData("?auth=valid_code")]
    [InlineData(null)]
    [InlineData("?auth=invalid")]
    
    public async Task QueryString_Rules(string? queryString)
    {
        // Arrange
        using var server = Host.CreateTestServer(logger, new PreviewAuthOptions
        {
            Enabled = true,
            Code = "valid_code",
        });
        var client = server.CreateClient();

        // Act
        var response = await client.GetAsync($"/{queryString}", TestContext.Current.CancellationToken);
        
        logger.WasLogMethodCalled(nameof(PreviewAuthMiddlewareLogs.LogRuleValidation));

        // Assert
        await Verify(response);
    }

    [Theory()]
    [ClassData(typeof(PreviewAuthOptionsWithRegexTestCases))]
    public async Task Auth_RegexRule_Tests(PreviewAuthOptions options, string url)
    {
        // Arrange
       using var server = Host.CreateTestServer(logger, options);
        var client = server.CreateClient();
        
        // Act
        var response = await client.GetAsync(url, TestContext.Current.CancellationToken);



       
        // Assert
        await Verify(response)
            .UseFileName(TestContext.Current.Test.FileSafeTestName());
    }

    [Theory()]
    [ClassData(typeof(PreviewAuthOptionsWithIPTestCases))]
    public async Task Auth_IPRule_Tests(PreviewAuthOptions options, string url)
    {
        // Arrange
        using var server = Host.CreateTestServer(logger, options);
        var client = server.CreateClient();

        // Act
        var response = await client.GetAsync(url, TestContext.Current.CancellationToken);




        // Assert
        await Verify(response)
            .UseFileName(TestContext.Current.Test.FileSafeTestName());
    }
}
