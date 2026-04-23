---
outline: deep
---

# Pages Registry

Every module that renders UI must maintain a **pages registry** -- a `Pages/index.ts` file that maps route names to React components. This is the bridge between C# endpoints and the React frontend.

## The Pattern

Each module exports a `pages` record from `Pages/index.ts`:

```ts
// modules/Products/src/SimpleModule.Products/Pages/index.ts
export const pages: Record<string, unknown> = {
  'Products/Browse': () => import('./Browse'),
  'Products/Manage': () => import('./Manage'),
  'Products/Create': () => import('./Create'),
  'Products/Edit': () => import('./Edit'),
};
```

Each key matches the component name passed to `Inertia.Render()` on the server side, and each value is a lazy import pointing to the React component.

## How It Connects to C# Endpoints

On the backend, view endpoints call `Inertia.Render` with a component name:

```csharp
// modules/Products/src/SimpleModule.Products/Pages/BrowseEndpoint.cs
public class BrowseEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/browse",
                async (IProductContracts products) =>
                    Inertia.Render(
                        "Products/Browse",  // <-- This key must exist in Pages/index.ts
                        new { products = await products.GetAllProductsAsync() }
                    )
            )
            .AllowAnonymous();
    }
}
```

The string `"Products/Browse"` is the key that `resolvePage` looks up in the module's `pages` record. If the key does not exist, `resolvePage` throws an explicit error and the ClientApp surfaces a toast notification.

::: danger Missing entries throw an explicit error
If you add a new `IViewEndpoint` with `Inertia.Render("Products/Something")` but forget to add a matching entry in `Pages/index.ts`:

- The endpoint compiles and runs fine on the server
- Navigating to that page causes `resolvePage` to throw:
  `Error: Page "Products/Something" not found in module "Products". Available pages: ...`
- The ClientApp surfaces this via a toast notification (not a silent 404), and the error is logged to the browser console

**Always add the pages registry entry immediately when creating a new view endpoint** -- the error is visible, but it still breaks navigation for users.
:::

## The Rule

For every `IViewEndpoint` that calls `Inertia.Render("ModuleName/PageName", ...)`, there must be a matching entry in that module's `Pages/index.ts`:

```ts
'ModuleName/PageName': () => import('./PageName'),
```

## Adding a New Page Step-by-Step

1. **Create the C# endpoint** implementing `IViewEndpoint`:

   ```csharp
   public class DetailsEndpoint : IViewEndpoint
   {
       public void Map(IEndpointRouteBuilder app)
       {
           app.MapGet("/{id}", (int id, IProductContracts products) =>
               Inertia.Render("Products/Details", new { product = ... }));
       }
   }
   ```

2. **Create the React component** in the module's `Pages/` directory:

   ```tsx
   // modules/Products/src/SimpleModule.Products/Pages/Details.tsx
   import { PageShell } from '@simplemodule/ui';
   import type { Product } from '../types';

   export default function Details({ product }: { product: Product }) {
     return (
       <PageShell title={product.name}>
         {/* ... */}
       </PageShell>
     );
   }
   ```

3. **Register in `Pages/index.ts`** immediately:

   ```ts
   export const pages: Record<string, unknown> = {
     'Products/Browse': () => import('./Browse'),
     'Products/Manage': () => import('./Manage'),
     'Products/Create': () => import('./Create'),
     'Products/Edit': () => import('./Edit'),
     'Products/Details': () => import('./Details'), // [!code ++]
   };
   ```

4. **Validate** with the validation script:

   ```bash
   npm run validate-pages
   ```

## Validating Page Registrations {#validate-pages}

The `validate-pages` script automatically checks that all C# endpoints have corresponding TypeScript entries and vice versa.

### What It Does

1. Scans all `.cs` files in each module's `src/{ModuleName}/` directory
2. Finds all `Inertia.Render("ComponentName/...")` calls via regex
3. Reads the module's `Pages/index.ts` file
4. Extracts all keys from the `pages` record
5. Compares the two lists and reports mismatches

### Running It

```bash
npm run validate-pages
```

On success:

```
=== Pages Registry Validation ===

All modules have valid Pages/index.ts registrations
```

On failure:

```
=== Pages Registry Validation ===

Module: Products
   Missing in Pages/index.ts: Products/Details

Found 1 module(s) with mismatches
Please update the Pages/index.ts files to match C# endpoints.
```

### Exit Codes

| Code | Meaning |
|---|---|
| `0` | All modules have valid registrations |
| `1` | Mismatches found (missing or extra entries) |

### CI Integration

The script exits with code 1 on failure, making it suitable for CI pipelines. Add it as a step in your build workflow to catch missing registrations before they reach production.

## Lazy vs Eager Imports

The pages registry supports both lazy and eager component imports:

```ts
// Lazy (recommended) -- component is loaded on demand
'Products/Browse': () => import('./Browse'),

// Eager -- component is bundled into the pages.js file
'Products/Browse': Browse,
```

Lazy imports are recommended because they allow Vite to code-split within the module bundle. The `resolvePage` function handles both patterns transparently.

## Next Steps

- [UI Components](/frontend/components) -- the shared Radix UI component library
- [Styling & Theming](/frontend/styling) -- Tailwind CSS configuration and theming
- [Vite Build System](/frontend/vite) -- how module bundles are built
