---
outline: deep
---

# Branding

The Branding module lets an administrator customize the appearance of a SimpleModule application from a single global configuration page — app name, logo, favicon, primary colors, an announcement top bar, a footer, and advanced custom CSS. Colors, custom CSS, and the favicon are injected into the document `<head>` server-side so they apply before first paint (no flash of default theme), while the app name, logo, top bar, and footer reach React as an Inertia shared prop.

## Admin UI

The module mounts an admin view at `/branding/manage` (mirroring Settings' `/settings/manage`) with a live preview panel. Everything on the page — and the API behind it — is protected by the single `Branding.Manage` permission.

Configurable surface:

| Section | What it controls |
|---|---|
| Identity | App name, logo image, favicon |
| Colors | Primary color for light and dark themes |
| Top bar | Announcement banner: message, colors, links, dismissible toggle |
| Footer | Footer text, links, copyright line |
| Advanced | Free-form custom CSS injected into every page |

## How values are stored

Branding values are stored through `ISettingsContracts` under `branding.*` keys (see `BrandingSettingKeys`), but they are **intentionally not registered as `SettingDefinition`s** — they are edited through the dedicated `/branding/manage` page, not the generic Settings admin UI. Defaults live in `BrandingDefaults` and `BrandingService`.

## Server-side head injection

The framework's `IInertiaHeadContributor` extension point lets a module contribute HTML to the `<head>` of every Inertia page render. The Branding module registers `BrandingHeadContributor`, which injects:

- **CSS variable overrides** when the primary color differs from the default. The full primary palette (hover, light, subtle, ring variants) is derived from the chosen color via `color-mix`, so button hovers and focus rings follow the brand color.
- **Custom CSS**, if any is configured.
- **A favicon `<link>`** pointing at the uploaded favicon asset.

Because injection happens server-side, branded colors apply before the first paint.

## The `branding` shared prop

`BrandingSharedDataMiddleware` publishes the resolved `BrandingDto` as the `branding` Inertia shared prop on every page render (API routes are skipped), so the React layout chrome can render the app name, logo, top bar, and footer without an extra request:

```csharp
[Dto]
public class BrandingDto
{
    public string AppName { get; set; }
    public string? LogoUrl { get; set; }
    public string? FaviconUrl { get; set; }
    public string ColorPrimary { get; set; }
    public string ColorPrimaryDark { get; set; }
    public TopBarConfig TopBar { get; set; }
    public FooterConfig Footer { get; set; }
}
```

Custom CSS is deliberately excluded from this DTO so it never ships in the per-page payload — it only travels via head injection.

## API endpoints

| Method | Route | Auth | Purpose |
|---|---|---|---|
| `GET` | `/api/branding` | `Branding.Manage` | Full editable model (`BrandingEditModel`) |
| `PUT` | `/api/branding` | `Branding.Manage` | Save the branding configuration |
| `POST` | `/api/branding/assets/{kind}` | `Branding.Manage` | Upload a `logo` or `favicon` image |
| `GET` | `/api/branding/assets/{kind}` | Anonymous | Serve the logo/favicon |

The asset-serve endpoint is intentionally anonymous: the logo and favicon must render on the login page, before any user is authenticated. FileStorage's own download endpoint is permissioned and ownership-checked, so branding assets get their own serve path with `Cache-Control: public, max-age=3600`.

## Security hardening

- **No SVG** — uploads and serving are restricted to raster/icon formats (PNG, JPEG, GIF, WebP, ICO). An SVG can carry inline script that executes on direct navigation, which would be stored XSS on an anonymously served asset. The content type is re-validated on the serve path, not just at upload.
- **`X-Content-Type-Options: nosniff`** on served assets.
- **Link URL sanitization** — top-bar and footer link URLs are sanitized to block the `javascript:` scheme.
- **Upload limits** — assets are capped at 2 MB.

## Querying from other modules

Depend on the contract if a module needs the resolved branding programmatically:

```csharp
public interface IBrandingContracts
{
    Task<BrandingDto> GetBrandingAsync();
    Task<string> GetCustomCssAsync();
    Task<BrandingEditModel> GetEditableAsync();
    Task UpdateAsync(BrandingEditModel model);
}
```

## Next Steps

- [Settings](/guide/settings) — the storage layer branding values live in.
- [File Storage](/guide/file-storage) — where uploaded logo/favicon files are kept.
- [Permissions](/guide/permissions) — how `Branding.Manage` is enforced.
