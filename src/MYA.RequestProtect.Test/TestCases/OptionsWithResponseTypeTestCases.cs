using MYA.RequestProtect.Enums;
using MYA.RequestProtect.Options;
using System.Collections;

namespace MYA.RequestProtect.Tests.TestCases;

public class OptionsWithResponseTypeTestCases : IEnumerable<TheoryDataRow<RequestProtectOptions, string, string?>>
{
    private static AuthRules BlockAllRules => new()
    {
        Rules =
        [
            new()
            {
                Name = "Block All",
                Pattern = ".*",
                Enabled = true,
                AppliesTo = AppliesTo.Path
            }
        ]
    };

    private readonly RequestProtectOptions RedirectAbsoluteUrl = new()
    {
        Enabled = true,
        Code = "valid_code",
        Rules = BlockAllRules,
        Response = new ResponseOptions
        {
            ResponseType = ResponseTypes.Redirect,
            Destination = "https://example.com/login"
        }
    };

    private readonly RequestProtectOptions RedirectRelativeUrl = new()
    {
        Enabled = true,
        Code = "valid_code",
        Rules = BlockAllRules,
        Response = new ResponseOptions
        {
            ResponseType = ResponseTypes.Redirect,
            Destination = "/login"
        }
    };

    private readonly RequestProtectOptions RedirectSamePathLoop = new()
    {
        Enabled = true,
        Code = "valid_code",
        Rules = BlockAllRules,
        Response = new ResponseOptions
        {
            ResponseType = ResponseTypes.Redirect,
            Destination = "/admin/page"
        }
    };

    private readonly RequestProtectOptions RedirectInvalidUri = new()
    {
        Enabled = true,
        Code = "valid_code",
        Rules = BlockAllRules,
        Response = new ResponseOptions
        {
            ResponseType = ResponseTypes.Redirect,
            Destination = ":::invalid"
        }
    };

    private readonly RequestProtectOptions RedirectEmptyDestination = new()
    {
        Enabled = true,
        Code = "valid_code",
        Rules = BlockAllRules,
        Response = new ResponseOptions
        {
            ResponseType = ResponseTypes.Redirect,
            Destination = ""
        }
    };

    private readonly RequestProtectOptions StaticFileHtml = new()
    {
        Enabled = true,
        Code = "valid_code",
        Rules = BlockAllRules,
        Response = new ResponseOptions
        {
            ResponseType = ResponseTypes.StaticFile,
            Destination = "error.html",
            MimeType = "text/html"
        }
    };

    private readonly RequestProtectOptions StaticFileMissing = new()
    {
        Enabled = true,
        Code = "valid_code",
        Rules = BlockAllRules,
        Response = new ResponseOptions
        {
            ResponseType = ResponseTypes.StaticFile,
            Destination = "nonexistent.html",
            MimeType = "text/html"
        }
    };

    private readonly RequestProtectOptions StaticFileJson = new()
    {
        Enabled = true,
        Code = "valid_code",
        Rules = BlockAllRules,
        Response = new ResponseOptions
        {
            ResponseType = ResponseTypes.StaticFile,
            Destination = "data.json",
            MimeType = "application/json"
        }
    };

    private readonly RequestProtectOptions DefaultStatusCode403 = new()
    {
        Enabled = true,
        Code = "valid_code",
        Rules = BlockAllRules,
        Response = new ResponseOptions
        {
            StatusCode = 403
        }
    };

    private readonly RequestProtectOptions DefaultStatusCode401 = new()
    {
        Enabled = true,
        Code = "valid_code",
        Rules = BlockAllRules,
        Response = new ResponseOptions
        {
            StatusCode = 401
        }
    };

    public IEnumerator<TheoryDataRow<RequestProtectOptions, string, string?>> GetEnumerator()
    {
        // Redirect tests
        yield return new TheoryDataRow<RequestProtectOptions, string, string?>(RedirectAbsoluteUrl, "/admin/page", null)
            { TestDisplayName = "Redirect_ValidAbsoluteUrl_Returns302" };
        yield return new TheoryDataRow<RequestProtectOptions, string, string?>(RedirectRelativeUrl, "/admin/page", null)
            { TestDisplayName = "Redirect_ValidRelativeUrl" };
        yield return new TheoryDataRow<RequestProtectOptions, string, string?>(RedirectSamePathLoop, "/admin/page", null)
            { TestDisplayName = "Redirect_SamePathAsRequest_FallsBackToDefault" };
        yield return new TheoryDataRow<RequestProtectOptions, string, string?>(RedirectInvalidUri, "/admin/page", null)
            { TestDisplayName = "Redirect_InvalidUri_FallsBackToDefault" };
        yield return new TheoryDataRow<RequestProtectOptions, string, string?>(RedirectEmptyDestination, "/admin/page", null)
            { TestDisplayName = "Redirect_EmptyDestination_FallsBackToDefault" };

        // Static file tests - webRootPath will be resolved in the test method
        yield return new TheoryDataRow<RequestProtectOptions, string, string?>(StaticFileHtml, "/admin/page", "wwwroot")
            { TestDisplayName = "StaticFile_ValidFile_ServesContent" };
        yield return new TheoryDataRow<RequestProtectOptions, string, string?>(StaticFileMissing, "/admin/page", "wwwroot")
            { TestDisplayName = "StaticFile_MissingFile_FallsBackToDefault" };
        yield return new TheoryDataRow<RequestProtectOptions, string, string?>(StaticFileJson, "/admin/page", "wwwroot")
            { TestDisplayName = "StaticFile_CustomMimeType" };

        // Default with custom status codes
        yield return new TheoryDataRow<RequestProtectOptions, string, string?>(DefaultStatusCode403, "/admin/page", null)
            { TestDisplayName = "Default_CustomStatusCode_403" };
        yield return new TheoryDataRow<RequestProtectOptions, string, string?>(DefaultStatusCode401, "/admin/page", null)
            { TestDisplayName = "Default_CustomStatusCode_401" };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
