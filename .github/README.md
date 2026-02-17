# Moriyama.RequestProtect

[![Downloads](https://img.shields.io/nuget/dt/Moriyama.RequestProtect?color=cc9900)](https://www.nuget.org/packages/Moriyama.RequestProtect/)
[![NuGet](https://img.shields.io/nuget/vpre/Moriyama.RequestProtect?color=0273B3)](https://www.nuget.org/packages/Moriyama.RequestProtect)
[![GitHub license](https://img.shields.io/github/license/Moriyama-Umbraco/MYA.RequestProtect?color=8AB803)](LICENSE)

A flexible and powerful ASP.NET Core middleware for protecting web requests through IP whitelisting, URL pattern matching, and query string authentication.

## Features

- 🔒 Request protection through multiple authentication methods
- 🌐 IP address whitelisting
- 📨 Header authorisation
- 🔑 Query string authentication
- 🎯 URL pattern matching rules
- 🍪 Automatic cookie-based authentication after successful validation
- ⚙️ Highly configurable through appsettings.json
- 📝 Comprehensive logging support

<img src="../docs/logo.svg" alt="Moriyama.RequestProtect Logo" width="128" height="128">

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
  "MYA":
  {
    "RP": {
      "Enabled": true,
      "QueryKey": "auth",
      "Code": "your_secret_code",
      "Rules": {
        "IpWhitelist": ["127.0.0.1", "::1"],
        "Rules": [
          {
            "Name": "Protect API",
            "Pattern": "^/api/",
            "AppliesTo": "Path",
            "Enabled": true
          }
        ]
      }
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
| `Rules` | Authentication rules configuration (see below) | Empty |
| `Response` | Response configuration for unauthorized requests (see below) | 400 status |
| `Cookie` | Cookie configuration for auth cookie behavior (see below) | 30-min persistent |

### Authentication Rules

Each rule in the `Rules` collection can specify:

- `Name`: A name for the rule (required)
- `Pattern`: Regex pattern to match against the request (required)
- `AppliesTo`: What part of the request to match against — `PathAndQuery` (default), `Path`, `Host`, or `Query`
- `Enabled`: Whether the rule is active

Rules use **inverted logic**: if any enabled rule matches the request, authentication **is** required. If no rules match, the request passes through unprotected.

### Protecting API Routes

```json
{
  "MYA":
  {
    "RP": {
      "Enabled": true,
      "QueryKey": "auth",
      "Code": "secret123",
      "Rules": {
        "Rules": [
          {
            "Name": "Protect API",
            "Pattern": "^/api/",
            "AppliesTo": "Path",
            "Enabled": true
          }
        ]
      }
    }
  }
}
```

This configuration will require query string authentication for all routes starting with `/api/`.

### IP Whitelisting

```json
{
  "MYA":
  {
    "RP": {
      "Enabled": true,
      "Rules": {
        "IPWhitelist": ["192.168.1.100", "10.0.0.*"]
      }
    }
  }
}
```

This configuration will only allow requests from the specified IP addresses.

### Header Authorisation

In some situations (Azure for example) IP White listing can be excessive, this feature allows for checking that a specific header exists. It can "just" exist (wild card value) or it can be limited to a specific value.

```json
{
  "MYA":
  {
    "RP": {
      "Enabled": true,
      "Rules": {
        "Headers": [
          {
            "Header": "myHeader",
            "Value": "someSecureValue"
          },
          {
            "Header": "wildCardHeader",
            "Value": "*"
          }
        ]
      }
    }
  }
}
```

### Rule Groups

Rules can be organised into hierarchical groups with `All` or `Any` operators to create complex matching logic:

```json
{
  "MYA":
  {
    "RP": {
      "Enabled": true,
      "Code": "secret123",
      "Rules": {
        "RulesOperator": "Any",
        "RuleGroups": [
          {
            "Name": "Staging Protection",
            "Enabled": true,
            "RulesOperator": "All",
            "Rules": [
              {
                "Name": "Staging Host",
                "Pattern": "^staging\\.",
                "AppliesTo": "Host",
                "Enabled": true
              },
              {
                "Name": "Admin Path",
                "Pattern": "^/admin/",
                "AppliesTo": "Path",
                "Enabled": true
              }
            ]
          }
        ]
      }
    }
  }
}
```

This configuration requires authentication only when **both** the host starts with `staging.` **and** the path starts with `/admin/`. Rule groups support recursive nesting via the `RuleGroups` property.

### Response Options

Control what happens when a request fails authentication:

```json
{
  "MYA":
  {
    "RP": {
      "Enabled": true,
      "Code": "secret123",
      "Response": {
        "ResponseType": "Redirect",
        "Destination": "https://example.com/unauthorized",
        "StatusCode": 302
      }
    }
  }
}
```

| Option | Description | Default |
|--------|-------------|---------|
| `ResponseType` | `Default` (status code only), `StaticFile`, or `Redirect` | `Default` |
| `Destination` | File path for StaticFile or URL for Redirect | — |
| `StatusCode` | HTTP status code returned | `400` |
| `MimeType` | Content type for StaticFile responses | `"text/html"` |

### Cookie Settings

Configure the authentication cookie that is set after successful validation:

```json
{
  "MYA":
  {
    "RP": {
      "Enabled": true,
      "Code": "secret123",
      "Cookie": {
        "ExpiryMinutes": 60,
        "PersistCookie": true
      }
    }
  }
}
```

| Option | Description | Default |
|--------|-------------|---------|
| `ExpiryMinutes` | Cookie expiry duration in minutes (1–525,600) | `30` |
| `PersistCookie` | When `true`, sets an explicit expiry (survives browser restart). When `false`, creates a session cookie (deleted on browser close). | `true` |

### Further Examples

For further examples, please check out our [Examples](../docs/examples.md) documentation.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

If you encounter any issues or need support, please create an issue in the GitHub repository.
