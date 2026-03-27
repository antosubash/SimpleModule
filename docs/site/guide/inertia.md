---
outline: deep
---

# Inertia.js Integration

SimpleModule uses [Inertia.js](https://inertiajs.com/) to bridge the server-side .NET backend with a React frontend. Instead of building a separate API + SPA, endpoints return Inertia responses that render React components with server-provided props -- giving you the DX of a SPA with the architecture of a server-rendered app.

## How It Works

The Inertia integration in SimpleModule has three layers:

1. **ASP.NET endpoints** call `Inertia.Render()` to specify a component name and props
2. **Blazor SSR** renders the HTML shell with the serialized page data
3. **React ClientApp** hydrates the page by dynamically importing the correct module's page bundle

## Request Flow

### Initial Page Load

On the first request (full page load), the flow is:

```
Browser GET /products/browse
    ↓
ASP.NET route handler
    → Inertia.Render("Products/Browse", { products: [...] })
    ↓
InertiaResult.ExecuteAsync()
    → Serializes page data (component, props, url, version) as JSON
    → Delegates to IInertiaPageRenderer
    ↓
InertiaPageRenderer (Blazor SSR)
    → Renders InertiaShell component with page JSON
    → Returns full HTML document
    ↓
Browser receives HTML
    → React's createInertiaApp hydrates the page
    → resolvePage() imports Products.pages.js
    → "Products/Browse" component renders with props
```

### Subsequent Navigation

On subsequent navigation (Inertia XHR requests), the flow is shorter:

```
Browser clicks Inertia link
    → XHR GET /products/browse (with X-Inertia header)
    ↓
ASP.NET route handler
    → Inertia.Render("Products/Browse", { products: [...] })
    ↓
InertiaResult.ExecuteAsync()
    → Detects X-Inertia header
    → Returns JSON response (not HTML)
    ↓
Inertia.js client
    → Swaps page component with new props
    → No full page reload
```

## Server Side

### Inertia.Render()

The static `Inertia.Render()` method creates an `IResult` that handles both full page loads and XHR requests:

```csharp
using SimpleModule.Core.Inertia;

public class BrowseEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/browse",
            async (IProductContracts products) =>
                Inertia.Render(
                    "Products/Browse",
                    new { products = await products.GetAllProductsAsync() }
                )
        );
    }
}
```

Parameters:
- **`component`** -- the page name (e.g., `"Products/Browse"`). Must match an entry in the module's `Pages/index.ts`.
- **`props`** -- an anonymous object or any serializable type. Serialized as camelCase JSON.

### Props Serialization

Props are serialized using `System.Text.Json` with `JsonNamingPolicy.CamelCase`:

```csharp
// Server
Inertia.Render("Products/Edit", new { product });

// Client receives:
// { "component": "Products/Edit", "props": { "product": { "id": 1, "name": "..." } } }
```

::: warning
Property names are automatically converted to camelCase. A C# property `ProductName` becomes `productName` in JavaScript.
:::

### Shared Data

Use `InertiaSharedData` to share props across all Inertia responses in a single HTTP request. This is useful for data that every page needs (current user, flash messages, etc.):

```csharp
public sealed class InertiaSharedData
{
    public void Set(string key, object? value);
    public T? Get<T>(string key, T? defaultValue = default);
    public bool Remove(string key);
    public bool Contains(string key);
    public IReadOnlyDictionary<string, object?> All { get; }
}
```

`InertiaSharedData` is registered as a **scoped** service. Set values in middleware or endpoint filters, and they are automatically merged into every Inertia response for that request:

```csharp
app.Use(async (context, next) =>
{
    var sharedData = context.RequestServices.GetRequiredService<InertiaSharedData>();
    sharedData.Set("appName", "My Application");
    sharedData.Set("user", new { name = context.User.Identity?.Name });
    await next();
});
```

Shared data has **lower priority** than endpoint props. If an endpoint sets a prop with the same key as shared data, the endpoint's value wins.

### Version Detection

The Inertia middleware handles asset versioning to prevent stale JavaScript from running after deployments:

```csharp
app.UseInertia(); // Add to middleware pipeline
```

The middleware:

1. Sets `X-Inertia-Version` on every response
2. On XHR requests (`X-Inertia` header present), compares the client's version with the server's
3. If versions differ, returns `409 Conflict` with `X-Inertia-Location` header, triggering a full page reload
4. Converts `302` redirects to `303` for PUT/PATCH/DELETE requests (Inertia protocol requirement)

The version is determined by:
1. `DEPLOYMENT_VERSION` environment variable (for rolling deployments)
2. Assembly version as fallback

## Blazor SSR Shell

The `InertiaShell` Blazor component renders the HTML document with embedded page data:

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <meta name="cache-buster" content="@CacheBuster" />
    <script type="importmap">
    {
        "imports": {
            "react": "/js/vendor/react.js",
            "react-dom": "/js/vendor/react-dom.js",
            "react/jsx-runtime": "/js/vendor/react-jsx-runtime.js",
            "react-dom/client": "/js/vendor/react-dom-client.js",
            "@inertiajs/react": "/js/vendor/inertiajs-react.js"
        }
    }
    </script>
</head>
<body>
    <!-- Blazor layout wraps the Inertia page data -->
    <InertiaPage PageJson="@PageJson" />
</body>
</html>
```

The `InertiaPageRenderer` uses Blazor's `HtmlRenderer` to produce static HTML server-side:

```csharp
public sealed class InertiaPageRenderer(
    IServiceProvider services,
    ILoggerFactory loggerFactory,
    IOptions<InertiaOptions> options
) : IInertiaPageRenderer
{
    public async Task RenderPageAsync(HttpContext httpContext, string pageJson)
    {
        await using var renderer = new HtmlRenderer(services, loggerFactory);
        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync(
                options.Value.ShellComponent,
                ParameterView.FromDictionary(new Dictionary<string, object?>
                {
                    ["PageJson"] = pageJson,
                    ["HttpContext"] = httpContext,
                })
            );
            return output.ToHtmlString();
        });

        httpContext.Response.ContentType = "text/html; charset=utf-8";
        await httpContext.Response.WriteAsync(html);
    }
}
```

You can customize the shell component via `InertiaOptions`:

```csharp
builder.Services.AddSimpleModuleBlazor(options =>
{
    options.ShellComponent = typeof(MyCustomShell);
});
```

## Client Side

### App Bootstrap

The React app is bootstrapped in `ClientApp/app.tsx`:

```typescript
import { createInertiaApp } from '@inertiajs/react';
import { resolvePage } from '@simplemodule/client/resolve-page';
import { createRoot } from 'react-dom/client';

