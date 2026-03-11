# User & Role Management Admin UI

## Overview

React-based admin panel for managing users and roles, served via Inertia protocol from the Users module. All endpoints gated with `RequireRole("Admin")`.

## Pages

### Users
- **Admin/Users** — Paginated table with search by name/email. Columns: display name, email, roles, email confirmed, locked out, created date. Actions: edit, lock/unlock.
- **Admin/Users/Edit** — Edit form: display name, email, email confirmed toggle, role assignment (multi-select), lock/unlock account.

### Roles
- **Admin/Roles** — Table listing all roles with description, user count, created date. Actions: edit, delete.
- **Admin/Roles/Create** — Form: name, description.
- **Admin/Roles/Edit** — Form: name, description. Shows assigned users.

## API Endpoints

All under `/admin`, require Admin role.

```
GET    /admin/users                → Inertia: Admin/Users (paginated, ?search=&page=)
GET    /admin/users/{id}/edit      → Inertia: Admin/Users/Edit
POST   /admin/users/{id}          → Update user (display name, email, emailConfirmed)
POST   /admin/users/{id}/roles    → Set roles (replaces all)
POST   /admin/users/{id}/lock     → Lock account
POST   /admin/users/{id}/unlock   → Unlock account

GET    /admin/roles               → Inertia: Admin/Roles
GET    /admin/roles/create        → Inertia: Admin/Roles/Create
POST   /admin/roles               → Create role
GET    /admin/roles/{id}/edit     → Inertia: Admin/Roles/Edit
POST   /admin/roles/{id}         → Update role
DELETE /admin/roles/{id}          → Delete role (fails if users assigned)
```

## Data Flow

Endpoints use `UserManager<ApplicationUser>` and `RoleManager<ApplicationRole>` directly. Props passed to React via `Inertia.Render()`. Form submissions POST back, then redirect via Inertia.

## File Changes

| Action | File |
|--------|------|
| New | `Users/Pages/Admin/Users.tsx` |
| New | `Users/Pages/Admin/UsersEdit.tsx` |
| New | `Users/Pages/Admin/Roles.tsx` |
| New | `Users/Pages/Admin/RolesCreate.tsx` |
| New | `Users/Pages/Admin/RolesEdit.tsx` |
| New | `Users/Pages/index.ts` |
| New | `Users/Features/Admin/AdminUsersEndpoint.cs` |
| New | `Users/Features/Admin/AdminRolesEndpoint.cs` |
| Modify | `UsersModule.cs` — register admin endpoints |
| Modify | `Users/vite.config.ts` — add pages build |
| Modify | `Users/package.json` — add react peer deps |

## Decisions

- No new contracts/DTOs — admin endpoints use UserManager/RoleManager directly with anonymous objects for Inertia props.
- Existing Blazor self-service pages (login, register, manage profile, 2FA) remain untouched.
- Admin role + seed user already exist via OpenIddictSeedService.
