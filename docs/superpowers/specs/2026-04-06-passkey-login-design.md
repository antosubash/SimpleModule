# Passkey Login — Design Spec

**Date:** 2026-04-06  
**Status:** Approved  
**Scope:** Add optional passkey authentication to the existing Users module

---

## Overview

Add WebAuthn passkey support to SimpleModule as an optional authentication method alongside existing password login. Users can register one or more passkeys from their account security settings and use them as an alternative to entering a password. This uses the built-in .NET 10 passkey support with no third-party library dependencies.

---

## Goals

- Users can register passkeys (Touch ID, Face ID, Windows Hello, hardware keys) from their account settings
- Users can sign in with a passkey instead of a password
- Users can view and delete their registered passkeys
- Passkeys are stored in the existing database alongside Identity data
- No changes to existing password login flow

---

## Non-Goals

- Passkey-only or passkey-first login (passwords remain the primary flow)
- Attestation validation
- Enterprise FIDO2 policy enforcement
- Replacing OpenIddict or the existing OAuth flows with passkeys

---

## Architecture

All changes live inside `modules/Users/`. No new module is created.

### Backend Components

**`UsersModule.ConfigureServices`** — configure `IdentityPasskeyOptions` via:
```csharp
services.Configure<IdentityPasskeyOptions>(configuration.GetSection("Passkeys"));
```
No `.AddPasskeys()` call is needed — passkey store support is included automatically when `AddEntityFrameworkStores<T>()` is used with Identity Schema Version 3.

**`UsersDbContext`** — override the `SchemaVersion` property to provision the `AspNetUserPasskeys` table:
```csharp
protected override Version SchemaVersion => IdentitySchemaVersions.Version3;
```
Then generate a new EF Core migration.

**6 new `IEndpoint` classes** under `Endpoints/Passkeys/`:

| Class | Route | Auth | Purpose |
|---|---|---|---|
| `PasskeyRegisterBeginEndpoint` | `POST /api/passkeys/register/begin` | Required | Calls `SignInManager.MakePasskeyCreationOptionsAsync()`, returns JSON options to browser. Call `.DisableAntiforgery()`. |
| `PasskeyRegisterCompleteEndpoint` | `POST /api/passkeys/register/complete` | Required | Calls `SignInManager.PerformPasskeyAttestationAsync(credentialJson)`, then `UserManager.AddOrUpdatePasskeyAsync(user, result.Passkey)` on success. Call `.DisableAntiforgery()`. |
| `PasskeyLoginBeginEndpoint` | `POST /api/passkeys/login/begin` | Anonymous | Calls `SignInManager.MakePasskeyRequestOptionsAsync()`, returns JSON options to browser. Call `.DisableAntiforgery()`. |
| `PasskeyLoginCompleteEndpoint` | `POST /api/passkeys/login/complete` | Anonymous | Calls `SignInManager.PasskeySignInAsync(credentialJson)`, sets auth cookie on success, returns redirect to `returnUrl`. Call `.DisableAntiforgery()`. |
| `GetPasskeysEndpoint` | `GET /api/passkeys` | Required | Returns list of registered passkeys via `UserManager.GetPasskeysAsync()` |
| `DeletePasskeyEndpoint` | `DELETE /api/passkeys/{credentialId}` | Required | Calls `UserManager.RemovePasskeyAsync()`, validates passkey belongs to requesting user |

> **Note on antiforgery:** All four POST passkey endpoints receive JSON via `fetch()` (not Inertia form submissions) and must call `.DisableAntiforgery()` on their routes to avoid HTTP 400 rejections from the antiforgery middleware.

> **Note on API ownership:** `MakePasskeyCreationOptionsAsync`, `MakePasskeyRequestOptionsAsync`, `PerformPasskeyAttestationAsync`, and `PasskeySignInAsync` are on `SignInManager`. `AddOrUpdatePasskeyAsync`, `GetPasskeysAsync`, and `RemovePasskeyAsync` are on `UserManager`.

