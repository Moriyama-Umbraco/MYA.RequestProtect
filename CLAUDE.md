# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Moriyama.RequestProtect is an ASP.NET Core middleware NuGet package that protects web requests through IP whitelisting (including CIDR), header authorization, URL pattern matching (regex), and query string authentication. Published as `Moriyama.RequestProtect` on NuGet.

## Build & Test Commands

```bash
# Build the entire solution
dotnet build src/MYA.RequestProtect.sln

# Build just the core library
dotnet build src/MYA.RequestProtect/MYA.RequestProtect.csproj

# Run all tests
dotnet test src/MYA.RequestProtect.Test/MYA.RequestProtect.Tests.csproj

# Run a single test by name
dotnet test src/MYA.RequestProtect.Test/MYA.RequestProtect.Tests.csproj --filter "FullyQualifiedName~TestMethodName"

# Run benchmarks (Release mode required)
dotnet run --project src/MYA.RequestProtect.Benchmarks/MYA.RequestProtect.Benchmarks.csproj -c Release
```

Note: Building the core library auto-generates `appsettings-schema.MYA.RequestProtect.json` via the `MYA.RequestProtect.ScehmaGenerator` project (runs on net8.0 target only).

## Architecture

### Middleware Pipeline

`RequestProtectMiddleware` is the core sealed class. On each request it:
1. Short-circuits if disabled or a valid auth cookie (`MYAPA`, 30-min expiry) exists
2. Checks if auth is not needed (IP whitelist -> headers -> rules, in that order with short-circuit)
3. If rules match (meaning protection applies), validates the query string code (`?{QueryKey}={Code}`)
4. Sets a secure HttpOnly cookie on successful auth; returns 400/redirect/static file on failure

Rules use **inverted logic**: if any enabled rule matches the request, auth **is** required. If no rules match, the request passes through unprotected.

### Rule System

- `AuthRule` - Leaf rule with regex `Pattern`, `AppliesTo` (Path, Host, Query, PathAndQuery), and `Enabled` flag
- `AuthRuleGroup` - Standalone class (no inheritance) with `Name`, `Enabled`, `RulesOperator` (All/Any), child `Rules[]` and nested `RuleGroups[]`. Supports recursive nesting
- `RuleGroupOperator` - Enum (`All`/`Any`) controlling how rules within a group are combined
- `AuthRules` has separate `Rules` (flat leaf rules) and `RuleGroups` (hierarchical groups) arrays, plus a top-level `RulesOperator` controlling combined evaluation. This design works natively with `Microsoft.Extensions.Configuration` binding (no custom JSON converter needed)

### Configuration

Options bind to the `MYA:RP` configuration section via `RequestProtectOptions`. Key nested types:
- `AuthRules` - contains `IpWhitelist` (string[]), `Headers` (HeaderDetail[]), `Rules` (AuthRule[]), `RuleGroups` (AuthRuleGroup[]), `RulesOperator` (RuleGroupOperator)
- `ResponseOptions` - controls unauthorized response behavior (Default 400, Redirect, or StaticFile with MimeType)

### Performance

The middleware pre-parses whitelist IPs and headers into struct arrays (`WhitelistEntry`, `HeaderEntry`) in the constructor. Regex patterns are cached in a `ConcurrentDictionary<string, Regex>`. Hot paths use `AggressiveInlining` and `Span`-based iteration.

### Projects

| Project | Purpose |
|---------|---------|
| `MYA.RequestProtect` | Core middleware library (net8.0/net9.0) |
| `MYA.RequestProtect.Tests` | xUnit v3 tests with Verify snapshot testing |
| `MYA.RequestProtect.Umbraco` | Umbraco CMS integration |
| `MYA.RequestProtect.Umbraco.Bellissima` | Umbraco Bellissima backoffice integration |
| `MYA.RequestProtect.ScehmaGenerator` | JSON schema generator (runs at build time) |
| `MYA.RequestProtect.Benchmarks` | BenchmarkDotNet performance suite |

### Test Infrastructure

Tests use `Microsoft.AspNetCore.TestHost` to create an in-memory test server. Key setup in `MYA.RequestProtect.Tests.Setup.Host`:
- Configures `RequestProtectOptions` from serialized JSON
- Injects `TestDatetimeProvider` (replacing `IDatetimeProvider`) and `TestLoggerProvider`
- Sets remote IP to `203.0.113.42` and pre-populates test headers
- Test cases are organized as `IEnumerable<TheoryDataRow>` classes in the `TestCases` folder

The core library exposes internals to the test assembly via `InternalsVisibleTo`.

**Verify Snapshots:** Tests use [Verify](https://github.com/VerifyTests/Verify) for snapshot testing. When tests produce new or changed `.received.txt` files, **do not auto-accept them**. The user must manually review and accept snapshots (by copying `.received.txt` to `.verified.txt` or using a Verify tool).

## Code Style

- File-scoped namespaces (`csharp_style_namespace_declarations = file_scoped`)
- 4-space indentation, CRLF line endings
- Allman-style braces (`csharp_new_line_before_open_brace = all`)
- `var` for built-in types and when type is apparent; explicit types elsewhere
- Nullable reference types enabled throughout
