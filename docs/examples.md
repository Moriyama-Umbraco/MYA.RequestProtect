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

## Example 3: Only block '/secret/` and child paths from being accessed without the code

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
            "Pattern": "^/secret/",
            "AppliesTo": "Path"
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
        "IpWhitelist":[
            "122.122.122.0/24",
            "86.86.86.85"
        ],
        "Rules": [
            {
            "Pattern": "^(?!my\\.allowed\\.host\\.com).*$",
            "AppliesTo": "Host"
          }
        ]
      }
    }
  }
}
```
This example blocks any request unless it comes in on the host `my.allowed.host.com` without the query string, or if the IP address of the client falls in the CIDR range `122.122.122.0/24` or is `86.86.86.85`.

## Example 5: Responed with a Static File
```json
{
  "MYA":
  { 
    "RP": {
      "Enabled": true,
      "Code": "My-Auth-Code",
      "Rules":{<Your rule's here>},
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