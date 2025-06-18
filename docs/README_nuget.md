# Moriyama.RequestProtect

A flexible and powerful ASP.NET Core middleware for protecting web requests through IP whitelisting, URL pattern matching, and query string authentication.

## Features

- 🔒 Request protection through multiple authentication methods
- 🌐 IP address whitelisting
- 🔑 Query string authentication
- 🎯 URL pattern matching rules
- 🍪 Automatic cookie-based authentication after successful validation
- ⚙️ Highly configurable through appsettings.json
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
  "MYA":
  {
    "RP": {
      "Enabled": true,
      "QueryKey": "auth",
      "Code": "your_secret_code"
    }
  }
}
```

## Documentation

For complete documentation, including detailed configuration options, examples, and best practices, please visit our [GitHub Repository](https://github.com/Moriyama-Umbraco/MYA.RequestProtect).

## Support

If you encounter any issues or need support, please create an issue in our [GitHub Repository](https://github.com/Moriyama-Umbraco/MYA.RequestProtect/issues).
