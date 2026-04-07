# Sidebar Reorganization — Admin Hub + Simplified Navigation

**Date:** 2026-04-06

## Problem

The sidebar has ~20 items for admin users (7 AppSidebar + 13 AdminSidebar). Most admin items are low-frequency config tools that don't warrant permanent sidebar slots. The UserDropdown has 3 granular account links that duplicate ManageLayout's sidebar. Some manage pages are missing the ManageLayout wrapper.

## Design

### 1. Sidebar (AppSidebar) — 7 items

| Order | Label | URL | Notes |
|-------|-------|-----|-------|
| 10 | Dashboard | `/` | Existing |
| 20 | Products | `/products` | Existing |
| 30 | Orders | `/orders` | Existing |
| 40 | Pages | `/pages` | Move from Navbar to AppSidebar |
| 50 | Files | `/files` | Existing |
| 80 | Settings | `/settings/me` | Changed from `/settings/manage` to user settings |
| 85 | Admin | `/admin` | **New** — Admin role only, links to hub page |

### 2. Items removed from sidebar

| Item | New location | Reason |
|------|-------------|--------|
| Marketplace | Navbar only (keep existing entry) | Public discovery, not daily workflow |
| Account | UserDropdown only | Accessed via avatar menu |
| All AdminSidebar items (13) | Admin hub page | Low-frequency config tools |

### 3. Admin Hub Page (`/admin`)

New `IViewEndpoint` in the Admin module at `GET /admin`. Renders an Inertia page `Admin/Hub` with a grid of card links grouped into sections.

**Identity**
- Users — `/admin/users` — Manage user accounts
- Roles — `/admin/roles` — Manage roles and permissions
- OAuth Clients — `/openiddict/clients` — Manage OAuth/OIDC applications
- Tenants — `/tenants/manage` — Manage tenants and hosts

**Content**
- Pages — `/pages/manage` — Manage published pages
- Email Templates — `/email/templates` — Create and edit email templates
- Email History — `/email/history` — View sent emails
- Menus — `/settings/menus` — Configure navigation menus

**System**
- Feature Flags — `/feature-flags/manage` — Toggle features on/off
- Rate Limiting — `/rate-limiting/manage` — Configure API rate limits
- Background Jobs — `/admin/jobs` — Monitor job queues
- Audit Logs — `/audit-logs/browse` — Review activity logs
- App Settings — `/settings/manage` — Application settings

Each card: icon (SVG), title, one-line description. Entire card is a link.

### 4. UserDropdown — simplified

Replace 3 granular account links with single entry:
- **Account Settings** → `/Identity/Account/Manage`
- *(divider)*
- **Logout** (existing)

### 5. ManageLayout consistency

Ensure ALL account manage pages use `ManageLayout` wrapper. Currently missing from:
- ManageIndex (Profile)
- Email
- ChangePassword
- SetPassword
- PersonalData
- DeletePersonalData
- ExternalLogins

## Files to change

### Module ConfigureMenu changes
- **Admin** — Remove Users/Roles from AdminSidebar; add Admin hub link to AppSidebar
- **AuditLogs** — Remove 2 AdminSidebar items
- **BackgroundJobs** — Remove AdminSidebar item
- **Email** — Remove 3 AdminSidebar items
- **FeatureFlags** — Remove AdminSidebar item
- **Marketplace** — Remove AppSidebar item (keep Navbar)
- **OpenIddict** — Remove AdminSidebar item
- **Orders** — Remove Navbar item (keep AppSidebar)
- **PageBuilder** — Remove Navbar item; change AdminSidebar to AppSidebar
- **Products** — Remove Navbar items (keep AppSidebar)
- **RateLimiting** — Remove AdminSidebar item
- **Settings** — Change AppSidebar URL to `/settings/me`; remove Menus AdminSidebar item
- **Tenants** — Remove AdminSidebar item
- **Users** — Replace 3 UserDropdown items with 1; remove Account from AppSidebar

### New files
- `modules/Admin/src/SimpleModule.Admin/Pages/HubEndpoint.cs` — Admin hub view endpoint
- `modules/Admin/src/SimpleModule.Admin/Views/Hub.tsx` — Admin hub React page

### Modified files
- `modules/Admin/src/SimpleModule.Admin/Pages/index.ts` — Register Hub page
- `packages/SimpleModule.UI/components/layouts/app-layout.tsx` — Remove AdminSection component (no longer needed)
- Account manage pages — Wrap with ManageLayout

### No changes
- API endpoints unchanged
- Route constants unchanged (already updated in previous task)
- Inertia page names unchanged
