# Branding Module — Design Spec

**Date:** 2026-06-25
**Status:** Draft (awaiting review)
**Goal:** A Branding module that lets an admin customize the appearance of a SimpleModule application — app name, logo, favicon, brand colors, custom CSS, a configurable top bar, and a configurable footer.

---

## 1. Decisions (locked)

| Decision | Choice | Rationale |
|---|---|---|
| Storage | **Layer on the existing Settings module** (no new DbContext) | Settings already has `Color`/`Text`/`MultilineText`/`Json` types, FusionCache-backed storage, and `Application` scope. Reuse it; don't duplicate infra. |
| Scope | **Global** (one config for the whole app) | Single-org deployment. `Scope.Application` settings. Extensible to per-tenant later. |
| Fields | App name, logo, favicon, brand colors, custom CSS, **top bar**, **footer** | Per user request. |
| Logo/favicon storage | **Reuse FileStorage module** | It already handles upload, serve-by-id, and permissions. Branding stores the returned file id in a setting. |

### Assumptions (confirm at review)

1. **"Top-navbar management" = a configurable top utility/announcement bar.** The authenticated layout (`app-layout.tsx`) has only a left sidebar today — there is no top navbar for logged-in users. So "top bar" is interpreted as a thin, full-width bar above the content (announcement message + optional links + background color + show/hide + dismissible), rendered in both the authenticated and public layouts. It is **not** about editing module-contributed sidebar menu items.
2. **v1 color control = primary color (light + dark only).** The theme defines `--color-primary`, `--color-primary-hover`, `--color-success`, `--color-danger`, `--color-surface`, `--color-text` — there is **no** `--color-accent`, so overriding an "accent" would be a no-op. `--color-primary` is the high-impact one (buttons, logo badge, progress bar, links), so v1 exposes only primary light + primary dark and leaves deeper theming to the custom-CSS box. (Default primary hex `#059669` — already the established fallback in `app.tsx`.)
3. **Favicon + brand-color application is server-side** (head injection) to avoid a flash of the default theme on first paint. This requires a small, generic framework addition (see §5).

---

## 2. Verified codebase facts (grounding)

- **Settings registration:** `IModule.ConfigureSettings(ISettingsBuilder settings)`; `settings.Add(new SettingDefinition { Key, DisplayName, Group, Scope, DefaultValue, Type, ... })`. `framework/SimpleModule.Core/Settings/`.
- **Setting types:** `Text, Number, Bool, Json, Select, Color, Url, Email, Password, MultilineText, DateTime`. Scopes: `System, Application, User`.
- **Read settings:** `ISettingsContracts` (`modules/Settings/.../Contracts`) — `GetSettingAsync<T>(key, scope, userId?)`, `SetManyAsync(IReadOnlyList<BulkSettingUpdate>)`, `GetSettingValuesAsync(filter)`, etc.
- **Shared Inertia props:** `InertiaSharedData.Set(key, value)` (`framework/SimpleModule.Core/Inertia/InertiaSharedData.cs`), populated by `InertiaLayoutDataMiddleware` (`framework/SimpleModule.Hosting/Middleware/`). There is **no** interface for modules to contribute shared props — a module adds its own via middleware registered in `ConfigureMiddleware`.
- **Module middleware hook:** `IModule.ConfigureMiddleware(IApplicationBuilder app)` exists.
- **Layout shell:** `packages/SimpleModule.UI/components/layouts/` — `app-layout.tsx` (authenticated), `public-layout.tsx` (public), `layout-provider.tsx`, `types.ts` (`SharedProps`). App name `"SimpleModule"` and the `"S"` badge are **hardcoded** in the sidebar, mobile header, and public nav. No footer exists. `SharedProps` already has `menus.navbar` and `publicMenu`.
- **Renderer:** `HtmlFileInertiaPageRenderer` reads `wwwroot/index.html` **once at startup**, splits at `<!--INERTIA_PAGE_DATA-->`, and per-request only replaces `<!--CSP_NONCE-->`. Head placeholders present: `<!--DEPLOY_VERSION-->`, `<!--MODULE_CSS_LINKS-->`, `<!--CSP_NONCE-->`.
- **Favicon:** `template/SimpleModule.Host/Program.cs` maps `GET /favicon.ico` → `wwwroot/favicon.svg`. `index.html` head has `<link rel="icon" type="image/svg+xml" href="/favicon.svg" />`.
- **DbContext schema helpers** (not needed here, recorded for completeness): `AddModuleDbContext<T>()`, `ModelBuilder.ApplyModuleSchema(name, dbOptions)`.

