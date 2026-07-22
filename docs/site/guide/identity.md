---
outline: deep
---

# Identity & Sessions

SimpleModule supports pluggable identity providers. The default is **OpenIddict** (self-hosted OAuth2/OIDC server). An alternative **Keycloak** module delegates authentication to an external Keycloak instance.

## Choosing an Identity Provider

Set `Identity:Provider` in configuration to switch providers:

| Value | Provider | Use case |
|-------|----------|----------|
| *(empty/omitted)* | OpenIddict | Self-contained apps, no external dependencies |
| `Keycloak` | Keycloak | SSO, social login, MFA, LDAP federation, enterprise IdP |

Both modules can coexist in the same Host project — only the configured one activates at startup.

```json
// appsettings.json — Keycloak mode
{
  "Identity": {
    "Provider": "Keycloak"
  },
  "Keycloak": {
    "Authority": "http://localhost:8080/realms/simplemodule",
    "ClientId": "simplemodule-app",
    "ClientSecret": "your-client-secret",
    "Realm": "simplemodule",
    "AdminApiBaseUrl": "http://localhost:8080/admin/realms/simplemodule",
    "AdminClientId": "simplemodule-admin",
    "AdminClientSecret": "your-admin-secret",
    "RequireHttpsMetadata": true
  }
}
```

## Architecture

### Provider-Agnostic Contracts (Identity.Contracts)

All modules depend on `SimpleModule.Identity.Contracts`, never on a specific provider:

```csharp
// Provider-agnostic session management
public interface ISessionContracts
{
    Task<IReadOnlyList<SessionDto>> GetActiveSessionsForUserAsync(string userId, ...);
    Task<RevokeSessionResult> TryRevokeSessionForUserAsync(string tokenId, string userId, ...);
    Task RevokeAllSessionsForUserAsync(string userId, ...);
    Task RevokeOtherSessionsForUserAsync(string userId, string? currentTokenId, ...);
}

// Provider metadata
public interface IIdentityProvider
{
    string Name { get; }
    bool SupportsLocalUsers { get; }
}
```

`SessionDto` carries `TokenId`, `Type`, `ApplicationName`, `CreationDate`, `ExpirationDate`, and `IsCurrent`.

`TryRevokeSessionForUserAsync` returns `RevokeSessionResult.NotFound` for unknown or cross-user tokens and `BlockedCurrent` when the caller tries to revoke their own session.

### Smart Authentication

Both providers use a "SmartAuth" policy scheme that selects the authentication handler per-request:

| Request | OpenIddict | Keycloak |
|---------|-----------|----------|
| `Authorization: Bearer <token>` | OpenIddict validation | JWT Bearer (Keycloak-issued) |
| Cookie (browser/Inertia) | ASP.NET Identity cookie | OIDC cookie (Keycloak redirect) |

### Users Module Dual-Mode

The Users module adapts automatically based on the active provider:

| Aspect | OpenIddict mode | Keycloak mode |
|--------|----------------|---------------|
| User store | ASP.NET Identity (local DB) | ASP.NET Identity (local DB) + JIT sync from Keycloak |
| Login pages | Local Inertia views | Redirect to Keycloak |
| Password management | Local | Keycloak |
| `IUserContracts` | `UserService` (via `UserManager`) | `ExternalUserService` (direct EF) |
| Admin user management | Full CRUD | Read-only (mutations throw `NotSupportedException`) |

## OpenIddict (Default)

Self-hosted OAuth2/OIDC server. No external dependencies.

### Grant Types

- **Authorization Code + PKCE** — standard browser flow
- **Refresh Token** — token renewal
- **Password Grant** — development/load testing only (set `OpenIddict:AllowPasswordGrant: true`)

### Certificate Management

