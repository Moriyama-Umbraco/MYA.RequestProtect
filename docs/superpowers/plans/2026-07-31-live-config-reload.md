# Live Config Reload Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `RequestProtectMiddleware` picks up `appsettings.json`/environment-variable config changes live (via `IOptionsMonitor<RequestProtectOptions>.OnChange`) instead of only at process start.

**Architecture:** Replace the middleware's one-time constructor snapshot (`RequestProtectOptions` field + precomputed regex/whitelist/header caches) with a single `CompiledConfig` object rebuilt on every `IOptionsMonitor.OnChange` notification and swapped into a `volatile` field. All existing option/cache reads are repointed at the current `CompiledConfig` via a compatibility property so most method bodies stay untouched. Test infrastructure gets a small reloadable `IConfigurationProvider` so tests can push a config change into a running `TestServer` and assert the middleware responds to the new rules without restarting the host.

**Tech Stack:** ASP.NET Core middleware, `Microsoft.Extensions.Options` (`IOptionsMonitor`), `Microsoft.Extensions.Configuration` (custom `IConfigurationProvider` for tests), xUnit v3 + Verify (existing test stack).

## Global Constraints

- Spec: `docs/superpowers/specs/2026-07-31-live-config-reload-design.md`
- No per-request config snapshotting (rejected in spec) — each `config.X` read reflects the latest compiled snapshot at read time; this matches existing `IOptionsMonitor.CurrentValue` semantics and is accepted risk.
- No `IDisposable` on the middleware — the `OnChange` registration lives for the app's lifetime, consistent with normal `IOptionsMonitor.OnChange` usage.
- Existing static helpers `BuildRegexCache`, `ParseWhitelist`, `ParseHeaders` in `RequestProtectMiddleware.cs` are reused unchanged, just called from a new `Compile` method.
- Existing tests (`MiddlewareShortCircuitTests`, `CookieConfigurationTests`, `ResponseTypeTests`, `AuthRulePolymorphismTests`, `LoggingValidationTests`) must continue to pass unchanged — the `Host.CreateTestServer` signature must not change for existing callers.

---

### Task 1: Compile config into a swappable snapshot in the middleware

**Files:**
- Modify: `src/MYA.RequestProtect/RequestProtectMiddleware.cs`

**Interfaces:**
- Produces: private nested `CompiledConfig` class with `Options` (`RequestProtectOptions`), `RegexCache` (`FrozenDictionary<string, Regex>`), `Whitelist` (`List<WhitelistEntry>`), `Headers` (`List<HeaderEntry>`) — all `required` init-only properties.
- Produces: `private static CompiledConfig Compile(RequestProtectOptions options)` — pure function, no `this` access, callable from both the constructor and the `OnChange` callback.
- Produces: `private volatile CompiledConfig _compiled` — current snapshot.
- Produces: `private RequestProtectOptions config => _compiled.Options;` — replaces the old `config` field so all other existing method bodies (`InvokeAsync`, `AuthNotNeeded`, `ValidateCode`, `HandleUnAuthorisedRequest`, `HandleStaticFileResponse`, `HandleRedirectResponse`, `DefaultResponse`, `ResolveClientIp`) keep compiling with zero further edits.

No new test file for this task — it's covered end-to-end by Task 3's tests. Existing tests must still pass (verified in Step 4 below) since this task preserves current behavior exactly; only the reload wiring is new.

- [ ] **Step 1: Replace the constructor's snapshot fields with `CompiledConfig` + `Compile`**

In `src/MYA.RequestProtect/RequestProtectMiddleware.cs`, remove these fields:

```csharp
private readonly RequestProtectOptions config;
private readonly FrozenDictionary<string, Regex> _regexCache;
private readonly List<WhitelistEntry> _parsedWhitelist;
private readonly List<HeaderEntry> _parsedHeaders;
```

Replace with:

```csharp
private volatile CompiledConfig _compiled;

private RequestProtectOptions config => _compiled.Options;

private sealed class CompiledConfig
{
    public required RequestProtectOptions Options { get; init; }
    public required FrozenDictionary<string, Regex> RegexCache { get; init; }
    public required List<WhitelistEntry> Whitelist { get; init; }
    public required List<HeaderEntry> Headers { get; init; }
}

private static CompiledConfig Compile(RequestProtectOptions options) => new()
{
    Options = options,
    RegexCache = BuildRegexCache(options.Rules),
    Whitelist = ParseWhitelist(options.Rules.IpWhitelist),
    Headers = ParseHeaders(options.Rules.Headers)
};
```

