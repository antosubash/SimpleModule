# Design: `@simplemodule/ui` — Shared Component Library

**Date:** 2026-03-16
**Status:** Approved

## Overview

A shadcn-style shared React component library for SimpleModule. Components are source files owned by the project (not a compiled npm package), built on Radix UI headless primitives and styled with cva + Tailwind referencing theme CSS variables.

## Architecture

- **Package:** `src/SimpleModule.UI/` as npm workspace `@simplemodule/ui`
- **Stack:** Radix UI (headless), class-variance-authority (cva), tailwind-merge + clsx via `cn()`
- **Design tokens:** Stay in `@simplemodule/theme-default` as CSS variables. Components reference them via Tailwind classes (`bg-primary`, `text-danger`, etc.)
- **No pre-build step:** Modules import source TSX directly; Vite handles transpilation

### File Structure

```
src/SimpleModule.UI/
├── package.json
├── components/              # active components (added via CLI)
│   └── index.ts             # barrel re-export
├── lib/
│   └── utils.ts             # cn() helper
└── registry/
    ├── registry.json         # component metadata
    └── templates/            # all available component templates
        ├── button.tsx
        ├── input.tsx
        ├── dialog.tsx
        └── ...
```

### Module Consumption

```tsx
import { Button, Card, Input, Badge } from '@simplemodule/ui';
```

## Component List (Starter Set — ~18 components)

| Component | Radix Primitive | Key Variants |
|-----------|----------------|--------------|
| Button | Slot (asChild) | variant: primary, secondary, ghost, danger, outline / size: sm, default, lg |
| Input | — | variant: default, error |
| Textarea | — | — |
| Label | @radix-ui/react-label | — |
| Select | @radix-ui/react-select | — |
| Checkbox | @radix-ui/react-checkbox | — |
| Radio Group | @radix-ui/react-radio-group | — |
| Switch | @radix-ui/react-switch | — |
| Dialog | @radix-ui/react-dialog | — |
| Dropdown Menu | @radix-ui/react-dropdown-menu | — |
| Popover | @radix-ui/react-popover | — |
| Tabs | @radix-ui/react-tabs | — |
| Table | — | — |
| Card | — | — |
| Badge | — | variant: success, danger, warning, info, default |
| Alert | — | variant: success, danger, warning, info |
| Separator | @radix-ui/react-separator | — |
| Spinner | — | size: sm, default, lg |

## Styling Approach

Components use cva for variant-driven styling with Tailwind classes that resolve to theme CSS variables.

Example (Button):
```tsx
const buttonVariants = cva(
  'inline-flex items-center justify-center gap-2 rounded-xl text-sm font-semibold transition-all duration-200 active:scale-[0.97] cursor-pointer',
  {
    variants: {
      variant: {
        primary: 'text-white bg-gradient-to-br from-primary to-accent shadow-(--shadow-primary) hover:shadow-(--shadow-primary-hover) hover:-translate-y-px',
        secondary: 'bg-surface text-text border border-border hover:bg-surface-raised hover:border-border-strong',
        ghost: 'bg-transparent text-text-secondary hover:bg-primary-subtle hover:text-primary',
        danger: 'text-white bg-danger shadow-(--shadow-danger) hover:bg-danger-hover hover:shadow-(--shadow-danger-hover) hover:-translate-y-px',
        outline: 'bg-transparent text-primary border-2 border-primary/30 hover:bg-primary-subtle hover:border-primary',
      },
      size: {
        sm: 'px-3.5 py-1.5 text-xs rounded-lg',
        default: 'px-5 py-2.5',
        lg: 'px-8 py-3.5 text-base',
      },
    },
    defaultVariants: { variant: 'primary', size: 'default' },
  }
);
```

## Theming

Components are fully theme-aware through CSS variables:

```
@simplemodule/theme-default (theme.css)
  defines: --color-primary, --shadow-primary, etc.
     ↓
Tailwind @theme block
  maps: bg-primary → var(--color-primary)
     ↓
@simplemodule/ui components
  uses: Tailwind classes like 'bg-primary', 'text-danger'
     ↓
Browser renders current variable values
```

To create a new theme, create a new theme package with the same CSS variable names and different values. Swap the import in `app.css`. All components update automatically.

### Shadow Tokens (new, added to theme)

```css
@theme {
  --shadow-primary: 0 4px 14px rgba(13, 148, 136, 0.35);
  --shadow-primary-hover: 0 6px 20px rgba(13, 148, 136, 0.5);
  --shadow-danger: 0 4px 14px rgba(225, 29, 72, 0.25);
  --shadow-danger-hover: 0 6px 20px rgba(225, 29, 72, 0.4);
}
```

## CLI Tool

**Location:** `tools/add-component.mjs` + npm script alias `npm run ui:add`

**Usage:**
```bash
npm run ui:add -- button          # add single component
npm run ui:add -- dialog badge    # add multiple
npm run ui:add -- --list          # show available components
```

**Behavior:**
1. Reads `registry/registry.json` for component metadata
2. Copies template from `registry/templates/<name>.tsx` → `components/<name>.tsx`
3. Auto-installs Radix packages if needed
4. Resolves dependencies (e.g., dialog pulls in button)
5. Updates `components/index.ts` barrel export

### Registry Format

```json
{
  "button": {
    "name": "Button",
    "file": "button.tsx",
    "dependencies": [],
    "radixPackage": null
  },
  "dialog": {
    "name": "Dialog",
    "file": "dialog.tsx",
    "dependencies": ["button"],
    "radixPackage": "@radix-ui/react-dialog"
  }
}
```

## Migration

- **No breaking changes.** Existing CSS classes (`btn-primary`, `glass-card`, etc.) stay in theme
- Modules adopt `@simplemodule/ui` components incrementally
- CSS classes can be retired once all modules have migrated off them
- No module Vite config changes needed — UI source gets bundled into each module's `.pages.js`
