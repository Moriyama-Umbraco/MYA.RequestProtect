# Moriyama.RequestProtect.Umbraco

Seamlessly integrate request protection into your Umbraco website. This package provides a simple way to protect your Umbraco website using IP whitelisting, URL pattern matching, and query string authentication.

## Features

- 🔒 Protect your Umbraco backoffice and content
- 🌐 IP whitelist specific sections of your site, like /preview/
- 🔑 Enable preview links with secure query string authentication
- 🎯 Pattern matching for Umbraco URLs and routes
- 🍪 Automatic cookie-based authentication after validation
- ⚙️ Easy configuration through appsettings.json
- 📝 Integration with Umbraco's logging

## Installation

```bash
dotnet add package Moriyama.RequestProtect.Umbraco
```

## Quick Start

1. Add the following to your `appsettings.json`:

```json
{
  "MYA:RP": {
    "Enabled": true,
    "QueryKey": "auth",
    "Code": "your_secret_code",
    "Rules": {
      "IPWhitelist": ["127.0.0.1"],
      "AuthRules": [
        {
          "Pattern": "/preview/*",
          "AppliesTo": "Path",
          "RequiresQueryString": true
        }
      ]
    }
  }
}
```

2. The package will automatically register itself with Umbraco's composition engine. No additional code is required!

## Common Use Cases

### Protecting Preview URLs

```json
{
  "MYA:RP": {
    "Enabled": true,
    "QueryKey": "preview",
    "Code": "your_secret_code",
    "Rules": {
      "AuthRules": [
        {
          "Pattern": "/preview/*",
          "AppliesTo": "Path",
          "RequiresQueryString": true
        }
      ]
    }
  }
}
```

### Protecting Staging Environments

```json
{
  "MYA:RP": {
    "Enabled": true,
    "Rules": {
      "IPWhitelist": [
        "office.ip.address",
        "vpn.ip.address"
      ],
      "AuthRules": [
        {
          "Pattern": "staging.website.com",
          "AppliesTo": "Host"
        }
      ]
    }
  }
}
```

### Protecting Umbraco Backoffice

```json
{
  "MYA:RP": {
    "Enabled": true,
    "Rules": {
      "IPWhitelist": ["office.ip.range.*"],
      "AuthRules": [
        {
          "Pattern": "/umbraco/*",
          "AppliesTo": "Path"
        }
      ]
    }
  }
}
```

## Documentation

For complete documentation of the core package features, please visit our [GitHub Repository](https://github.com/moriyama/MYA.RequestProtect).

## Support

If you encounter any issues or need support, please create an issue in our [GitHub Repository](https://github.com/moriyama/MYA.RequestProtect/issues).