Update the constructor body from:

```csharp
this.config = config.CurrentValue;
_next = next;
this.logger = logger;
this.dateTimeProvider = dateTimeProvider;
this.hostingEnvironment = hostingEnvironment;
_regexCache = BuildRegexCache(this.config.Rules);
_parsedWhitelist = ParseWhitelist(this.config.Rules.IpWhitelist);
_parsedHeaders = ParseHeaders(this.config.Rules.Headers);
```

to:

```csharp
_next = next;
this.logger = logger;
this.dateTimeProvider = dateTimeProvider;
this.hostingEnvironment = hostingEnvironment;
_compiled = Compile(config.CurrentValue);
config.OnChange(newOptions => _compiled = Compile(newOptions));
```

(The constructor parameter is still named `config`, of type `IOptionsMonitor<RequestProtectOptions>` — it shadows the new `config` property inside the constructor only, same as it shadowed the old field before. No parameter rename needed.)

- [ ] **Step 2: Repoint the three remaining direct cache-field usages**

In `DoesRulePass`, change:

```csharp
if (!_regexCache.TryGetValue(r.Pattern, out var regex))
```
to:
```csharp
if (!_compiled.RegexCache.TryGetValue(r.Pattern, out var regex))
```

In `IsIpAllowed`, change:
```csharp
foreach (ref readonly var entry in CollectionsMarshal.AsSpan(_parsedWhitelist))
```
to:
```csharp
var whitelist = _compiled.Whitelist;
foreach (ref readonly var entry in CollectionsMarshal.AsSpan(whitelist))
```
(local variable needed because `CollectionsMarshal.AsSpan` takes a `List<T>` argument, not an expression that re-reads a volatile field mid-iteration — reading `_compiled` once up front avoids observing two different snapshots inside a single loop.)

In `HeadersAuthorised`, change:
```csharp
if (_parsedHeaders.Count == 0) return false;

foreach (ref readonly var header in CollectionsMarshal.AsSpan(_parsedHeaders))
```
to:
```csharp
var headers = _compiled.Headers;
if (headers.Count == 0) return false;

foreach (ref readonly var header in CollectionsMarshal.AsSpan(headers))
```

- [ ] **Step 3: Build**

Run: `dotnet build src/MYA.RequestProtect/MYA.RequestProtect.csproj`
Expected: builds with 0 errors across all three target frameworks (net8.0/net9.0/net10.0).

- [ ] **Step 4: Run the full existing test suite to confirm no regression**

Run: `dotnet test src/MYA.RequestProtect.Test/MYA.RequestProtect.Tests.csproj`
Expected: all currently-passing tests still pass (this task is a behavior-preserving refactor).

- [ ] **Step 5: Commit**

```bash
git add src/MYA.RequestProtect/RequestProtectMiddleware.cs
git commit -m "refactor: compile options into swappable snapshot, wire OnChange

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 2: Reloadable test configuration source

**Files:**
- Create: `src/MYA.RequestProtect.Test/Setup/ReloadableConfigurationSource.cs`
- Modify: `src/MYA.RequestProtect.Test/Setup/Host.cs`

**Interfaces:**
- Consumes: `RequestProtectOptions.Key` (`"MYA:RP"`, from `src/MYA.RequestProtect/Options/RequestProtectOptions.cs:7`).
- Produces: `internal sealed class ReloadableConfigurationProvider : ConfigurationProvider` with `public void Update(IDictionary<string, string?> newData)`.
- Produces: `internal sealed class ReloadableConfigurationSource : IConfigurationSource` with `public ReloadableConfigurationProvider Provider { get; }`.
- Produces: `Host.CreateTestServer(...)` (existing signature, unchanged) now also registers the `ReloadableConfigurationProvider` (when `options is not null`) as a singleton service, retrievable via `server.Services.GetRequiredService<ReloadableConfigurationProvider>()`, so tests can call `.Update(...)` on a live `TestServer` without any signature change.

- [ ] **Step 1: Create `ReloadableConfigurationSource.cs`**

```csharp
using Microsoft.Extensions.Configuration;

namespace MYA.RequestProtect.Tests.Setup;

internal sealed class ReloadableConfigurationProvider : ConfigurationProvider
{
    public void Update(IDictionary<string, string?> newData)
    {
        Data = new Dictionary<string, string?>(newData, StringComparer.OrdinalIgnoreCase);
        OnReload();
    }
}