> **Note on challenge storage:** The .NET 10 implementation stores the challenge in an encrypted/signed authentication cookie, not in ASP.NET Core Session. No `AddSession()` / `UseSession()` call is required.

**1 new `IViewEndpoint`** — `ManagePasskeysEndpoint` at `GET /Users/Account/Manage/Passkeys`, renders Inertia component `"Users/Account/Manage/Passkeys"`.

**1 updated `IViewEndpoint`** — existing `LoginEndpoint` passes a `passkeySupported` flag (derived from whether `IdentityPasskeyOptions.ServerDomain` is configured, not per-user passkey presence — the user is not yet identified at login time).

### Frontend Components

**`passkey.ts`** — shared utility handling:
- Base64url encoding/decoding (required by WebAuthn API)
- `startPasskeyRegistration(options)` — wraps `navigator.credentials.create()`
- `startPasskeyAssertion(options)` — wraps `navigator.credentials.get()`

> **CSP note:** WebAuthn does not make network requests itself, so the existing `connect-src 'self'` CSP does not block the passkey flow. The `fetch()` calls in `passkey.ts` go to `'self'` endpoints and are also unaffected.

**`ManagePasskeys.tsx`** — new Inertia page at route `Users/Account/Manage/Passkeys`:
- Table of registered passkeys: name, device type hint, registered date
- "Add passkey" button — triggers registration flow
- Delete button per passkey with confirmation dialog

**`Login.tsx`** — updated to add a "Sign in with passkey" button below the existing password form (shown only when `passkeySupported` prop is true):
1. POST `/api/passkeys/login/begin` → receive options
2. `navigator.credentials.get({ publicKey: options })`
3. POST `/api/passkeys/login/complete` → redirect on success

