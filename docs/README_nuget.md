# Moriyama.RequestProtect

A flexible and powerful ASP.NET Core middleware for protecting web requests through IP whitelisting, URL pattern matching, and query string authentication.

## Features

- 🔒 Request protection through multiple authentication methods
- 🌐 IP address whitelisting (including support for CIDR notation)
- 📨 Header authorisation
- 🔑 Query string authentication
- 🎯 URL pattern matching rules
- 🍪 Automatic cookie-based authentication after successful validation
- ⚙️ Highly configurable through appsettings.json
- 🔄 Live config reload — changes to appsettings.json or environment variables take effect without an app restart
- 📝 Comprehensive logging support

## Quick Start

1. Install the package via NuGet:

```bash
dotnet add package Moriyama.RequestProtect
```

2. Add the middleware to your application in `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add the middleware to your services
builder.Services.AddRequestProtect();

var app = builder.Build();

// Use the middleware in your request pipeline
app.UseMiddleware<RequestProtectMiddleware>();
```

3. Add basic configuration in your `appsettings.json`:

```json
{
  "MYA:RP": {
    "Enabled": true,
    "QueryKey": "auth",
    "Code": "your_secret_code"
  }
}
```

## Cookie Options

```json
{
  "MYA:RP": {
    "Enabled": true,
    "QueryKey": "auth",
    "Code": "your_secret_code",
    "Cookie": {
      "ExpiryMinutes": 30,
      "PersistCookie": true,
      "SlidingExpiration": true
    }
  }
}
```

With `SlidingExpiration: true`, an already-authenticated visitor's cookie expiry resets to `now + ExpiryMinutes` on every request, so an actively-browsing user is never logged out mid-session. Has no effect when `PersistCookie` is `false` (session cookies have no server-tracked expiry to extend). Note: this sets a `Set-Cookie` header on every authenticated response, which prevents response/output caching and CDN caching for that traffic.

## Live Config Reload

Config is bound via `IOptionsMonitor<RequestProtectOptions>`, so editing `appsettings.json` (or an environment variable, if your host reloads config from it) takes effect immediately — no restart required. If an edit produces invalid config (e.g. a bad regex `Pattern`), the error is logged and the middleware keeps running with its last valid configuration.

## Documentation

For complete documentation, including detailed configuration options, examples, and best practices, please visit our [GitHub Repository](https://github.com/moriyama-umbraco/MYA.RequestProtect).

## Support

If you encounter any issues or need support, please create an issue in our [GitHub Repository](https://github.com/moriyama-umbraco/MYA.RequestProtect/issues).
