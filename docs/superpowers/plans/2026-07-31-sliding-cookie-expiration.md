# Sliding Cookie Expiration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an opt-in `CookieSettings.SlidingExpiration` option so an actively-browsing user's `MYAPA` auth cookie expiry is pushed forward on every request instead of lapsing at a fixed time from first issue.

**Architecture:** Extract the existing inline cookie-issuing code in `RequestProtectMiddleware.InvokeAsync` into a private `SetAuthCookie(HttpContext, string value)` helper. Change `HasMiddlewareAuthCookie` to also return the existing cookie's value via `out`. When a valid cookie is present and `SlidingExpiration && PersistCookie` are both true, call `SetAuthCookie` with the existing cookie value to reissue it with a refreshed `Expires`. No new types, no new configuration surface beyond one boolean.

**Tech Stack:** ASP.NET Core middleware, xUnit v3 + Verify snapshot testing (existing test stack).

## Global Constraints

- Spec: `docs/superpowers/specs/2026-07-31-sliding-cookie-expiration-design.md`
- `CookieSettings.SlidingExpiration` defaults to `false` — no behavior change for existing configs.
- `SlidingExpiration: true` with `PersistCookie: false` is accepted but inert (session cookies have no `Expires` to extend) — not validated, not rejected, no exception.
- No threshold/debounce logic — every request with sliding enabled and a valid cookie reissues `Set-Cookie` (rejected in spec as unnecessary complexity for this task).
- Sliding-refresh reuses the cookie's *existing* value (read from the request) — it does not mint a new value. Only `Expires` changes.
- Existing tests (`CookieConfigurationTests.cs` and the rest of the 96-test suite) must continue to pass unchanged.

---

### Task 1: Extract `SetAuthCookie` helper and add sliding refresh to `InvokeAsync`

**Files:**
- Modify: `src/MYA.RequestProtect/Options/CookieSettings.cs`
- Modify: `src/MYA.RequestProtect/RequestProtectMiddleware.cs`

**Interfaces:**
- Produces: `CookieSettings.SlidingExpiration` (`bool`, default `false`).
- Produces: `private void SetAuthCookie(HttpContext context, string value)` on `RequestProtectMiddleware` — builds the `CookieOptions` (`HttpOnly`, `SameSite=Strict`, `IsEssential`, `Secure`, conditional `Expires` when `config.Cookie.PersistCookie`) and appends the cookie via `context.Response.Cookies.Append(RequestProtectCookieName, value, cookieOpts)`.
- Produces: `private static bool HasMiddlewareAuthCookie(HttpRequest request, out string cookieValue)` — replaces the existing single-arg overload; `cookieValue` is the cookie's raw string value when found (non-null, non-whitespace), or `string.Empty` when not found.

No test file for this task alone — covered end-to-end by Task 2's new tests, and this task's Step 4 (full suite run) proves no regression on first-issue behavior.

- [ ] **Step 1: Add `SlidingExpiration` to `CookieSettings`**

In `src/MYA.RequestProtect/Options/CookieSettings.cs`, add after the existing `PersistCookie` property:

```csharp
/// <summary>
/// When true, and PersistCookie is also true, the auth cookie's expiry is reset to
/// now + ExpiryMinutes on every request that carries a valid cookie, keeping an
/// actively-browsing user authenticated indefinitely. No effect when PersistCookie
/// is false (session cookies carry no server-tracked expiry to extend).
/// </summary>
public bool SlidingExpiration { get; set; } = false;
```

The full file should read:

```csharp
using System.ComponentModel.DataAnnotations;

namespace MYA.RequestProtect.Options;

public class CookieSettings
{
    /// <summary>
    /// Cookie expiry duration in minutes (default: 30)
    /// </summary>
    [Range(1, 525_600)]
    public int ExpiryMinutes { get; set; } = 30;

    /// <summary>
    /// When true, sets an explicit Expires header on the cookie (survives browser restart).
    /// When false, creates a session cookie (deleted on browser close).
    /// </summary>
    public bool PersistCookie { get; set; } = true;

    /// <summary>
    /// When true, and PersistCookie is also true, the auth cookie's expiry is reset to
    /// now + ExpiryMinutes on every request that carries a valid cookie, keeping an
    /// actively-browsing user authenticated indefinitely. No effect when PersistCookie
    /// is false (session cookies carry no server-tracked expiry to extend).
    /// </summary>
    public bool SlidingExpiration { get; set; } = false;
}
```

