# Examples of various rules

## Example 1: Full site block

```json
{
  "MYA":
  { 
    "RP": {
      "Enabled": true,
      "Code": "My-Auth-Code"
    }
  }
}
```
This config would mean that `?auth=My-Auth-Code` would be needed at the end of the first request in order to access the site.

## Example 2: Full site block, changed the querystring key

```json
{
  "MYA":
  { 
    "RP": {
      "Enabled": true,
      "QueryKey": "my-query-key",
      "Code": "My-Auth-Code"
    }
  }
}
```
This config would mean that `?my-query-key=My-Auth-Code` would be needed at the end of the first request in order to access the site.

## Example 3: Only block '/secret/' and child paths from being accessed without the code

```json
{
  "MYA":
  {
    "RP": {
      "Enabled": true,
      "Code": "My-Auth-Code",
      "Rules": {
        "Rules": [
          {
            "Name": "Secret Area",
            "Pattern": "^/secret/",
            "AppliesTo": "Path",
            "Enabled": true
          }
        ]
      }
    }
  }
}
```
This config would mean that `/my-path` would be accessible, but `/secret/area` would not be unless the auth code query string was applied

## Example 4: Block any request not on a specific host name, or in the whitelisted IPs

```json
{
  "MYA":
  {
    "RP": {
      "Enabled": true,
      "Code": "My-Auth-Code",
      "Rules": {
        "IpWhitelist": [
          "122.122.122.0/24",
          "86.86.86.85"
        ],
        "Rules": [
          {
            "Name": "Block Non-Allowed Hosts",
            "Pattern": "^(?!my\\.allowed\\.host\\.com).*$",
            "AppliesTo": "Host",
            "Enabled": true
          }
        ]
      }
    }
  }
}
```
This example blocks any request unless it comes in on the host `my.allowed.host.com` without the query string, or if the IP address of the client falls in the CIDR range `122.122.122.0/24` or is `86.86.86.85`.

## Example 5: Respond with a Static File
```json
{
  "MYA":
  {
    "RP": {
      "Enabled": true,
      "Code": "My-Auth-Code",
      "Rules": { "<Your rules here>": "" },
      "Response": {
        "ResponseType": "StaticFile",
        "Destination": "error.html",
        "StatusCode": 200,
        "MimeType": "text/html"
      }
    }
  }
}
```
This example would respond with the contents of `error.html` file with a status code of 200 and a mime type of `text/html` when the request does not have the correct auth code.

## Example 6: Redirect unauthorised requests
```json
{
  "MYA":
  {
    "RP": {
      "Enabled": true,
      "Code": "My-Auth-Code",
      "Response": {
        "ResponseType": "Redirect",
        "Destination": "https://example.com/unauthorized",
        "StatusCode": 302
      }
    }
  }
}
```
This example would redirect unauthorised requests to `https://example.com/unauthorized` with a 302 status code.

## Example 7: Configure cookie expiry and session cookies
```json
{
  "MYA":
  {
    "RP": {
      "Enabled": true,
      "Code": "My-Auth-Code",
      "Cookie": {
        "ExpiryMinutes": 60,
        "PersistCookie": true
      }
    }
  }
}
```
This example sets the authentication cookie to expire after 60 minutes. Set `PersistCookie` to `false` to create a session cookie that is deleted when the browser closes.

## Example 8: Hierarchical rule groups with All/Any operators
```json
{
  "MYA":
  {
    "RP": {
      "Enabled": true,
      "Code": "My-Auth-Code",
      "Rules": {
        "RulesOperator": "Any",
        "RuleGroups": [
          {
            "Name": "Staging Admin",
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
          },
          {
            "Name": "Protect Secret",
            "Enabled": true,
            "RulesOperator": "Any",
            "Rules": [
              {
                "Name": "Secret Area",
                "Pattern": "^/secret/",
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
This example uses rule groups to create complex matching logic. The top-level `RulesOperator` is `Any`, meaning authentication is required if **either** group matches. The "Staging Admin" group uses `All`, so it only matches when **both** the host starts with `staging.` **and** the path starts with `/admin/`. Rule groups can be nested recursively via the `RuleGroups` property.