createInertiaApp({
  resolve: resolvePage,
  setup({ el, App, props }) {
    createRoot(el).render(<App {...props} />);
  },
});
```

### Page Resolution

The `resolvePage` function dynamically imports module page bundles based on the component name:

```typescript
export async function resolvePage(name: string) {
  const moduleName = name.split('/')[0];
  const cacheBuster = (
    document.querySelector('meta[name="cache-buster"]') as HTMLMetaElement
  )?.content;
  const suffix = cacheBuster ? `?v=${cacheBuster}` : '';

  const mod = await import(
    `/_content/${moduleName}/${moduleName}.pages.js${suffix}`
  );

  const page = mod.pages[name];
  // Supports lazy entries: () => import('./SomePage')
  if (typeof page === 'function') {
    const resolved = await page();
    return resolved.default ? resolved : { default: resolved };
  }

  return page.default ? page : { default: page };
}
```

For a component name like `"Products/Browse"`:
1. Extracts module name: `"Products"`
2. Imports `/_content/Products/Products.pages.js`
3. Looks up `"Products/Browse"` in the `pages` export
4. Supports lazy loading via function entries

### Module Pages Registry

Each module exports a `pages` record in `Pages/index.ts`:

```typescript
// modules/Products/src/Products/Pages/index.ts
export const pages: Record<string, any> = {
  'Products/Browse': () => import('../Views/Browse'),
  'Products/Manage': () => import('../Views/Manage'),
  'Products/Create': () => import('../Views/Create'),
  'Products/Edit': () => import('../Views/Edit'),
};
```

::: danger Critical
Every `IViewEndpoint` that calls `Inertia.Render("Module/Page", ...)` **must** have a matching entry in the module's `Pages/index.ts`. Missing entries silently fail with no error in the console. Run `npm run validate-pages` to catch mismatches.
:::

### Writing a Page Component

Page components receive props from the server as React props:

```tsx
import { PageHeader } from '@simplemodule/ui/components';

interface BrowseProps {
  products: Product[];
}

export default function Browse({ products }: BrowseProps) {
  return (
    <div>
      <PageHeader title="Products" />
      <ul>
        {products.map((p) => (
          <li key={p.id}>{p.name} - ${p.price}</li>
        ))}
      </ul>
    </div>
  );
}
```

### Error Handling

The ClientApp handles non-Inertia error responses (404, 500, etc.) by intercepting the `invalid` event on the Inertia router:

```typescript
router.on('invalid', (event) => {
  event.preventDefault();
  const response = event.detail.response;
  const body = response.data as { detail?: string; title?: string } | undefined;
  const message = body?.detail ?? body?.title ?? `Server error (${response.status})`;
  showErrorToast(message);
});
```

Instead of showing the default "must receive a valid Inertia response" error, a toast notification displays the server error message.

## Full Example

Here is the complete flow for a Products/Browse page:

**1. Endpoint (C#):**

```csharp
public class BrowseEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/browse",
            async (IProductContracts products) =>
                Inertia.Render(
                    "Products/Browse",
                    new { products = await products.GetAllProductsAsync() }
                )
        ).AllowAnonymous();
    }
}
```

**2. Page registry (TypeScript):**

```typescript
// Pages/index.ts
export const pages: Record<string, any> = {
  'Products/Browse': () => import('../Views/Browse'),
};
```

**3. Page component (React):**

```tsx
// Views/Browse.tsx
export default function Browse({ products }: { products: Product[] }) {
  return (
    <div>
      <h1>Products</h1>
      {products.map((p) => (
        <div key={p.id}>{p.name}</div>
      ))}
    </div>
  );
}
```

**4. What happens at runtime:**

1. User navigates to `/products/browse`
2. ASP.NET matches the route, calls the endpoint handler
3. `IProductContracts.GetAllProductsAsync()` fetches products from the database
4. `Inertia.Render("Products/Browse", { products })` serializes the page data
5. On initial load: Blazor SSR renders the full HTML shell with embedded JSON
6. React hydrates, `resolvePage("Products/Browse")` imports `Products.pages.js`
7. The Browse component renders with the server-provided products array
8. On subsequent navigation: only JSON is returned, React swaps the component

## Next Steps

- [Frontend Overview](/frontend/overview) -- the complete React + Inertia.js architecture
- [Pages Registry](/frontend/pages) -- how page components are resolved at runtime
- [Vite Build System](/frontend/vite) -- module-scoped library mode builds
