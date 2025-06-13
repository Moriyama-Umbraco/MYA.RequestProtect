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

## Installation

Install the package via NuGet:

```bash
dotnet add package Moriyama.RequestProtect
```

Or using the Package Manager Console:

```powershell
Install-Package Moriyama.RequestProtect
```

## Quick Start

1. Add the middleware to your application in `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add the middleware to your services
builder.Services.AddRequestProtect();

var app = builder.Build();

// Use the middleware in your request pipeline
app.UseMiddleware<RequestProtectMiddleware>();
```

2. Configure the middleware in your `appsettings.json`:

```json
{
  "MYA:RP": {
    "Enabled": true,
    "QueryKey": "auth",
    "Code": "your_secret_code",
    "Rules": {
      "IPWhitelist": ["127.0.0.1", "::1"],
      "AuthRules": [
        {
          "Pattern": "/api/*",
          "AppliesTo": "Path",
          "RequiresQueryString": true
        }
      ]
    }
  }
}
```

## Configuration Options

| Option | Description | Default |
|--------|-------------|---------|
| `Enabled` | Enable/disable the middleware | `false` |
| `QueryKey` | The query string parameter name for authentication | `"auth"` |
| `Code` | The secret code that must be provided in the query string | Required |
| `Rules` | Collection of authentication rules | Empty collection |

### Authentication Rules

Each rule in the `AuthRules` collection can specify:

- `Pattern`: URL pattern to match (supports wildcards)
- `AppliesTo`: What part of the request to match against (`Path`, `Host`, etc.)
- `RequiresQueryString`: Whether the rule requires query string authentication

## Examples

### Protecting API Routes

```json
{
  "MYA:RP": {
    "Enabled": true,
    "QueryKey": "auth",
    "Code": "secret123",
    "Rules": {
      "AuthRules": [
        {
          "Pattern": "/api/*",
          "AppliesTo": "Path",
          "RequiresQueryString": true
        }
      ]
    }
  }
}
```

This configuration will require query string authentication for all routes starting with `/api/`.

### IP Whitelisting

```json
{
  "MYA:RP": {
    "Enabled": true,
    "Rules": {
      "IPWhitelist": ["192.168.1.100", "10.0.0.*"]
    }
  }
}
```

This configuration will only allow requests from the specified IP addresses.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

If you encounter any issues or need support, please create an issue in the GitHub repository.