- [ ] **Step 2: Extract `SetAuthCookie` and change `HasMiddlewareAuthCookie` to an `out`-returning overload**

In `src/MYA.RequestProtect/RequestProtectMiddleware.cs`, replace the current `InvokeAsync` method (lines 138-175):

```csharp
public async Task InvokeAsync(HttpContext context)
{
    if (!config.Enabled || HasMiddlewareAuthCookie(context.Request))
    {
        await _next(context);
        return;
    }

    logger.LogAuthCookieNotFound();

    if (RequestIsAuthorised(context, out var setCookie))
    {
        if (setCookie is true)
        {
            var cookieOpts = new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Strict,
                IsEssential = true,
                Secure = true
            };

            if (config.Cookie.PersistCookie)
            {
                cookieOpts.Expires = dateTimeProvider.NowOffSet.AddMinutes(config.Cookie.ExpiryMinutes);
            }

            context.Response.Cookies.Append(RequestProtectCookieName, dateTimeProvider.Now.Ticks.ToString(), cookieOpts);
        }
    }
    else
    {
        await HandleUnAuthorisedRequest(context);
        return;
    }

    await _next(context);
}
```

with:

```csharp
public async Task InvokeAsync(HttpContext context)
{
    if (!config.Enabled)
    {
        await _next(context);
        return;
    }

    if (HasMiddlewareAuthCookie(context.Request, out var existingCookieValue))
    {
        if (config.Cookie.SlidingExpiration && config.Cookie.PersistCookie)
        {
            SetAuthCookie(context, existingCookieValue);
        }

        await _next(context);
        return;
    }

    logger.LogAuthCookieNotFound();

    if (RequestIsAuthorised(context, out var setCookie))
    {
        if (setCookie is true)
        {
            SetAuthCookie(context, dateTimeProvider.Now.Ticks.ToString());
        }
    }
    else
    {
        await HandleUnAuthorisedRequest(context);
        return;
    }

    await _next(context);
}

private void SetAuthCookie(HttpContext context, string value)
{
    var cookieOpts = new CookieOptions
    {
        HttpOnly = true,
        SameSite = SameSiteMode.Strict,
        IsEssential = true,
        Secure = true
    };

    if (config.Cookie.PersistCookie)
    {
        cookieOpts.Expires = dateTimeProvider.NowOffSet.AddMinutes(config.Cookie.ExpiryMinutes);
    }

    context.Response.Cookies.Append(RequestProtectCookieName, value, cookieOpts);
}
```

Then find the existing `HasMiddlewareAuthCookie` method (currently around line 481):

```csharp
private static bool HasMiddlewareAuthCookie(HttpRequest request)
    => request.Cookies.TryGetValue(RequestProtectCookieName, out var cookieVal) && !string.IsNullOrWhiteSpace(cookieVal);
```

Replace it with the `out`-returning form:

```csharp
private static bool HasMiddlewareAuthCookie(HttpRequest request, out string cookieValue)
{
    var found = request.Cookies.TryGetValue(RequestProtectCookieName, out var cookieVal) && !string.IsNullOrWhiteSpace(cookieVal);
    cookieValue = found ? cookieVal! : string.Empty;
    return found;
}
```

