---
outline: deep
---

# Styling

SimpleModule uses **Tailwind CSS 4** for styling, with a centralized design system defined in the `@simplemodule/theme-default` package. The theme provides semantic color tokens, component classes, and full dark mode support.

## Tailwind CSS 4 Setup

The global stylesheet lives at `template/SimpleModule.Host/Styles/app.css` and imports Tailwind along with the theme:

```css
@import "tailwindcss";
@import "../../../packages/SimpleModule.Theme.Default/theme.css";
@source "../../../packages/SimpleModule.UI/";
@source "../../../packages/SimpleModule.Client/";
@source "../../../modules/**/Views/**/*.tsx";
@source "../../../modules/**/Pages/**/*.tsx";
```

The `@source` directives tell Tailwind where to scan for class usage, ensuring that utility classes used in module components, UI package files, and React TSX components are all included in the final CSS output.

## The Default Theme

The `@simplemodule/theme-default` package (`packages/SimpleModule.Theme.Default/`) provides a complete design system via CSS custom properties inside a `@theme` block. This is the single source of truth for all colors, shadows, and visual tokens.

### Color Tokens

Colors are defined in **OKLCH** for perceptually even lightness and vivid, accessible hues. The palette is "quiet editorial" — emerald primary, rose danger, amber warning, sky info, with warm-leaning neutrals.

#### Primary & Accent

| Token | Light | Purpose |
|---|---|---|
| `--color-primary` | `oklch(0.62 0.16 158)` | Primary actions, links, focus rings (emerald) |
| `--color-primary-hover` | `oklch(0.55 0.17 158)` | Primary hover state |
| `--color-primary-light` | `oklch(0.78 0.16 158)` | Light primary variant |
| `--color-primary-subtle` | `oklch(0.62 0.16 158 / 0.09)` | Subtle backgrounds |
| `--color-primary-ring` | `oklch(0.62 0.16 158 / 0.32)` | Focus ring color |
| `--color-accent` | `oklch(0.5 0.11 192)` | Deep emphasis (teal) |

#### Semantic Colors

| Category | Tokens |
|---|---|
| **Success** | `success`, `success-light`, `success-bg`, `success-text` |
| **Danger** | `danger`, `danger-hover`, `danger-bg`, `danger-text` |
| **Warning** | `warning`, `warning-bg`, `warning-border`, `warning-text` |
| **Info** | `info`, `info-bg` |

#### Surfaces & Text

| Token | Light | Dark |
|---|---|---|
| `--color-surface` | `oklch(1 0 0)` | `oklch(0.205 0.006 60)` |
| `--color-surface-raised` | `oklch(0.985 0.003 80)` | `oklch(0.245 0.007 60)` |
| `--color-surface-sunken` | `oklch(0.965 0.005 80)` | `oklch(0.16 0.006 60)` |
| `--color-text` | `oklch(0.22 0.015 250)` | `oklch(0.96 0.005 80)` |
| `--color-text-secondary` | `oklch(0.45 0.015 250)` | `oklch(0.74 0.008 80)` |
| `--color-text-muted` | `oklch(0.62 0.012 250)` | `oklch(0.55 0.008 80)` |

Dark surfaces sit at hue 60 (toward amber) at very low chroma — a warm graphite that never reads as cold blue. All tokens are accessible as Tailwind utilities, for example `bg-surface`, `text-primary`, `border-border-strong`.

### Shadows

The theme defines a layered, OKLCH-based shadow scale. `--shadow-xs` through `--shadow-xl` cover elevation; colored variants tint buttons:

```css
--shadow-xs: 0 1px 2px 0 oklch(0.18 0.012 260 / 0.05);
--shadow-sm: 0 1px 2px 0 oklch(0.18 0.012 260 / 0.06), 0 1px 3px 0 oklch(0.18 0.012 260 / 0.04);
--shadow-md: 0 4px 8px -2px oklch(0.18 0.012 260 / 0.08), 0 2px 4px -2px oklch(0.18 0.012 260 / 0.05);
--shadow-lg: 0 12px 24px -8px oklch(0.18 0.012 260 / 0.12), 0 4px 8px -4px oklch(0.18 0.012 260 / 0.06);
--shadow-xl: 0 24px 48px -12px oklch(0.18 0.012 260 / 0.18), 0 8px 16px -8px oklch(0.18 0.012 260 / 0.08);
--shadow-primary: 0 6px 18px -4px oklch(0.62 0.16 158 / 0.42), 0 2px 4px -2px oklch(0.62 0.16 158 / 0.25);
--shadow-danger: 0 6px 18px -4px oklch(0.6 0.21 17 / 0.32);
```

In dark mode the elevation shadows deepen and the primary shadow becomes a colored glow.

### Motion, Layering & Opacity

The theme also exposes scales for animation, stacking, and opacity so components stay consistent:

```css
/* Motion */
--duration-instant: 80ms;  --duration-fast: 150ms;  --duration-base: 200ms;
--duration-moderate: 300ms;  --duration-slow: 500ms;
--ease-out: cubic-bezier(0.16, 1, 0.3, 1);
--ease-spring: cubic-bezier(0.34, 1.56, 0.64, 1);

/* Z-index stack */
--z-sticky: 10;  --z-overlay: 20;  --z-sidebar: 40;  --z-dropdown: 50;
--z-modal: 60;  --z-toast: 80;  --z-tooltip: 90;

/* Opacity */
--opacity-disabled: 0.45;  --opacity-muted: 0.65;
--opacity-overlay: 0.78;  --opacity-backdrop: 0.5;
```

