# OpenIddict & Permissions Module Split

**Date**: 2026-03-19
**Branch**: feature/permission-system
**Goal**: Extract OpenIddict and permissions from Users module into two independent modules.

## Current State

The Users module currently owns:
- ASP.NET Identity (ApplicationUser, ApplicationRole, UsersDbContext)
- OpenIddict OAuth2 (authorization code flow, token endpoints, client seeding)
- Permission entities (UserPermission, RolePermission)
- Permission seeding
- User CRUD (IUserContracts)

This is too much responsibility in one module.

## Design

### Users Module (slimmed down)

Keeps only identity concerns:
- `ApplicationUser`, `ApplicationRole` entities
- `UsersDbContext` — Identity tables only (no OpenIddict, no permission tables)
- User CRUD endpoints + `IUserContracts`
- `ConsoleEmailSender`
- Login/register view logic

### OpenIddict Module (new)

Owns all OAuth2/OIDC infrastructure:
- `OpenIddictDbContext` — OpenIddict application/token/authorization/scope tables
- Connect endpoints: authorize, token, logout, userinfo
- `OpenIddictSeedService` — seeds client applications, dev certs
- Token generation: calls `IPermissionContracts.GetPermissionsForUserAsync(userId)` to issue permission claims
- `OpenIddict.Contracts` — auth constants (OAuth2Scheme, scopes, etc.)
- **Depends on**: `Users.Contracts`, `Permissions.Contracts`

### Permissions Module (new)

Owns permission data storage and queries:
- `PermissionsDbContext` — `UserPermission`, `RolePermission` tables (string-to-string mappings)
- `PermissionSeedService` — seeds Admin role permissions at startup using `PermissionRegistry`
- `Permissions.Contracts` — `IPermissionContracts` interface:
  - `GetPermissionsForUserAsync(string userId)` — combined user + role permissions
  - `GetPermissionsForRoleAsync(string roleId)` — role permissions
  - `SetPermissionsForRoleAsync(string roleId, IEnumerable<string> permissions)`
  - `SetPermissionsForUserAsync(string userId, IEnumerable<string> permissions)`
- **Depends on**: nothing (role IDs and permissions are plain strings)

## Dependency Graph

```
Admin ──→ Users.Contracts
Admin ──→ Permissions.Contracts
OpenIddict ──→ Users.Contracts
OpenIddict ──→ Permissions.Contracts
Permissions ──→ (nothing)
Users ──→ (nothing)
```

## What Stays in Core (unchanged)

- `PermissionRegistry` / `PermissionRegistryBuilder`
- `PermissionAuthorizationHandler`
- `RequirePermissionAttribute`
- `IModule.ConfigurePermissions()`

## Key Decisions

- **Permissions are const strings** — defined in each module's Contracts (e.g., `AdminPermissions.ManageUsers = "Admin.ManageUsers"`).
- **Each module gets its own DbContext** — follows existing project convention.
- **RolePermission/UserPermission use plain string IDs** — no entity dependency on Users module. The Permissions module doesn't validate that a role exists.
- **OpenIddict calls IPermissionContracts during token generation** — direct contract dependency, not event-based.