Search the file for any other call site of `HasMiddlewareAuthCookie(` besides the one in `InvokeAsync` you just updated — if any exist, update them to use the new `out`-returning signature too (discard the value with `out _` if the call site doesn't need it).

- [ ] **Step 3: Build**

Run: `dotnet build src/MYA.RequestProtect/MYA.RequestProtect.csproj`
Expected: builds with 0 errors across net8.0/net9.0/net10.0.

- [ ] **Step 4: Run the full existing test suite to confirm no regression**

Run: `dotnet test src/MYA.RequestProtect.Test/MYA.RequestProtect.Tests.csproj`
Expected: all 96 currently-passing tests still pass (default `SlidingExpiration = false` means the new `if (config.Cookie.SlidingExpiration && ...)` branch is never taken by any existing test, so first-issue and short-circuit behavior is unchanged).

- [ ] **Step 5: Commit**

```bash
git add src/MYA.RequestProtect/Options/CookieSettings.cs src/MYA.RequestProtect/RequestProtectMiddleware.cs
git commit -m "feat: add opt-in sliding cookie expiration

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 2: Sliding expiration tests

**Files:**
- Modify: `src/MYA.RequestProtect.Test/CookieConfigurationTests.cs`

**Interfaces:**
- Consumes: `Host.CreateTestServer(logger, options)` (existing, unchanged), `RequestProtectOptions`, `CookieSettings.SlidingExpiration` / `.PersistCookie` (Task 1).
- Consumes: the existing `MYAPA` cookie name and cookie-request pattern already used elsewhere in this file's sibling tests (`MiddlewareShortCircuitTests.cs`): `request.Headers.Add("Cookie", "MYAPA=somevalue");`.

- [ ] **Step 1: Write the three new tests**

Add to `src/MYA.RequestProtect.Test/CookieConfigurationTests.cs`, inside the `CookieConfigurationTests` class, after the existing `ValidAuth_SessionCookie_ExpiryMinutesIgnored` test:

```csharp
[Fact]
public async Task SlidingExpiration_Enabled_RefreshesCookieOnEachRequest()
{
    // Arrange
    var options = BlockingOptions;
    options.Cookie.SlidingExpiration = true;
    options.Cookie.PersistCookie = true;

    using var server = Host.CreateTestServer(logger, options);
    var client = server.CreateClient();

    var request = new HttpRequestMessage(HttpMethod.Get, "/admin/secret");
    request.Headers.Add("Cookie", "MYAPA=somevalue");

    // Act
    var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

    // Assert
    Assert.True(response.Headers.Contains("Set-Cookie"));
    var setCookie = response.Headers.GetValues("Set-Cookie").Single();
    Assert.Contains("MYAPA=somevalue", setCookie);
    Assert.Contains("expires=", setCookie, StringComparison.OrdinalIgnoreCase);
}

[Fact]
public async Task SlidingExpiration_Disabled_DoesNotRefreshCookie()
{
    // Arrange
    var options = BlockingOptions;
    options.Cookie.SlidingExpiration = false;
    options.Cookie.PersistCookie = true;

    using var server = Host.CreateTestServer(logger, options);
    var client = server.CreateClient();

    var request = new HttpRequestMessage(HttpMethod.Get, "/admin/secret");
    request.Headers.Add("Cookie", "MYAPA=somevalue");

    // Act
    var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

    // Assert
    Assert.False(response.Headers.Contains("Set-Cookie"));
}

[Fact]
public async Task SlidingExpiration_Enabled_WithSessionCookie_DoesNotRefreshCookie()
{
    // Arrange
    var options = BlockingOptions;
    options.Cookie.SlidingExpiration = true;
    options.Cookie.PersistCookie = false;

    using var server = Host.CreateTestServer(logger, options);
    var client = server.CreateClient();

    var request = new HttpRequestMessage(HttpMethod.Get, "/admin/secret");
    request.Headers.Add("Cookie", "MYAPA=somevalue");

    // Act
    var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

    // Assert
    Assert.False(response.Headers.Contains("Set-Cookie"));
}
```

Add `using System.Linq;` to the top of `CookieConfigurationTests.cs` if it is not already present (needed for `.Single()`); check the existing `using` block first since `ImplicitUsings` may already cover it — if `dotnet build` (Step 3 below) reports no error about `Single`, leave the usings as they are.

- [ ] **Step 2: Run the three new tests**

Run: `dotnet test src/MYA.RequestProtect.Test/MYA.RequestProtect.Tests.csproj --filter "FullyQualifiedName~SlidingExpiration"`
Expected: PASS (Task 1's implementation is already in place).

- [ ] **Step 3: Build and run the full suite**

Run: `dotnet build src/MYA.RequestProtect.Test/MYA.RequestProtect.Tests.csproj`
Expected: 0 errors.

Run: `dotnet test src/MYA.RequestProtect.Test/MYA.RequestProtect.Tests.csproj`
Expected: all tests pass (96 existing + 3 new = 99) across net8.0/net9.0/net10.0.

- [ ] **Step 4: Commit**

```bash
git add src/MYA.RequestProtect.Test/CookieConfigurationTests.cs
git commit -m "test: cover sliding cookie expiration behavior

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 3: Document the new option

**Files:**
- Modify: `CLAUDE.md`
- Modify: `docs/README_nuget.md`

**Interfaces:**
- Consumes: `CookieSettings.SlidingExpiration` (Task 1) — no new interfaces produced, documentation only.

- [ ] **Step 1: Add a short note to `CLAUDE.md`**

In `CLAUDE.md`, find the `### Configuration` section's bullet list (it currently ends with `- \`ResponseOptions\` - controls unauthorized response behavior (Default 400, Redirect, or StaticFile with MimeType)`). Add one line after it:

```markdown
- `CookieSettings` - controls the `MYAPA` auth cookie's `ExpiryMinutes`, whether it `PersistCookie` (persistent vs. session cookie), and optional `SlidingExpiration` (refreshes the cookie's expiry on every request from an already-authenticated client; no effect when `PersistCookie` is false)
```

- [ ] **Step 2: Add a config example to `docs/README_nuget.md`**

In `docs/README_nuget.md`, after the existing "Quick Start" JSON example (the block ending `"Code": "your_secret_code"`), add a new subsection before `## Documentation`:

```markdown
## Cookie Options

```json
{
  "MYA:RP": {
    "Enabled": true,
    "QueryKey": "auth",
    "Code": "your_secret_code",
    "Cookie": {
      "ExpiryMinutes": 30,
      "PersistCookie": true,
      "SlidingExpiration": true
    }
  }
}
```

With `SlidingExpiration: true`, an already-authenticated visitor's cookie expiry resets to `now + ExpiryMinutes` on every request, so an actively-browsing user is never logged out mid-session. Has no effect when `PersistCookie` is `false` (session cookies have no server-tracked expiry to extend).
```

- [ ] **Step 3: Verify no build/test impact**

Run: `dotnet build src/MYA.RequestProtect.sln`
Expected: 0 errors (documentation-only change, sanity check that nothing else broke).

- [ ] **Step 4: Commit**

```bash
git add CLAUDE.md docs/README_nuget.md
git commit -m "docs: document sliding cookie expiration option

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

## Self-Review Notes

- **Spec coverage:** `CookieSettings.SlidingExpiration` (Task 1) ✅, `SetAuthCookie` extraction + reissue on sliding (Task 1) ✅, `HasMiddlewareAuthCookie` out-value change (Task 1) ✅, inert-when-session-cookie behavior (Task 1's `&& config.Cookie.PersistCookie` guard) ✅, all three testing scenarios from the spec's Testing section (Task 2) ✅, existing cookie tests unaffected (Task 1 Step 4, Task 2 Step 3) ✅. Non-goals (no threshold/debounce, no `ExpiryMinutes` semantic change) are respected — no task introduces either.
- **Type consistency:** `SetAuthCookie(HttpContext, string)`, `HasMiddlewareAuthCookie(HttpRequest, out string)` signatures match between Task 1's description and its code, and Task 2's tests only call through `Host.CreateTestServer`/HTTP, not these private members directly, so no cross-task signature drift risk.
- **No placeholders:** all steps contain literal code, not descriptions.