internal sealed class ReloadableConfigurationSource : IConfigurationSource
{
    public ReloadableConfigurationProvider Provider { get; } = new();

    public IConfigurationProvider Build(IConfigurationBuilder builder) => Provider;
}
```

- [ ] **Step 2: Wire it into `Host.CreateTestServer`**

In `src/MYA.RequestProtect.Test/Setup/Host.cs`, add a `using System.Linq;` if not already present (needed for `.ToDictionary` below), then replace the `ConfigureAppConfiguration` block:

```csharp
.ConfigureAppConfiguration((context, config) =>
{
    if (options is not null)
    {
        var json = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            [RequestProtectOptions.Key] = options
        });

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        config.AddJsonStream(stream);
    }
})
```

with:

```csharp
.ConfigureAppConfiguration((context, config) =>
{
    if (options is not null)
    {
        var source = new ReloadableConfigurationSource();
        source.Provider.Update(FlattenOptions(options));
        configProvider = source.Provider;
        config.Add(source);
    }
})
```

Immediately before the `var builder = new WebHostBuilder()` line, declare:

```csharp
ReloadableConfigurationProvider? configProvider = null;
```

In the existing `.ConfigureServices(services => { ... })` block, add the registration after the existing two lines:

```csharp
services.RemoveAll<IDatetimeProvider>();
services.AddSingleton<IDatetimeProvider, TestDatetimeProvider>();
if (configProvider is not null)
{
    services.AddSingleton(configProvider);
}
```

Add a private static helper at the bottom of the `Host` class (same file):

```csharp
private static Dictionary<string, string?> FlattenOptions(RequestProtectOptions options)
{
    var json = JsonSerializer.Serialize(new Dictionary<string, object?>
    {
        [RequestProtectOptions.Key] = options
    });

    using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
    var flat = new ConfigurationBuilder().AddJsonStream(stream).Build();
    return flat.AsEnumerable().ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);
}
```

(`ConfigureAppConfiguration` runs during `WebHostBuilder.Build()` before `ConfigureServices` runs, so the captured `configProvider` local is already assigned by the time the `ConfigureServices` closure executes — both closures capture the same outer variable.)

- [ ] **Step 3: Build and run full existing suite**

Run: `dotnet build src/MYA.RequestProtect.Test/MYA.RequestProtect.Tests.csproj`
Expected: builds with 0 errors.

Run: `dotnet test src/MYA.RequestProtect.Test/MYA.RequestProtect.Tests.csproj`
Expected: all existing tests still pass (the flattened-JSON config data is functionally equivalent to the old `AddJsonStream` binding — same JSON, just pre-flattened into key/value pairs before being handed to the config system).

- [ ] **Step 4: Commit**

```bash
git add src/MYA.RequestProtect.Test/Setup/ReloadableConfigurationSource.cs src/MYA.RequestProtect.Test/Setup/Host.cs
git commit -m "test: add reloadable configuration source for live-reload tests

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 3: Live reload tests

**Files:**
- Create: `src/MYA.RequestProtect.Test/LiveConfigReloadTests.cs`

**Interfaces:**
- Consumes: `Host.CreateTestServer(logger, options)` (from Task 2), `server.Services.GetRequiredService<ReloadableConfigurationProvider>()`, `configProvider.Update(IDictionary<string,string?>)`.
- Consumes: `RequestProtectOptions`, `AuthRules`, `AuthRule`, `AppliesTo` (existing, from `MYA.RequestProtect.Options`).