---

## 3. Module layout (new files)

```
modules/Branding/
├── src/
│   ├── SimpleModule.Branding.Contracts/
│   │   ├── SimpleModule.Branding.Contracts.csproj   # references SimpleModule.Core only
│   │   ├── IBrandingContracts.cs                     # GetBrandingAsync() -> BrandingDto
│   │   ├── BrandingDto.cs                            # [Dto] — the resolved shared-prop shape
│   │   ├── TopBarConfig.cs                           # [Dto] nested
│   │   ├── FooterConfig.cs                           # [Dto] nested
│   │   └── BrandingLink.cs                           # [Dto] { Label, Url }
│   └── SimpleModule.Branding/
│       ├── SimpleModule.Branding.csproj             # SDK.StaticWebAssets; refs Core + Contracts + Settings.Contracts + FileStorage.Contracts; FrameworkReference AspNetCore.App
│       ├── BrandingModule.cs                         # [Module("Branding", ...)] IModule
│       ├── BrandingService.cs                        # IBrandingContracts — resolves settings -> BrandingDto (+ cached)
│       ├── BrandingPermissions.cs                    # Branding.Manage
│       ├── BrandingSettingKeys.cs                    # const string keys + defaults
│       ├── BrandingSharedDataMiddleware.cs           # sets InertiaSharedData["branding"]
│       ├── BrandingHeadContributor.cs                # IInertiaHeadContributor — emits <style>/<link rel=icon>
│       ├── Endpoints/
│       │   └── Branding/
│       │       ├── GetBrandingEndpoint.cs            # GET  /api/branding (admin)  -> editable values
│       │       ├── UpdateBrandingEndpoint.cs         # PUT  /api/branding (admin)  -> SetManyAsync
│       │       └── UploadAssetEndpoint.cs            # POST /api/branding/asset (logo|favicon) -> FileStorage
│       ├── Pages/
│       │   ├── index.ts                              # { "Branding/Manage": () => import("./Manage") }
│       │   ├── ManageEndpoint.cs                     # IViewEndpoint GET /branding (admin) -> Inertia.Render("Branding/Manage", ...)
│       │   ├── Manage.tsx                            # admin form + live preview
│       │   └── components/                           # field sections, preview pane
│       ├── vite.config.ts
│       ├── package.json
│       ├── tsconfig.json
│       └── wwwroot/                                  # built output
└── tests/
    └── SimpleModule.Branding.Tests/
        └── SimpleModule.Branding.Tests.csproj
```

Wire-up: add project references to `template/SimpleModule.Host/SimpleModule.Host.csproj` and entries to `SimpleModule.slnx`.

---

## 4. Data model — Settings keys

All `Scope.Application`, `Group = "Branding"`.

| Key | Type | Default | Notes |
|---|---|---|---|
| `branding.app_name` | `Text` | `"SimpleModule"` | Shown in sidebar/header/title. |
| `branding.logo_file_id` | `Text` | `""` (empty → badge fallback) | FileStorage id. |
| `branding.favicon_file_id` | `Text` | `""` (empty → `/favicon.svg`) | FileStorage id. |
| `branding.color_primary` | `Color` | `#059669` | Overrides `--color-primary` (light). |
| `branding.color_primary_dark` | `Color` | `#34d399` | Overrides `--color-primary` under `.dark`. |
| `branding.custom_css` | `MultilineText` | `""` | Injected verbatim into an inline `<style>` (style-src already allows `'unsafe-inline'`). Power-user footgun — documented. |
| `branding.topbar` | `Json` | `{ "enabled": false, ... }` | `TopBarConfig`. |
| `branding.footer` | `Json` | `{ "enabled": false, ... }` | `FooterConfig`. |

`TopBarConfig` = `{ enabled: bool, message: string, backgroundColor: string, textColor: string, links: BrandingLink[], dismissible: bool }`.
`FooterConfig` = `{ enabled: bool, text: string, links: BrandingLink[], showCopyright: bool }`.
`BrandingLink` = `{ label: string, url: string }`.

**`BrandingDto`** has two faces:

