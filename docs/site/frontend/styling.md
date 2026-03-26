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

The `@source` directives tell Tailwind where to scan for class usage, ensuring that utility classes used in module components, UI package files, and Blazor Razor components are all included in the final CSS output.

## The Default Theme

The `@simplemodule/theme-default` package (`packages/SimpleModule.Theme.Default/`) provides a complete design system via CSS custom properties inside a `@theme` block. This is the single source of truth for all colors, shadows, and visual tokens.

### Color Tokens

#### Primary & Accent

| Token | Light | Purpose |
|---|---|---|
| `--color-primary` | `#16a34a` | Primary actions, links, focus rings |
| `--color-primary-hover` | `#15803d` | Primary hover state |
| `--color-primary-light` | `#4ade80` | Light primary variant |
| `--color-primary-subtle` | `rgba(22, 163, 74, 0.08)` | Subtle backgrounds |
| `--color-accent` | `#166534` | Gradient endpoints, deep emphasis |

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
| `--color-surface` | `#ffffff` | `#0f172a` |
| `--color-surface-raised` | `#f8fafc` | `#1e293b` |
| `--color-surface-sunken` | `#f1f5f9` | `#0b1120` |
| `--color-text` | `#0f172a` | `#f1f5f9` |
| `--color-text-secondary` | `#475569` | `#94a3b8` |
| `--color-text-muted` | `#94a3b8` | `#64748b` |

All tokens are accessible as Tailwind utilities. For example, `bg-surface`, `text-primary`, `border-border-strong`.

### Shadows

The theme defines themeable shadows used by button components:

```css
--shadow-primary: 0 4px 14px rgba(22, 163, 74, 0.35);
--shadow-primary-hover: 0 6px 20px rgba(22, 163, 74, 0.5);
--shadow-danger: 0 4px 14px rgba(225, 29, 72, 0.25);
--shadow-danger-hover: 0 6px 20px rgba(225, 29, 72, 0.4);
```

## Dark Mode {#dark-mode}

Dark mode is built into the theme via the `.dark` class selector. When the `.dark` class is present on a parent element, all color tokens automatically switch to their dark variants:

```css
.dark {
  --color-surface: #0f172a;
  --color-surface-raised: #1e293b;
  --color-text: #f1f5f9;
  --color-text-secondary: #94a3b8;
  --color-border: #1e293b;
  /* ... all tokens are overridden */
}
```

Because the theme uses CSS custom properties, dark mode works without any JavaScript -- just toggling the `.dark` class switches the entire color palette.

## Built-in Component Classes

The theme provides pre-built CSS component classes in the `@layer components` block:

### Buttons

```css
.btn-primary     /* Gradient background, white text, elevated shadow */
.btn-secondary   /* Surface background, bordered */
.btn-ghost       /* Transparent, subtle hover */
.btn-danger      /* Red background, elevated shadow */
.btn-outline     /* Transparent, primary border */
.btn-sm          /* Small size modifier */
.btn-lg          /* Large size modifier */
```

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
.gradient-text       /* Gradient-filled text */
.gradient-border     /* Gradient border using mask technique */
.bg-mesh             /* Animated background gradient mesh */
.spinner             /* CSS loading spinner */
```

## Typography

The theme sets two font families:

- **Body text**: `"DM Sans"`, system-ui, sans-serif
- **Headings**: `"Sora"`, "DM Sans", system-ui, sans-serif
- **Code**: `"JetBrains Mono"`, "Fira Code", monospace

Headings (`h1`-`h6`) are styled in the base layer with tight tracking and bold weight.

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
