# Admin Module Design

## Overview

Separate Admin module for full user and role management with permissions, security controls, soft delete, and audit logging. Extracted from the Users module to provide clean separation of concerns — Users handles authentication/identity, Admin handles administration.

## Module Structure

```
modules/Admin/
├── src/Admin.Contracts/
│   ├── IAdminContracts.cs
│   ├── AdminUserDto.cs
│   ├── AdminRoleDto.cs
│   ├── AuditLogEntryDto.cs
│   ├── CreateAdminUserRequest.cs
│   └── UpdateAdminUserRequest.cs
├── src/Admin/
│   ├── AdminModule.cs
│   ├── AdminDbContext.cs
│   ├── Entities/
│   │   └── AuditLogEntry.cs
│   ├── Services/
│   │   ├── AdminService.cs
│   │   └── AuditService.cs
│   ├── Endpoints/Admin/
│   ├── Views/Admin/
│   ├── Pages/Admin/
│   ├── Pages/components/
│   ├── vite.config.ts
│   └── package.json
└── tests/Admin.Tests/
```

## Dependencies

- `Users.Contracts` — `IUserContracts`, `UserDto`, `UserId`
- `Users` (direct) — `UserManager<ApplicationUser>`, `RoleManager<ApplicationRole>`, Identity entities
- `SimpleModule.Core` — `PermissionRegistry`, `IModule`, `IEndpoint`, `[Dto]`

## Data Model

### AuditLogEntry (new entity, owned by Admin module)

| Field | Type | Purpose |
|-------|------|---------|
| Id | long | PK, auto-increment |
| UserId | string | Target user |
| PerformedByUserId | string | Admin who performed action |
| Action | string | Action type (e.g., "RoleAdded", "PasswordReset") |
| Details | string? | JSON context (role name, old/new values) |
| Timestamp | DateTimeOffset | When it happened |

### ApplicationUser Change (in Users module)

Add `DeactivatedAt` (`DateTimeOffset?`) to `ApplicationUser`. Null = active, non-null = soft-deleted.

## Audit Actions Tracked

- `UserCreated`, `UserUpdated`, `UserDeactivated`, `UserReactivated`
- `RoleAdded`, `RoleRemoved`
- `PermissionGranted`, `PermissionRevoked`
- `PasswordReset`, `AccountLocked`, `AccountUnlocked`
- `EmailReverified`, `TwoFactorDisabled`
- `RoleCreated`, `RoleUpdated`, `RoleDeleted`
- `RolePermissionAdded`, `RolePermissionRemoved`

## Soft Delete

- `DeactivatedAt` field on `ApplicationUser` (nullable DateTimeOffset)
- Deactivated users cannot log in (checked during authentication)
- Appear in admin list with "Deactivated" status
- Can be reactivated by admin
- Excluded from `IUserContracts.GetAllUsersAsync()` by default

## Pages & Tabs

### Users List (`/admin/users`)

- Search bar (name/email), pagination
- Table: Name, Email, Roles (badges), Status (Active/Deactivated/Locked), Created
- "Create User" button in header
- Edit button per row

### User Create (`/admin/users/create`)

- Form: Display Name, Email, Password, Confirm Password, Email Confirmed toggle, Role checkboxes
- "Create User" submit

### User Edit (`/admin/users/{id}/edit?tab=`)

4 tabs:

**Details** — Display Name, Email, Email Confirmed toggle. Save button.

**Roles & Permissions** — Role checkbox list + Direct Permissions grouped checkboxes by module (from `PermissionRegistry.ByModule`). Save button per section.

**Security** — Reset Password (new password + confirm), Lock/Unlock, Force Email Re-verification, Disable 2FA/Reset Authenticator, Failed login attempts + last login (read-only).

**Activity** — Audit log timeline. Each entry: action icon, description, performed by, relative timestamp. Paginated with "Load more."

### Roles List (`/admin/roles`)

- Table: Name, Description, Users (badge), Permissions (badge), Created
- Create Role button, Edit/Delete per row

### Role Create (`/admin/roles/create`)

- Form: Name, Description, Permission grouped checkboxes with select-all per module

### Role Edit (`/admin/roles/{id}/edit?tab=`)

3 tabs:

**Details** — Name, Description. Save button.

**Permissions** — Grouped checkboxes by module with select-all per group. Save button.

**Users** — Read-only list of assigned users with link to their edit page.

## Backend Endpoints

### User Endpoints (`/admin/users`)

| Method | Route | Purpose |
|--------|-------|---------|
| GET | `/admin/users` | View: User list (Inertia) |
| GET | `/admin/users/create` | View: Create form (Inertia) |
| POST | `/admin/users` | Action: Create user |
| GET | `/admin/users/{id}/edit` | View: Edit page with tabs (Inertia) |
| POST | `/admin/users/{id}` | Action: Update details |
| POST | `/admin/users/{id}/roles` | Action: Set roles |
| POST | `/admin/users/{id}/permissions` | Action: Set direct permissions |
| POST | `/admin/users/{id}/reset-password` | Action: Reset password |
| POST | `/admin/users/{id}/lock` | Action: Lock account |
| POST | `/admin/users/{id}/unlock` | Action: Unlock account |
| POST | `/admin/users/{id}/force-reverify` | Action: Clear email confirmed |
| POST | `/admin/users/{id}/disable-2fa` | Action: Disable 2FA |
| POST | `/admin/users/{id}/deactivate` | Action: Soft delete |
| POST | `/admin/users/{id}/reactivate` | Action: Restore |
| GET | `/admin/users/{id}/activity` | View: Activity log (Inertia partial) |

### Role Endpoints (`/admin/roles`)

| Method | Route | Purpose |
|--------|-------|---------|
| GET | `/admin/roles` | View: Role list (Inertia) |
| GET | `/admin/roles/create` | View: Create form (Inertia) |
| POST | `/admin/roles` | Action: Create role with permissions |
| GET | `/admin/roles/{id}/edit` | View: Edit page with tabs (Inertia) |
| POST | `/admin/roles/{id}` | Action: Update details |
| POST | `/admin/roles/{id}/permissions` | Action: Set permissions |
| DELETE | `/admin/roles/{id}` | Action: Delete role |

## Authorization

All endpoints require `RequireRole("Admin")`. Module defines permissions `Admin.ManageUsers`, `Admin.ManageRoles`, `Admin.ViewAuditLog` for future granularity.

## Frontend Components

### Reusable

- `PermissionGroups.tsx` — grouped checkboxes by module with select-all
- `ActivityTimeline.tsx` — audit log timeline with icons
- `TabNav.tsx` — URL-driven tab navigation (`?tab=` query param)

### Tab state

URL-driven via `?tab=details|roles|security|activity`. Tabs are linkable and bookmarkable. Form submissions preserve the current tab via redirect.

## Migration from Users Module

Remove from Users module:
- `Endpoints/Admin/AdminUsersEndpoint.cs`
- `Endpoints/Admin/AdminRolesEndpoint.cs`
- `Views/Admin/` (all 5 endpoints)
- `Pages/Admin/` (all 5 React pages)
- Menu items for "Manage Users" / "Manage Roles"

Users module keeps: authentication, OAuth/OpenIddict, account self-service (2FA, personal data), API endpoints (`/api/users/*`).