- **Shared-prop face** (what reaches every page as the `branding` Inertia prop, for React to render): `appName`, `logoUrl` (resolved from file id → public file URL, or null), `topBar: TopBarConfig`, `footer: FooterConfig`. This is the minimal payload React chrome needs.
- **Head-applied values** (NOT in the shared prop): `colorPrimary`, `colorPrimaryDark`, `customCss`, `faviconUrl`. These are applied server-side via the head contributor (§5) so they take effect before first paint. (`customCss` in particular is kept off the per-page payload.)

The admin **live preview** (§6) uses the form's local state, not the shared prop, so it does not need colors in the prop. The editable `GET /api/branding` response (admin-only) returns the full set including colors and custom CSS for the form to populate.

---

## 5. Runtime application

### 5a. Shared prop (React chrome)

`BrandingSharedDataMiddleware` (registered via `BrandingModule.ConfigureMiddleware`) resolves `IBrandingContracts.GetBrandingAsync()` and calls `InertiaSharedData.Set("branding", dto)`. Runs only for requests that will render Inertia pages (cheap; the DTO is cached in `BrandingService` and invalidated on update).

Frontend: extend `SharedProps` in `packages/SimpleModule.UI/components/layouts/types.ts` with `branding: BrandingProps`. Layouts consume it:
- `app-layout.tsx` + `public-layout.tsx`: replace hardcoded name/badge with `branding.appName` and, if `branding.logoUrl`, an `<img>` (else keep the colored badge with the first letter of `appName`).
- New `top-bar.tsx`: rendered above content when `branding.topBar.enabled`. Dismissible state in `localStorage`.
- New `footer.tsx`: rendered below content when `branding.footer.enabled`.
- Set `document.title` from `branding.appName` (client-side, in layout effect) — title is the one head element we set client-side since `index.html` ships a static `<title>`.

### 5b. Head injection (colors, custom CSS, favicon) — framework addition

Minimal, generic, reusable extension so colors/favicon apply with **no flash** and **no response buffering**:

1. **Core:** add `IInertiaHeadContributor { string? GetHeadHtml(HttpContext context); }` in `SimpleModule.Core.Inertia`.
2. **Template:** add `<!--HEAD_CONTRIBUTIONS-->` placeholder inside `<head>` of `template/SimpleModule.Host/wwwroot/index.html` (just before `</head>`).
3. **Renderer:** in `HtmlFileInertiaPageRenderer.RenderPageAsync`, resolve `IEnumerable<IInertiaHeadContributor>` from `httpContext.RequestServices`, concatenate their output, and replace `<!--HEAD_CONTRIBUTIONS-->` in `before` **before** the existing nonce replacement (so contributor `<style nonce="<!--CSP_NONCE-->">` tags get a real nonce). If no contributors are registered, the placeholder is replaced with empty string.
4. **Branding:** `BrandingHeadContributor` emits:
   - `<style>:root{--color-primary:<light>;}.dark{--color-primary:<dark>;}<customCss></style>` — only emitting the `:root`/`.dark` blocks when the color differs from the default, and always appending custom CSS if non-empty.
   - `<link rel="icon" href="/api/branding/assets/favicon?v=<id>">` when a custom favicon is set (placed after the static favicon `<link>` → wins).

**CSP:** confirmed `style-src 'self' 'unsafe-inline' ...` in `SimpleModuleHostExtensions.cs` — inline `<style>` is allowed without a nonce. (Do **not** add a nonce to the style tag: since `style-src` has no nonce/hash source, plain inline styles are permitted by `'unsafe-inline'`.)

### 5c. Asset serving (logo + favicon) — anonymous

FileStorage's `DownloadEndpoint` (`GET /api/files/{id}`) requires `FileStorage.View` **and** passes a `FileOwnershipCheck` — so it cannot serve a logo/favicon to anonymous visitors (e.g. on the login page). Branding therefore exposes its own **anonymous** asset endpoint:

- `GET /api/branding/assets/{kind}` (`kind` ∈ `logo|favicon`), `.AllowAnonymous()` — resolves the configured file id from settings, streams the bytes via `IFileStorageContracts.DownloadFileAsync(id)`, sets a long cache header. Returns 404 when unset.
- `logoUrl`/`faviconUrl` in the DTO point at `/api/branding/assets/logo?v=<fileId>` (the file-id query busts the cache when the asset changes).

---

## 6. Admin UI — `Branding/Manage`