**`Pages/index.ts`** — add entry (key must match the string passed to `Inertia.Render(...)` in the C# endpoint):
```ts
"Users/Account/Manage/Passkeys": () => import("./ManagePasskeys"),
```

**Menu** — add "Passkeys" link to the Security section of the account management sidebar (alongside "Two-factor authentication"). The sidebar nav lives in `modules/Users/src/SimpleModule.Users/components/ManageLayout.tsx`.

---

## Data Model

Identity Schema Version 3 adds `AspNetUserPasskeys` automatically:

| Column | Type | Notes |
|---|---|---|
| `UserId` | string | FK → `AspNetUsers.Id` |
| `CredentialId` | byte[] | PK |
| `Name` | string | User-assigned label |
| `PublicKey` | byte[] | COSE-encoded public key |
| `SignCount` | long | Replay attack counter |
| `Transports` | string[] | Browser transport hints |
| `CreatedAt` | DateTimeOffset | Registration timestamp |
| `LastUsedAt` | DateTimeOffset | Last successful assertion |

One EF Core migration generated after the schema version bump. No manual entity definitions needed.

---

## Configuration

Add to `appsettings.json`:

```json
"Passkeys": {
  "ServerDomain": "localhost"
}
```

`ServerDomain` maps to `IdentityPasskeyOptions.ServerDomain` — the WebAuthn Relying Party ID. Must exactly match the origin domain. Passkeys registered on one domain cannot be used on another.

- Development: `localhost`
- Production: the actual domain (e.g. `yourdomain.com`)

---

## Request Flows

### Registration

```
ManagePasskeys page
  → POST /api/passkeys/register/begin
  ← PublicKeyCredentialCreationOptions (challenge stored in encrypted auth cookie)
  → navigator.credentials.create({ publicKey: options })
  ← AuthenticatorAttestationResponse (from device)
  → POST /api/passkeys/register/complete (credential JSON)
  ← 200 OK + updated passkey list
```

### Authentication

```
Login page
  → POST /api/passkeys/login/begin
  ← PublicKeyCredentialRequestOptions (challenge stored in encrypted auth cookie)
  → navigator.credentials.get({ publicKey: options })
  ← AuthenticatorAssertionResponse (from device)
  → POST /api/passkeys/login/complete (credential JSON)
  ← 200 OK + redirect to returnUrl (auth cookie set)
```

---

## Error Handling

- **Browser doesn't support WebAuthn**: `window.PublicKeyCredential` check — hide passkey button if unsupported
- **User cancels biometric prompt**: `navigator.credentials.get()` rejects — show "Passkey sign-in cancelled" message, do not redirect
- **Challenge mismatch / signature failure**: `PasskeySignInAsync` returns `SignInResult.Failed` — return 401, show error on login page
- **No passkeys registered**: `GetPasskeysEndpoint` returns empty list — manage page shows "No passkeys registered yet" with an add button

---

## Security Considerations

- Challenge stored in an encrypted/signed auth cookie by the .NET 10 passkey implementation — prevents replay attacks
- `SignCount` incremented and validated on each assertion — detects cloned credentials
- `ServerDomain` (RP ID) scoped to origin domain — prevents cross-site credential phishing
- All POST passkey endpoints call `.DisableAntiforgery()` — they are JSON API endpoints receiving `fetch()` requests, not form submissions
- Delete endpoint requires authentication and validates passkey belongs to the requesting user
- `passkeySupported` flag on login page reflects server-level config only — no per-user passkey lookup on anonymous login GET

---

## Testing

- Unit tests for each new endpoint (using existing `SimpleModuleWebApplicationFactory`)
- Registration flow: begin → browser mock → complete → verify passkey in DB
- Login flow: begin → browser mock → complete → verify auth cookie set
- Delete: verify only owner can delete their own passkeys
- Error cases: invalid credential, wrong user, missing session challenge

---

## Files to Create / Modify

### New files
- `modules/Users/src/SimpleModule.Users/Endpoints/Passkeys/PasskeyRegisterBeginEndpoint.cs`
- `modules/Users/src/SimpleModule.Users/Endpoints/Passkeys/PasskeyRegisterCompleteEndpoint.cs`
- `modules/Users/src/SimpleModule.Users/Endpoints/Passkeys/PasskeyLoginBeginEndpoint.cs`
- `modules/Users/src/SimpleModule.Users/Endpoints/Passkeys/PasskeyLoginCompleteEndpoint.cs`
- `modules/Users/src/SimpleModule.Users/Endpoints/Passkeys/GetPasskeysEndpoint.cs`
- `modules/Users/src/SimpleModule.Users/Endpoints/Passkeys/DeletePasskeyEndpoint.cs`
- `modules/Users/src/SimpleModule.Users/Pages/Account/Manage/ManagePasskeysEndpoint.cs`
- `modules/Users/src/SimpleModule.Users/Pages/Account/Manage/ManagePasskeys.tsx`
- `modules/Users/src/SimpleModule.Users/Pages/passkey.ts`
- EF Core migration for Identity Schema Version 3

### Modified files
- `modules/Users/src/SimpleModule.Users/UsersModule.cs` — configure `IdentityPasskeyOptions` via `services.Configure<IdentityPasskeyOptions>()`
- `modules/Users/src/SimpleModule.Users/UsersDbContext.cs` — set Identity schema to Version 3
- `modules/Users/src/SimpleModule.Users/Pages/Account/Login.tsx` — add passkey sign-in button (conditional on `passkeySupported` prop)
- `modules/Users/src/SimpleModule.Users/Pages/index.ts` — register `"Users/Account/Manage/Passkeys"` page
- `modules/Users/src/SimpleModule.Users/components/ManageLayout.tsx` — add Passkeys nav link in Security section
- `template/SimpleModule.Host/appsettings.json` — add `Passkeys` config section
- `template/SimpleModule.Host/appsettings.Development.json` — add dev passkey config (`ServerDomain: "localhost"`)
