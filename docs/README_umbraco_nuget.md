# Moriyama.RequestProtect.Umbraco

Seamlessly integrate request protection into your Umbraco website. This package provides a simple way to protect your Umbraco website using IP whitelisting, URL pattern matching, and query string authentication.

## Features

- 🔒 Protect your Umbraco backoffice and content
- 🌐 IP whitelist specific sections of your site, like /preview/
- 📨 Use Header authorisation to allow third party systems/other websites to bypass blocks
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
  "MYA":
  {
    "RP": {
      "Enabled": true,
      "QueryKey": "auth",
      "Code": "your_secret_code",
      "Rules": {
        "IpWhitelist": ["127.0.0.1"],
        "Rules": [
          {
            "Name": "Protect Preview",
            "Pattern": "^/preview/",
            "AppliesTo": "Path",
            "Enabled": true
          }
        ]
      }
    }
  }
}
```

2. The package will automatically register itself with Umbraco's composition engine. No additional code is required!

## Common Use Cases

### Protecting Preview URLs

```json
{
  "MYA":
  {
    "RP": {
      "Enabled": true,
      "QueryKey": "preview",
      "Code": "your_secret_code",
      "Rules": {
        "Rules": [
          {
            "Name": "Protect Preview",
            "Pattern": "^/preview/",
            "AppliesTo": "Path",
            "Enabled": true
          }
        ]
      }
    }
  }
}
```

### Protecting Staging Environments

```json
{
  "MYA":
  {
    "RP": {
      "Enabled": true,
      "Code": "your_secret_code",
      "Rules": {
        "IpWhitelist": [
          "office.ip.address",
          "vpn.ip.address"
        ],
        "Rules": [
          {
            "Name": "Staging Host",
            "Pattern": "^staging\\.website\\.com$",
            "AppliesTo": "Host",
            "Enabled": true
          }
        ]
      }
    }
  }
}
```

### Protecting Umbraco Backoffice

```json
{
  "MYA":
  {
    "RP": {
      "Enabled": true,
      "Code": "your_secret_code",
      "Rules": {
        "IpWhitelist": ["office.ip.range.*"],
        "Rules": [
          {
            "Name": "Protect Backoffice",
            "Pattern": "^/umbraco/",
            "AppliesTo": "Path",
            "Enabled": true
          }
        ]
      }
    }
  }
}
```

## Documentation

For complete documentation of the core package features, please visit our [GitHub Repository](https://github.com/Moriyama-Umbraco/MYA.RequestProtect).

## Support

If you encounter any issues or need support, please create an issue in our [GitHub Repository](https://github.com/Moriyama-Umbraco/MYA.RequestProtect/issues).