- `ManageEndpoint : IViewEndpoint` → `GET /branding`, `.RequirePermission(BrandingPermissions.Manage)`, `Inertia.Render("Branding/Manage", new { branding = <editable values incl. custom css>, defaults })`.
- **Registered in `Pages/index.ts`** (`"Branding/Manage"`) — mandatory per project rule.
- `Manage.tsx` sections: **Identity** (app name text; logo upload + preview + clear; favicon upload + preview + clear), **Colors** (primary light, primary dark — color pickers with swatches), **Top bar** (enable toggle, message, bg/text color, links editor, dismissible toggle), **Footer** (enable toggle, text, links editor, show-copyright toggle), **Advanced** (custom CSS textarea with a warning note). A **live preview** pane shows a mock sidebar header + top bar + footer reflecting current form state.
- Uploads: `UploadAssetEndpoint` (`POST /api/branding/assets/{kind}` with `kind=logo|favicon`, `.DisableAntiforgery()`, admin-only) forwards the `IFormFile` to `IFileStorageContracts.UploadFileAsync(stream, name, contentType, folder: "branding", userId)`, persists the returned file id into the matching setting, and returns the new file id + URL. Save of the rest of the form persists via `SetManyAsync`.
- Save: `PUT /api/branding` validates and calls `ISettingsContracts.SetManyAsync([...])`. `BrandingService` cache invalidates so the next page render reflects the change.

---

## 7. Permissions & menu

- `BrandingPermissions : IModulePermissions` with `Manage = "Branding.Manage"`. Endpoints use `.RequirePermission(BrandingPermissions.Manage)`.
- `ConfigureMenu`: add an **Admin sidebar** menu item "Branding" → `/branding`, `RequiredPermission = BrandingPermissions.Manage`, suitable icon/order.

---

## 8. Testing

**Backend (xUnit, `SimpleModuleWebApplicationFactory`):**
- Defaults: with no settings stored, `GetBrandingAsync` returns built-in defaults (`appName == "SimpleModule"`, top bar/footer disabled, null logo/favicon URLs).
- Persistence: `PUT /api/branding` then `GET /api/branding` round-trips all fields; settings actually written via `ISettingsContracts`.
- Authorization: `GET/PUT /api/branding` and `GET /branding` return 403 without `Branding.Manage`; 200 with it.
- Shared prop: an Inertia page response includes a `branding` prop with the resolved DTO.
- Head contributor: given non-default colors/custom CSS/favicon, `BrandingHeadContributor.GetHeadHtml` emits the expected `<style>`/`<link>`; emits minimal/empty output at defaults.
- Upload: `POST /api/branding/asset` stores via FileStorage and returns a usable URL; rejects empty/oversized/non-image files.
- Framework: renderer replaces `<!--HEAD_CONTRIBUTIONS-->` (empty when no contributors; content when registered) and nonces are applied to contributed `<style>` tags.

**Frontend:** `npm run validate-pages` passes (Branding/Manage registered). `npm run check` clean.

**E2E (verify-feature / qa):** log in as admin → open /branding → change app name + primary color + enable top bar + footer + upload a logo → save → reload → confirm sidebar shows new name/logo, colors changed with no flash, top bar + footer render, favicon updated.

---

## 9. Out of scope (v1 / YAGNI)

- Per-tenant branding (global only).
- Theme presets / multiple saved themes / import-export.
- Full OKLCH palette editing and secondary/accent colors (only primary light+dark in v1; rest via custom CSS).
- Editing module-contributed sidebar/nav menu items (the top bar is a separate, branding-owned bar).
- Font customization (could be a fast-follow via custom CSS).

---

## 10. Risk notes

- **CSP for inline styles** — RESOLVED: `style-src` already includes `'unsafe-inline'`, so the injected `<style>` works. (Keep no nonce on it.)
- **Framework touch** — this feature necessarily edits framework (renderer + Core interface + index.html) and the shared UI package (layouts), not just a self-contained module. That is inherent to branding the shell. Kept minimal and generic (`IInertiaHeadContributor` benefits any module).
- **Asset serving** — RESOLVED: FileStorage's download endpoint is permissioned + ownership-checked, so Branding serves logo/favicon via its own anonymous `GET /api/branding/assets/{kind}` (§5c).
- **Middleware ordering** — the branding shared-prop middleware (registered via `IModule.ConfigureMiddleware`) must run within the request pipeline before the Inertia response renders. `SimpleModule.Localization` already uses `ConfigureMiddleware` for per-request work, confirming the pattern; verify ordering during implementation.
- **FileStorage as a hard dependency** — Branding references `SimpleModule.FileStorage.Contracts`. FileStorage is in the shipping/load-test host set, so this is safe.
```