This task's tests fail against Task 1's `main` branch state reverted (i.e. they'd fail against the pre-fix middleware) and pass once Task 1's fix is in place. Since Task 1 already landed by the time this task runs, Step 2 below is a sanity check by temporarily reverting — do this via inspection, not by literally reverting: read `RequestProtectMiddleware.cs` and confirm the `OnChange` wiring exists; if Step 4 (run tests) passes on the first try, that's the confirmation the fix works, so an actual revert-and-rerun is not required as a separate step.

- [ ] **Step 1: Write the two failing-then-passing tests**

```csharp
using Microsoft.Extensions.DependencyInjection;
using MYA.RequestProtect.Options;
using MYA.RequestProtect.Tests.Extensions;
using MYA.RequestProtect.Tests.Setup;

namespace MYA.RequestProtect.Tests;

public class LiveConfigReloadTests
{
    private readonly TestLogger logger = new();

    private static RequestProtectOptions UnprotectedOptions => new()
    {
        Enabled = true,
        Code = "valid_code",
        Rules = new AuthRules()
    };

    private static RequestProtectOptions BlockAllOptions => new()
    {
        Enabled = true,
        Code = "valid_code",
        Rules = new AuthRules
        {
            Rules =
            [
                new()
                {
                    Name = "Block All",
                    Pattern = ".*",
                    Enabled = true,
                    AppliesTo = AppliesTo.Path
                }
            ]
        }
    };

    private static RequestProtectOptions WhitelistOptions(string ip) => new()
    {
        Enabled = true,
        Code = "valid_code",
        Rules = new AuthRules
        {
            IpWhitelist = [ip],
            Rules =
            [
                new()
                {
                    Name = "Block All",
                    Pattern = ".*",
                    Enabled = true,
                    AppliesTo = AppliesTo.Path
                }
            ]
        }
    };

    [Fact]
    public async Task RuleChange_ViaConfigReload_TakesEffectWithoutRestart()
    {
        // Arrange
        using var server = Host.CreateTestServer(logger, UnprotectedOptions);
        var client = server.CreateClient();

        var before = await client.GetAsync("/admin/secret", TestContext.Current.CancellationToken);
        Assert.Equal(System.Net.HttpStatusCode.OK, before.StatusCode);

        // Act: push a new rule set that now blocks everything
        var configProvider = server.Services.GetRequiredService<ReloadableConfigurationProvider>();
        configProvider.Update(Host.FlattenOptionsForTest(BlockAllOptions));

        var after = await client.GetAsync("/admin/secret", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, after.StatusCode);
    }

    [Fact]
    public async Task WhitelistChange_ViaConfigReload_TakesEffectWithoutRestart()
    {
        // Arrange: whitelist a different IP than the test's remote IP, so the request is blocked
        using var server = Host.CreateTestServer(logger, WhitelistOptions("198.51.100.1"));
        var client = server.CreateClient();

        var before = await client.GetAsync("/admin/secret", TestContext.Current.CancellationToken);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, before.StatusCode);

        // Act: push a config update that whitelists the test's actual remote IP
        var configProvider = server.Services.GetRequiredService<ReloadableConfigurationProvider>();
        configProvider.Update(Host.FlattenOptionsForTest(WhitelistOptions(Host.DefaultRemoteIP)));

        var after = await client.GetAsync("/admin/secret", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, after.StatusCode);
    }
}
```

Because the test needs to flatten a `RequestProtectOptions` the same way `Host` does internally, change `FlattenOptions` in `Host.cs` (Task 2, Step 2) from `private static` to `internal static` and rename it `FlattenOptionsForTest` for a clear public-to-tests name:

```csharp
internal static Dictionary<string, string?> FlattenOptionsForTest(RequestProtectOptions options)
```

(Update the one call site inside `Host.CreateTestServer` accordingly: `source.Provider.Update(FlattenOptionsForTest(options));`.)

- [ ] **Step 2: Run the new tests**

Run: `dotnet test src/MYA.RequestProtect.Test/MYA.RequestProtect.Tests.csproj --filter "FullyQualifiedName~LiveConfigReloadTests"`
Expected: PASS (Task 1's fix is already in place, so these pass immediately — they exist to pin the behavior going forward and guard against regression).

- [ ] **Step 3: Run the full suite**

Run: `dotnet test src/MYA.RequestProtect.Test/MYA.RequestProtect.Tests.csproj`
Expected: all tests pass, including the two new ones.

- [ ] **Step 4: Commit**

```bash
git add src/MYA.RequestProtect.Test/LiveConfigReloadTests.cs src/MYA.RequestProtect.Test/Setup/Host.cs
git commit -m "test: cover live config reload for rules and IP whitelist

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

## Self-Review Notes

- **Spec coverage:** `CompiledConfig`/`Compile`/`OnChange` wiring (Task 1) ✅, consistency model documented inline via the volatile snapshot read pattern (Task 1, Step 2) ✅, testing section from spec covered by Task 3 ✅, out-of-scope items (Umbraco projects, per-request snapshot) untouched ✅.
- **Type consistency:** `CompiledConfig`, `Compile`, `_compiled`, `config` property names match between Task 1's description and step code. `ReloadableConfigurationProvider`/`ReloadableConfigurationSource`/`FlattenOptionsForTest` names match between Task 2 and Task 3.
- **No placeholders:** all steps contain literal code, not descriptions.
