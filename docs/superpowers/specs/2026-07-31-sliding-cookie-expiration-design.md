# Sliding cookie expiration

## Problem

`RequestProtectMiddleware`'s auth cookie (`MYAPA`) is issued once, on successful query-string auth, with a fixed expiry (`now + Cookie.ExpiryMinutes`). Once a valid cookie exists, `InvokeAsync` short-circuits straight through to `_next(context)` (`RequestProtectMiddleware.cs:116-120`) without ever touching the cookie again. An actively-browsing user whose session outlives `ExpiryMinutes` gets logged out and must re-supply the query-string code, even though they never stopped using the site.

## Goal

Add an opt-in sliding expiration: while enabled, every request that carries a valid auth cookie has that cookie's expiry pushed forward to `now + ExpiryMinutes`, so an active user's cookie never lapses.

## Design

### `CookieSettings`

New property, default `false` (opt-in, no behavior change for existing configs):

```csharp
/// <summary>
/// When true, and PersistCookie is also true, the auth cookie's expiry is reset to
/// now + ExpiryMinutes on every request that carries a valid cookie, keeping an
/// actively-browsing user authenticated indefinitely. No effect when PersistCookie
/// is false (session cookies carry no server-tracked expiry to extend).
/// </summary>
public bool SlidingExpiration { get; set; } = false;
```

### `RequestProtectMiddleware`

Currently:

```csharp
public async Task InvokeAsync(HttpContext context)
{
    if (!config.Enabled || HasMiddlewareAuthCookie(context.Request))
    {
        await _next(context);
        return;
    }
    ...
```

Change the short-circuit branch to refresh the cookie when sliding is enabled and a valid cookie is present:

```csharp
public async Task InvokeAsync(HttpContext context)
{
    if (!config.Enabled)
    {
        await _next(context);
        return;
    }

    if (HasMiddlewareAuthCookie(context.Request, out var cookieValue))
    {
        if (config.Cookie.SlidingExpiration && config.Cookie.PersistCookie)
        {
            SetAuthCookie(context, cookieValue);
        }

        await _next(context);
        return;
    }
    ...
```

`SetAuthCookie` is extracted from the existing cookie-issuing code in the `ValidateCode` success path (currently inlined in `InvokeAsync`, `RequestProtectMiddleware.cs:126-141`) into a private helper taking the cookie value to write, so both the first-issue path and the sliding-refresh path share one cookie-construction implementation:

```csharp
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

First-issue call site becomes `SetAuthCookie(context, dateTimeProvider.Now.Ticks.ToString());` (same value generation as today — unchanged).

Sliding-refresh call site reuses the *existing* cookie value read from the request (`cookieValue`, from `HasMiddlewareAuthCookie`) rather than minting a new one — the cookie's identity value doesn't change on a slide, only its expiry.

`HasMiddlewareAuthCookie` changes from:

```csharp
private static bool HasMiddlewareAuthCookie(HttpRequest request)
    => request.Cookies.TryGetValue(RequestProtectCookieName, out var cookieVal) && !string.IsNullOrWhiteSpace(cookieVal);
```

to an overload-style signature that also returns the value, so the sliding path doesn't do a second cookie lookup:

```csharp
private static bool HasMiddlewareAuthCookie(HttpRequest request, out string cookieValue)
{
    var found = request.Cookies.TryGetValue(RequestProtectCookieName, out var cookieVal) && !string.IsNullOrWhiteSpace(cookieVal);
    cookieValue = found ? cookieVal! : string.Empty;
    return found;
}
```

### Non-goals

- No change to `ExpiryMinutes` semantics — it's still "minutes from last (re)issue," just now re-issued on every request when sliding is on, instead of only once.
- No sliding when `PersistCookie` is `false` — session cookies have no `Expires` for the server to extend; `SlidingExpiration: true` with `PersistCookie: false` is accepted but inert, not validated/rejected.
- No threshold/debounce logic (e.g. only reissue past 50% elapsed, as ASP.NET Core's own cookie auth does) — every request with sliding enabled reissues the `Set-Cookie` header. Simpler, matches user's explicit choice; a debounce can be added later if the extra header traffic becomes a real concern.

## Testing

- New test: sliding enabled + `PersistCookie: true` — request with a valid cookie gets a `Set-Cookie` response header with a fresh `Expires` value (assert response has `Set-Cookie`, not just that request passed through).
- New test: sliding disabled (default) — request with a valid cookie passes through with no `Set-Cookie` header, confirming no behavior change from today.
- New test: sliding enabled + `PersistCookie: false` — request with a valid cookie passes through with no `Set-Cookie` header (inert combination).
- Existing cookie tests (`CookieConfigurationTests.cs`) must continue to pass unchanged — first-issue behavior is unaffected by this change.
