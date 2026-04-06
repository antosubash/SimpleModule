# Passkey Login — Design Spec

**Date:** 2026-04-06  
**Status:** Approved  
**Scope:** Add optional passkey authentication to the existing Users module

---

## Overview

Add WebAuthn passkey support to SimpleModule as an optional authentication method alongside existing password login. Users can register one or more passkeys from their account security settings and use them as an alternative to entering a password. This uses the built-in .NET 10 passkey support (`Microsoft.AspNetCore.Authentication.WebAuthn`) with no third-party library dependencies.

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

**`UsersModule.ConfigureServices`** — calls `.AddPasskeys()` on the Identity builder and binds `PasskeyOptions` from configuration (RP domain, server name).

**`UsersDbContext`** — bumps Identity schema to `IdentitySchemaVersions.Version3`, which provisions the `AspNetUserPasskeys` table via a new EF Core migration.

**6 new `IEndpoint` classes** under `Endpoints/Passkeys/`:

| Class | Route | Auth | Purpose |
|---|---|---|---|
| `PasskeyRegisterBeginEndpoint` | `POST /api/passkeys/register/begin` | Required | Returns `PublicKeyCredentialCreationOptions` from `UserManager.MakePasskeyCreationOptionsAsync()` |
| `PasskeyRegisterCompleteEndpoint` | `POST /api/passkeys/register/complete` | Required | Receives browser credential, calls `UserManager.AddOrUpdatePasskeyAsync()` |
| `PasskeyLoginBeginEndpoint` | `POST /api/passkeys/login/begin` | Anonymous | Returns `PublicKeyCredentialRequestOptions` from `SignInManager.MakePasskeyRequestOptionsAsync()` |
| `PasskeyLoginCompleteEndpoint` | `POST /api/passkeys/login/complete` | Anonymous | Calls `SignInManager.PasskeySignInAsync()`, sets auth cookie, returns redirect |
| `GetPasskeysEndpoint` | `GET /api/passkeys` | Required | Returns list of registered passkeys for current user |
| `DeletePasskeyEndpoint` | `DELETE /api/passkeys/{credentialId}` | Required | Calls `UserManager.RemovePasskeyAsync()` |

**1 new `IViewEndpoint`** — `ManagePasskeysEndpoint` at `GET /Identity/Account/Manage/Passkeys`

**1 updated `IViewEndpoint`** — existing `LoginEndpoint` passes a flag indicating passkeys are supported so the frontend can render the passkey button.

### Frontend Components

**`passkey.ts`** — shared utility handling:
- Base64url encoding/decoding (required by WebAuthn API)
- `startPasskeyRegistration(options)` — wraps `navigator.credentials.create()`
- `startPasskeyAssertion(options)` — wraps `navigator.credentials.get()`

**`ManagePasskeys.tsx`** — new Inertia page at `Identity/Account/Manage/Passkeys`:
- Table of registered passkeys: name, device type hint, registered date
- "Add passkey" button — triggers registration flow
- Delete button per passkey with confirmation dialog

**`Login.tsx`** — updated to add a "Sign in with passkey" button below the existing password form:
1. POST `/api/passkeys/login/begin` → receive options
2. `navigator.credentials.get({ publicKey: options })`
3. POST `/api/passkeys/login/complete` → redirect on success

**`Pages/index.ts`** — add entry:
```ts
"Identity/Account/Manage/Passkeys": () => import("./ManagePasskeys"),
```

**Menu** — add "Passkeys" link to the Security section of the account management sidebar (alongside "Two-factor authentication").

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
  "ServerDomain": "localhost",
  "ServerName": "SimpleModule"
}
```

`ServerDomain` is the WebAuthn Relying Party ID — must exactly match the origin domain. Passkeys registered on one domain cannot be used on another.

- Development: `localhost`
- Production: the actual domain (e.g. `yourdomain.com`)

---

## Request Flows

### Registration

```
ManagePasskeys page
  → POST /api/passkeys/register/begin
  ← PublicKeyCredentialCreationOptions (challenge stored in session)
  → navigator.credentials.create({ publicKey: options })
  ← AuthenticatorAttestationResponse (from device)
  → POST /api/passkeys/register/complete (credential JSON)
  ← 200 OK + updated passkey list
```

### Authentication

```
Login page
  → POST /api/passkeys/login/begin
  ← PublicKeyCredentialRequestOptions (challenge stored in session)
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

- Challenge stored server-side in session (not in cookie) — prevents replay attacks
- `SignCount` incremented and validated on each assertion — detects cloned credentials
- `ServerDomain` (RP ID) scoped to origin domain — prevents cross-site credential phishing
- Delete endpoint requires authentication and validates passkey belongs to the requesting user

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
- EF Core migration for schema version 3

### Modified files
- `modules/Users/src/SimpleModule.Users/UsersModule.cs` — add `.AddPasskeys()` and `PasskeyOptions` binding
- `modules/Users/src/SimpleModule.Users/UsersDbContext.cs` — set `IdentitySchemaVersions.Version3`
- `modules/Users/src/SimpleModule.Users/Pages/Account/Login.tsx` — add passkey sign-in button
- `modules/Users/src/SimpleModule.Users/Pages/index.ts` — register `ManagePasskeys` page
- `modules/Users/src/SimpleModule.Users/Pages/Account/Manage/ManageLayout.tsx` (or equivalent sidebar) — add Passkeys nav link
- `template/SimpleModule.Host/appsettings.json` — add `Passkeys` config section
- `template/SimpleModule.Host/appsettings.Development.json` — add dev passkey config
