# SimpleModule Host Restructure Design

**Date:** 2026-03-13
**Goal:** Rename SimpleModule.Api → SimpleModule.Host and extract reusable concerns into focused packages.
**Approach:** Bottom-up — create all packages first, then rename Api → Host.

## New Packages

### 1. SimpleModule.Blazor (NuGet — Razor Class Library)

**Location:** `src/SimpleModule.Blazor/`

Composable Blazor SSR components and the Inertia page renderer.

**Contents:**
- `InertiaPageRenderer.cs` — Blazor SSR implementation of `IInertiaPageRenderer`
- **Composable components** (extracted from current `MainLayout.razor`):
  - `ModuleNav.razor` — renders menu items from `IMenuRegistry`
  - `UserDropdown.razor` — avatar, name, dropdown menu items, logout form
  - `DarkModeToggle.razor` — theme toggle button
- **Shell components:**
  - `InertiaShell.razor` — full HTML shell for Inertia pages (importmap, layout, page JSON)
  - `InertiaPage.razor` — `<div id="app">` + script tag
- `DarkModeScript.razor` — `applyTheme()`/`toggleTheme()`/MutationObserver inline script

**NOT included:** `App.razor`, `Routes.razor`, `MainLayout.razor` — these stay in Host (host-specific branding and assembly references).

**csproj:** `Microsoft.NET.Sdk.Razor`, references `SimpleModule.Core`, `<FrameworkReference Include="Microsoft.AspNetCore.App" />`.

### 2. @simplemodule/client (npm package)

**Location:** `src/SimpleModule.Client/`

Reusable Vite plugin and Inertia page resolver for the React frontend.

**Contents:**
- `vite-plugin-vendor.ts` — `vendorBuildPlugin()` made configurable (vendor list, output dir as parameters)
- `resolve-page.ts` — Inertia page resolver function (module name → dynamic import)
- `index.ts` — public exports
- `package.json` — peer deps on React, React-DOM, @inertiajs/react, vite, esbuild

**Host's ClientApp after extraction:**
- `app.tsx` → ~5 lines: imports `resolvePage` from `@simplemodule/client`
- `vite.config.ts` → ~10 lines: imports vendor plugin from `@simplemodule/client`

### 3. @simplemodule/theme-default (npm package)

**Location:** `src/SimpleModule.Theme.Default/`

Full design system CSS — theme variables, dark mode, component styles, utilities, animations.

**Contents:**
- `theme.css` — all 543 LOC from current `Styles/app.css`: `@theme` variables, dark mode overrides, base layer, component layer (glass-card, buttons, badges, alerts, code-block, panel, card, nav-link, spinner, user-dropdown, dash-card, validation), utilities (gradient-text, gradient-border), bg-mesh animation, table styling, scrollbar
- `package.json` — `@simplemodule/theme-default`

**Host's Styles after extraction:**
```css
@import '@simplemodule/theme-default/theme.css';
@source "../../modules/";
```

**Future themes:** Same pattern — `@simplemodule/theme-*`. Host swaps one import line.

### 4. Dashboard Module

**Location:** `src/modules/Dashboard/src/Dashboard/`

Extracted from current `Home.razor` (312 LOC). Converts Blazor SSR page → React/Inertia, consistent with all other modules.

**Contents:**
- `DashboardModule.cs` — `[Module("Dashboard", RoutePrefix = "dashboard")]`
- `Pages/Home.tsx` — landing page / dashboard UI
- OAuth PKCE flow, token tester, API tester as React components
- `Endpoints/` — API endpoints for dashboard features
- `vite.config.ts` + `package.json` — standard module Vite library build
- `Dashboard.Contracts/` — if cross-module communication is needed

## SimpleModule.Host (renamed from Api)

**Location:** `src/SimpleModule.Host/` (renamed from `src/SimpleModule.Api/`)

Thin host that assembles packages and modules.

**What stays:**
- `Program.cs` — bootstrap, service registration, middleware pipeline
- `appsettings.json` + `Properties/launchSettings.json`
- `Components/App.razor` — HTML shell (uses `<DarkModeScript />` from Blazor package)
- `Components/Routes.razor` — router with module assembly references
- `Components/Layout/MainLayout.razor` — assembles `<ModuleNav />`, `<UserDropdown />`, `<DarkModeToggle />` with host branding
- `Components/Pages/OAuthCallback.razor` — host-specific OAuth redirect
- `ClientApp/app.tsx` — slim, imports from `@simplemodule/client`
- `ClientApp/vite.config.ts` — slim, imports plugin from `@simplemodule/client`
- `Styles/app.css` — slim, imports from `@simplemodule/theme-default`
- `wwwroot/js/shell.js` — dropdown toggle logic

**References:** SimpleModule.Core, SimpleModule.Database, SimpleModule.Blazor, SimpleModule.Generator, all modules (Users, Products, Orders, Dashboard).

**csproj:** keeps `PublishAot`, Tailwind build target, Vite build target, TS type extraction.

## Solution-Wide Changes

- `SimpleModule.slnx` — remove `SimpleModule.Api`, add `SimpleModule.Host`, `SimpleModule.Blazor`, Dashboard module projects
- `CLAUDE.md` — update all references from `SimpleModule.Api` → `SimpleModule.Host`
- Root `package.json` — update workspace patterns if needed

## Execution Order (Bottom-Up)

1. Create `SimpleModule.Blazor` — extract components + renderer
2. Create `@simplemodule/client` — extract Vite plugin + page resolver
3. Create `@simplemodule/theme-default` — extract design system CSS
4. Create Dashboard module — convert Home.razor → React/Inertia
5. Rename `SimpleModule.Api` → `SimpleModule.Host` — rewire references, slim down files
6. Update solution file, CLAUDE.md, package.json workspaces
