using MYA.RequestProtect.Logging;
using MYA.RequestProtect.Options;
using MYA.RequestProtect.Tests.Extensions;
using MYA.RequestProtect.Tests.Setup;
using MYA.RequestProtect.Tests.TestCases;

namespace MYA.RequestProtect.Tests;

public class RequestProtectMiddlewareTests
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
        using var server = Host.CreateTestServer(logger, new RequestProtectOptions
        {
            Enabled = true,
            Code = "valid_code",
        });
        var client = server.CreateClient();

        // Act
        var response = await client.GetAsync($"/{queryString}", TestContext.Current.CancellationToken);
        
        logger.WasLogMethodCalled(nameof(RequestProtectMiddlewareLogs.LogRuleValidation));

        // Assert
        await Verify(response);
    }

    [Theory()]
    [ClassData(typeof(RequestProtectOptionsWithRegexTestCases))]
    public async Task Auth_RegexRule_Tests(RequestProtectOptions options, string url)
    {
        // Arrange
       using var server = Host.CreateTestServer(logger, options, new Uri("https://myHost.localhost"));
        var client = server.CreateClient();
        
        // Act
        var response = await client.GetAsync(url, TestContext.Current.CancellationToken);



       
        // Assert
        await Verify(response)
            .UseFileName(TestContext.Current.Test.FileSafeTestName());
    }

    [Theory()]
    [ClassData(typeof(RequestProtectOptionsWithIPTestCases))]
    public async Task Auth_IPRule_Tests(RequestProtectOptions options, string url)
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
    [ClassData(typeof(RequestProtectOptionsWithHeaderTestCases))]
    public async Task Auth_HeaderRule_Tests(RequestProtectOptions options, string url)
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
    [ClassData(typeof(RequestProtectOptionsWithAuthRuleGroupTestCases))]
    public async Task Auth_RuleGroup_Tests(RequestProtectOptions options, string url)
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
