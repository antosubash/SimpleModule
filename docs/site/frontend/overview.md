---
outline: deep
---

# Frontend Overview

SimpleModule's frontend is built on **React 19** served through **Inertia.js** with a **Blazor SSR** shell. Each module ships its own self-contained page bundle, and the ClientApp bootstraps Inertia to dynamically load pages from any module at runtime.

## Architecture

The frontend architecture follows a modular pattern that mirrors the backend:

```
Browser Request
  --> ASP.NET route handler calls Inertia.Render("Products/Browse", props)
  --> Inertia middleware renders Blazor SSR shell with JSON props
  --> React ClientApp dynamically imports Products.pages.js
  --> Component hydrates with server-provided props
```

Each module compiles its React pages into a single ES module bundle (`{ModuleName}.pages.js`) using Vite in library mode. The host application's ClientApp is the Inertia bootstrap that resolves and loads these bundles on demand.

## Tech Stack

| Technology | Version | Purpose |
|---|---|---|
| React | 19 | UI rendering |
| Inertia.js | 2.x | SPA-like navigation without a client-side router |
| Vite | 6.x | Build tooling and dev server |
| Tailwind CSS | 4.x | Utility-first styling |
| TypeScript | 5.8 | Type safety |
| Biome | 2.x | Linting and formatting |

## How Module Frontends Work

Every module that has a UI builds a `{ModuleName}.pages.js` file into its `wwwroot/` directory. This file exports a `pages` record that maps route names to React components:

```ts
// modules/Products/src/Products/Pages/index.ts
export const pages: Record<string, unknown> = {
  'Products/Browse': () => import('../Views/Browse'),
  'Products/Manage': () => import('../Views/Manage'),
  'Products/Create': () => import('../Views/Create'),
  'Products/Edit': () => import('../Views/Edit'),
};
```

These bundles externalize shared dependencies (React, React-DOM, Inertia) so they are not duplicated across modules. The shared libraries are vendored once by the ClientApp build.

## ClientApp: The Inertia Bootstrap

The ClientApp at `template/SimpleModule.Host/ClientApp/app.tsx` is the entry point for the entire frontend. It creates an Inertia app and delegates page resolution to `@simplemodule/client`:

```tsx
import { createInertiaApp, router } from '@inertiajs/react';
import { resolvePage } from '@simplemodule/client/resolve-page';
import { createRoot } from 'react-dom/client';

createInertiaApp({
  resolve: resolvePage,
  setup({ el, App, props }) {
    createRoot(el).render(<App {...props} />);
  },
});
```

The ClientApp also handles non-Inertia error responses (404, 500, etc.) by intercepting invalid responses on the `router` and displaying a toast notification instead of the default Inertia error.

## Dynamic Page Resolution

When Inertia needs to render a page, `resolvePage` splits the route name to determine which module bundle to load:

```ts
// @simplemodule/client/resolve-page.ts
export async function resolvePage(name: string) {
  const moduleName = name.split('/')[0];
  const mod = await import(`/_content/${moduleName}/${moduleName}.pages.js`);
  const page = mod.pages[name];

  // Support lazy page entries: () => import('./SomePage')
  if (typeof page === 'function') {
    const resolved = await page();
    return resolved.default ? resolved : { default: resolved };
  }

  return page.default ? page : { default: page };
}
```

For a route name like `Products/Browse`:
1. The module name `Products` is extracted from the first segment
2. The bundle `/_content/Products/Products.pages.js` is dynamically imported
3. The `pages` record is looked up for the key `Products/Browse`
4. Lazy entries (functions) are resolved, eager entries are returned directly

A cache-buster query parameter is appended from a `<meta name="cache-buster">` tag when present, ensuring browsers pick up new builds without stale caches.

## The @simplemodule/client Package

The `@simplemodule/client` package (`packages/SimpleModule.Client/`) provides the core frontend infrastructure:

| Export | Purpose |
|---|---|
| `@simplemodule/client/resolve-page` | Page resolution for Inertia's `resolve` callback |
| `@simplemodule/client/module` | `defineModuleConfig()` -- unified Vite config factory for modules |
| `@simplemodule/client/vite` | Vendor build plugin and externalization helpers |

## Type Safety

The source generator discovers C# types marked with the `[Dto]` attribute and embeds TypeScript interface definitions. The `scripts/extract-ts-types.mjs` script extracts these into `.ts` files under `ClientApp/types/`, giving React components full type safety over server-provided props:

```tsx
import type { Product } from '../types';

export default function Browse({ products }: { products: Product[] }) {
  return (
    <PageShell title="Products" description="Browse the product catalog.">
      {products.map((p) => (
        <Card key={p.id}>
          <CardContent>
            <span>{p.name}</span>
            <span>${p.price.toFixed(2)}</span>
          </CardContent>
        </Card>
      ))}
    </PageShell>
  );
}
```

## Next Steps

- [Pages Registry](/frontend/pages) -- how page components are resolved and loaded
- [UI Components](/frontend/components) -- the shared Radix UI component library
- [Styling & Theming](/frontend/styling) -- Tailwind CSS configuration and theme customization
