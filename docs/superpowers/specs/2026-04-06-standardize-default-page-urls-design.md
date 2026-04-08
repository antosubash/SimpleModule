# Standardize Default Page URLs

**Date:** 2026-04-06
**Convention:** Hybrid by audience — public/user-facing modules use root URL, admin-only modules use explicit action suffix.

## Changes

### Public/user-facing: shorten to root

| Module | Before | After | Constant |
|--------|--------|-------|----------|
| Products | `/products/browse` | `/products/` | `ProductsConstants.Routes.Browse = "/"` |
| Marketplace | `/marketplace/browse` | `/marketplace/` | `MarketplaceConstants.Routes.Browse = "/"` |
| FileStorage | `/files/browse` | `/files/` | `FileStorageConstants.Routes.Browse = "/"` |

### Admin-only: add explicit action

| Module | Before | After | Constant |
|--------|--------|-------|----------|
| FeatureFlags | `/feature-flags/` | `/feature-flags/manage` | `FeatureFlagsConstants.Routes.Manage = "/manage"` |
| RateLimiting | `/rate-limiting/` | `/rate-limiting/manage` | `RateLimitingConstants.Routes.Admin = "/manage"` |
| Settings | `/settings/` | `/settings/manage` | `SettingsConstants.Routes.Views.AdminSettings = "/manage"` |

## Files to Change Per Module

### 1. Route constant (source of truth)
- `modules/{Module}/src/SimpleModule.{Module}.Contracts/{Module}Constants.cs`

### 2. Menu URLs in module class
- `modules/{Module}/src/SimpleModule.{Module}/{Module}Module.cs`
- Products: 2 menu items reference `/products/browse` (Navbar + AppSidebar)
- Marketplace: 2 menu items reference `/marketplace/browse` (Navbar + AppSidebar)
- FileStorage: 1 menu item references `/files/browse`
- FeatureFlags: 1 menu item references `/feature-flags`
- RateLimiting: 1 menu item references `/rate-limiting`
- Settings: 1 menu item references `/settings` (AppSidebar)

### 3. Auto-generated routes.ts
- `packages/SimpleModule.Client/src/routes.ts` — regenerated via `npm run generate:routes` after `dotnet build`

### 4. E2E page objects
- `tests/e2e/pages/products/browse.page.ts` — `/products/browse` -> `/products/`
- `tests/e2e/pages/marketplace/browse.page.ts` — `/marketplace/browse` -> `/marketplace/`
- `tests/e2e/pages/feature-flags/manage.page.ts` — `/feature-flags` -> `/feature-flags/manage`
- `tests/e2e/pages/rate-limiting/admin.page.ts` — `/rate-limiting` -> `/rate-limiting/manage`
- `tests/e2e/pages/settings/admin.page.ts` — `/settings` -> `/settings/manage`

### 5. E2E test specs
- `tests/e2e/tests/flows/permissions.spec.ts` — `/products/browse`
- `tests/e2e/tests/smoke/filestorage.spec.ts` — `/files/browse` (4 references)
- `tests/e2e/tests/flows/filestorage-crud.spec.ts` — `/files/browse` (4 references)

## No changes needed
- Inertia page names (e.g., `Products/Browse`) stay the same — only URLs change
- `Pages/index.ts` files in each module — unchanged
- API endpoints — unchanged
- Integration test files in `*.Tests/` projects — no URL references found