## Dark Mode {#dark-mode}

Dark mode is built into the theme via the `.dark` class selector. When the `.dark` class is present on a parent element, all color tokens automatically switch to their dark variants:

```css
.dark {
  --color-surface: oklch(0.205 0.006 60);
  --color-surface-raised: oklch(0.245 0.007 60);
  --color-text: oklch(0.96 0.005 80);
  --color-text-secondary: oklch(0.74 0.008 80);
  --color-border: oklch(0.3 0.008 60);
  /* ... all tokens are overridden */
}
```

Because the theme uses CSS custom properties, dark mode works without any JavaScript -- just toggling the `.dark` class switches the entire color palette.

## Built-in Component Classes

The theme provides pre-built CSS component classes in the `@layer components` block:

### Buttons

```css
.btn-primary     /* Flat solid emerald, white text, elevated shadow */
.btn-secondary   /* Surface background, bordered */
.btn-ghost       /* Transparent, subtle hover */
.btn-danger      /* Solid rose background, elevated shadow */
.btn-outline     /* Transparent, primary border */
.btn-sm          /* Small size modifier */
.btn-lg          /* Large size modifier */
```

::: tip
Design-system v2 removed gradient surface treatments — the primary button and accents are now flat solid color. Legacy helpers like `.gradient-text` are kept for compatibility but render as solid primary.
:::

### Cards & Surfaces

```css
.glass-card      /* Glassmorphism effect with backdrop blur */
.card            /* Standard card with border and hover effect */
.dash-card       /* Dashboard stat card */
.panel           /* Section panel with gradient accent bar */
```

### Badges & Alerts

```css
.badge-success .badge-danger .badge-warning .badge-info
.alert-success .alert-danger .alert-warning .alert-info
```

### Navigation & Layout

```css
.nav-link-active     /* Active navigation link */
.nav-link-inactive   /* Inactive navigation link */
.app-layout          /* Root flex layout */
.app-sidebar         /* Fixed sidebar with collapse support */
.app-content         /* Main content area */
```

### Utilities

```css
.font-display        /* Fraunces display serif (headings, stat values) */
.font-body           /* Geist body sans */
.dash-stat           /* Large display-font metric value */
.dash-label          /* Small uppercase metric label */
.gradient-text       /* Compatibility shim — now renders solid primary */
.gradient-border     /* Primary border using mask technique */
.bg-mesh             /* Flat ambient background fill */
.spinner             /* CSS loading spinner */
```

### Tables

Base table styles ship globally. Row-hover is **opt-in** — set `data-interactive` on the `<table>` so read-only tables (recovery codes, dashboard lists) don't pick up an interactive cue. Add the `.num` class to a `td`/`th` for right-aligned, tabular-figure numeric columns.

```html
<table data-interactive>
  <thead><tr><th>Name</th><th class="num">Requests</th></tr></thead>
  ...
</table>
```

## Typography

The theme sets three font families, exposed as `--font-body`, `--font-display`, and `--font-mono`:

- **Body text** (`--font-body`): `"Geist"`, "Inter", system-ui, sans-serif
- **Display / headings** (`--font-display`): `"Fraunces"`, serif fallbacks
- **Code** (`--font-mono`): `"JetBrains Mono"`, "Fira Code", monospace

Headings (`h1`-`h6`) use the display serif with tight tracking. Apply `.font-display` or `.font-body` directly when you need the family on non-heading elements (for example, stat values).

## Theme Customization

To customize the theme, you have two options:

### Override tokens in your app.css

Add overrides after the theme import:

```css
@import "tailwindcss";
@import "../../../packages/SimpleModule.Theme.Default/theme.css";

@theme {
  --color-primary: #2563eb;         /* Switch to blue */
  --color-primary-hover: #1d4ed8;
  --color-accent: #1e40af;
}
```

### Create a custom theme package

Create a new package modeled after `@simplemodule/theme-default` with your own `theme.css` file, and update the import in `app.css`.

## Module-Specific Styles

Modules can include their own CSS alongside their components. The `@source` directives in `app.css` scan module directories for Tailwind class usage:

```css
@source "../../../modules/**/Views/**/*.tsx";
@source "../../../modules/**/Pages/**/*.tsx";
```

For module-specific styles that go beyond Tailwind utilities, add a CSS file in the module's source directory and import it in your component or include it in the module's Vite build.

## Biome Configuration for CSS

Biome is configured to understand Tailwind CSS directives like `@theme`, `@layer`, and `@source`:

```json
{
  "css": {
    "parser": {
      "tailwindDirectives": true
    }
  }
}
```

This prevents false-positive lint/format errors on Tailwind-specific CSS syntax.

## Next Steps

- [Vite Build System](/frontend/vite) -- how styles are bundled per module
- [UI Components](/frontend/components) -- pre-built components using these styles
- [Inertia.js Integration](/guide/inertia) -- how server data flows to styled React components
