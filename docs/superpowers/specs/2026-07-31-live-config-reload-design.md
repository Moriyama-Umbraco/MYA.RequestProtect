# Live config reload for RequestProtectMiddleware

## Problem

`RequestProtectMiddleware` takes `IOptionsMonitor<RequestProtectOptions>` but reads `config.CurrentValue` once in the constructor into a readonly field. Derived caches (`_regexCache`, `_parsedWhitelist`, `_parsedHeaders`) are also built once in the constructor. Because ASP.NET Core constructs middleware once for the app lifetime, changes to `appsettings.json` (via the file-reload provider) or environment variables never take effect without an app restart.

## Goal

Config changes picked up by `IOptionsMonitor<RequestProtectOptions>` (file change, `IConfiguration` reload) must update middleware behavior — including rule/whitelist/header matching — without an app restart.

## Design

Introduce a private nested `CompiledConfig` holding everything currently derived from options once at startup:

```csharp
private sealed class CompiledConfig
{
    public required RequestProtectOptions Options { get; init; }
    public required FrozenDictionary<string, Regex> RegexCache { get; init; }
    public required List<WhitelistEntry> Whitelist { get; init; }
    public required List<HeaderEntry> Headers { get; init; }
}
```

A single `private static CompiledConfig Compile(RequestProtectOptions options)` method builds one from raw options (reusing existing `BuildRegexCache`, `ParseWhitelist`, `ParseHeaders` static helpers unchanged).

Middleware holds:

```csharp
private volatile CompiledConfig _compiled;
```

Constructor:

```csharp
_compiled = Compile(monitor.CurrentValue);
monitor.OnChange(o => _compiled = Compile(o));
```

Reference reassignment is atomic in .NET; `volatile` ensures visibility across threads without locking readers.

All existing call sites (`config.Enabled`, `config.Rules...`, `_regexCache`, `_parsedWhitelist`, `_parsedHeaders`) are repointed at `_compiled`:

- Existing private `config` field is removed; a private property `RequestProtectOptions config => _compiled.Options;` preserves nearly every existing call site's source text unchanged.
- `_regexCache` → `_compiled.RegexCache`, `_parsedWhitelist` → `_compiled.Whitelist`, `_parsedHeaders` → `_compiled.Headers` (3 usage sites: `DoesRulePass`, `IsIpAllowed`, `HeadersAuthorised`).

### Consistency model

Each `config.X` read reflects the latest compiled snapshot at the moment of the read; a config reload mid-request could theoretically mix old/new values across multiple reads within one request. This matches the risk profile already inherent to `IOptionsMonitor.CurrentValue` and is accepted — not worth threading a per-request snapshot through ~10 private method signatures for a config that reloads rarely (deploy/ops time, not per-request).

### Lifecycle

No `IDisposable` implementation. The middleware instance lives for the app's lifetime; `OnChange` registration is not explicitly disposed (consistent with typical `IOptionsMonitor.OnChange` usage in ASP.NET Core apps where the callback lives as long as the app).

## Testing

- New test: build test host with initial `RequestProtectOptions`, assert baseline rule behavior, then push updated config through `IConfiguration` (test host's `TestServer` config reload or by re-binding `IOptionsMonitor` test double), assert middleware now matches the new rules/whitelist/headers without restarting the host.
- Confirm existing snapshot tests still pass unchanged (no behavior change for the non-reload path).

## Out of scope

- Hot-reloading `MYA.RequestProtect.Umbraco` / `.Bellissima` integration wiring — they only register the middleware, no separate config snapshot logic to fix.
- Per-request config snapshotting for strict consistency (rejected above).