Production expects signing and encryption certificates (PKCS#12, one shared password):

```json
{
  "OpenIddict": {
    "SigningCertificatePath": "/certs/signing.pfx",
    "EncryptionCertificatePath": "/certs/encryption.pfx",
    "CertificatePassword": "your-cert-password"
  }
}
```

Development uses ephemeral keys automatically. A real deployment without certificates still
starts, but falls back to ephemeral keys and logs a warning — every restart then regenerates
the keys, invalidating all issued tokens and signing everyone out. Configuring only one of the
two certificate paths fails startup (the pair only works together). Configure certificates for
anything beyond a throwaway deployment.

### OpenIddict Session Management

Sessions are exposed via `IOpenIddictSessionContracts` (extends `ISessionContracts`). Tokens sharing an `AuthorizationId` collapse into a single session row so users can't revoke half of their own login.

## Keycloak

Delegates authentication to an external [Keycloak](https://www.keycloak.org/) server.

### Keycloak Configuration

| Setting | Description |
|---------|-------------|
| `Keycloak:Authority` | Realm URL, e.g. `https://keycloak.example.com/realms/simplemodule` |
| `Keycloak:ClientId` | Application client ID (confidential, auth code + PKCE) |
| `Keycloak:ClientSecret` | Application client secret |
| `Keycloak:Realm` | Realm name |
| `Keycloak:AdminApiBaseUrl` | Admin REST API URL, e.g. `https://keycloak.example.com/admin/realms/simplemodule` |
| `Keycloak:AdminClientId` | Service account client ID for admin API |
| `Keycloak:AdminClientSecret` | Service account client secret |
| `Keycloak:RequireHttpsMetadata` | `true` in production, `false` for local dev |

### Claims Transformation

Keycloak uses non-standard claim structures. `KeycloakClaimsTransformation` normalizes them before `PermissionClaimsTransformation` runs:

| Keycloak claim | Mapped to |
|---------------|-----------|
| `realm_access.roles` (JSON) | Individual `ClaimTypes.Role` claims |
| `preferred_username` | `ClaimTypes.Name` |
| `sub` | Used as-is (same as OpenIddict) |

### JIT User Provisioning

When a Keycloak user first authenticates, `KeycloakUserSyncService` creates a local shadow `ApplicationUser` record with `Id = Keycloak sub`. On subsequent logins, email and display name are updated if they changed in Keycloak.

This ensures local modules (permissions, audit logs, settings) can reference users by ID without depending on the Keycloak API.

### Session Management

`KeycloakSessionService` implements `ISessionContracts` via the [Keycloak Admin REST API](https://www.keycloak.org/docs-api/latest/rest-api/index.html). Token management for the admin API uses a singleton `KeycloakTokenCache` with thread-safe double-checked locking.

### Sign Out Everywhere

The Keycloak module handles `UserSignedOutEverywhereEvent` (published by the Users module) by calling `RevokeAllSessionsForUserAsync`, which maps to `POST /admin/realms/{realm}/users/{userId}/logout` on the Keycloak Admin API.

## Development with Aspire

The Aspire AppHost includes a Keycloak launch profile:

```bash
# Default (OpenIddict)
dotnet run --project SimpleModule.AppHost

# Keycloak mode
dotnet run --project SimpleModule.AppHost --launch-profile keycloak
```

The `keycloak` profile starts a Keycloak 26.2 container with a pre-imported realm containing:

| Test User | Password | Roles |
|-----------|----------|-------|
| `admin@simplemodule.dev` | `Admin123!` | Admin, User |
| `user@simplemodule.dev` | `User123!` | User |

Keycloak Admin Console: `http://localhost:8080` (admin/admin)

The realm import JSON is at `SimpleModule.AppHost/keycloak/simplemodule-realm.json`.

## Account lockout and self-service unlock

When ASP.NET Identity locks an account after repeated failed logins the user is redirected to `/Identity/Account/Lockout`. From there `Send unlock email` posts to `/Identity/Account/SendUnlockEmail`, which:

1. Resolves the user by email (silently no-ops on miss to avoid enumeration).
2. Generates a single-use token bound to the user and the `AccountUnlock` purpose.
3. Calls `IAccountUnlockEmailSender.SendUnlockLinkAsync(email, unlockLink)`.

Clicking the link validates the token, clears the lockout, and signs the user out for re-authentication.

`IAccountUnlockEmailSender` defaults to `ConsoleAccountUnlockEmailSender` (logs the link). Replace it with a production implementation:

```csharp
builder.Services.AddScoped<IAccountUnlockEmailSender, MailgunAccountUnlockEmailSender>();
```

## Phone number confirmation

The account manage page uses `ISmsSender` for phone verification codes. Default `ConsoleSmsSender` logs to console. Provide a Twilio/Vonage/AWS SNS implementation for production.

## Two-factor authentication and recovery codes

Users manage two-factor authentication from `/Identity/Account/Manage/TwoFactorAuthentication`. Enabling an authenticator app walks through setup → verify a code → view recovery codes. Recovery codes let a user sign in if they lose their authenticator device.

### Generating recovery codes

The generate flow lives at `/Identity/Account/Manage/GenerateRecoveryCodes`:

| Method | Behaviour |
|--------|-----------|
| `GET` | Renders the confirmation page (2FA must be enabled) |
| `POST` | Generates 10 new codes via `UserManager.GenerateNewTwoFactorRecoveryCodesAsync` and renders them once |

Codes are **hashed at rest**, exactly like passwords — the plaintext is shown only at generation time. There is no "retrieve codes" API by design; if a user loses them, they regenerate, which invalidates the previous set.

### Download and print

The page that shows freshly generated codes (`ShowRecoveryCodes`) offers two ways to save them off-screen:

- **Download** — builds a `simplemodule-recovery-codes.txt` file in the browser with a header line (`generated for {email} on {date}`) followed by one code per line.
- **Print** — calls `window.print()` with a scoped `@media print` stylesheet that hides page chrome and prints only the codes as black-on-white monospace.

Both run entirely client-side; the codes never make a second round-trip to the server.

### Remaining-codes status

The two-factor page reflects how many codes are left:

| Codes remaining | Treatment |
|-----------------|-----------|
| 4 or more | Neutral status line ("Recovery codes: N remaining") |
| 2–3 | Warning alert with a link to regenerate |
| 0–1 | Danger alert with a link to regenerate |

## Next Steps

- [Permissions](/guide/permissions) — claims-based authorization layered on top of identity.
- [Notifications](/guide/notifications) — channel the unlock and verification messages through a unified pipeline.
- [Settings](/guide/settings) — toggle lockout policy thresholds without redeploying.
