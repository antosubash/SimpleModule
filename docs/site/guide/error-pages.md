---
outline: deep
---

# Error Pages

SimpleModule ships a consistent error-handling pipeline that serves the right response shape depending on who's asking:

- **Inertia (browser) requests** get a React error page rendered as a full Inertia response.
- **API / fetch requests** get an RFC 7807 `ProblemDetails` JSON body.
- **Catastrophic failures** (exception before Inertia can render) fall back to a static `wwwroot/error.html`.

No per-module wiring is required — everything below is turned on by `AddSimpleModuleInfrastructure()` and `UseSimpleModule()`.

## How Dispatch Works

### Thrown exceptions

The framework registers `GlobalExceptionHandler` via `AddExceptionHandler<GlobalExceptionHandler>()`. It maps domain exceptions to HTTP status codes:

| Exception | Status | Notes |
|-----------|--------|-------|
| `ValidationException` | 400 | Errors serialized into the `errors` extension |
| `ArgumentException` | 400 | Fallback for unchecked arg errors |
| `NotFoundException` | 404 | Prefer this over returning `NotFound()` for domain misses |
| `ForbiddenException` | 403 | For authorization failures surfaced from services |
| `ConflictException` | 409 | Optimistic concurrency, duplicate keys, etc. |
| _anything else_ | 500 | Logged at `Error`; the response message is `ErrorMessages.UnexpectedError` |

The handler inspects `X-Inertia` on the request:

- **Present** → writes an Inertia page JSON with component `Error/{statusCode}` and props `{ status, title, message }`.
- **Absent** → writes `ProblemDetails` JSON.

### Unmatched routes

`UseSimpleModule()` registers a `MapFallback` for `GET` requests so browser navigation to a non-existent URL gets a 404 page instead of a bare 404:

```text
GET /some/unknown/path
  → MapFallback
  → RenderErrorPage(404)
  → Inertia.Render("Error/404", { status, title, message })
```

The fallback only fires for unmatched requests. Endpoints that return bare `401`/`403` from authentication middleware are untouched, so API tests that assert on those status codes keep working.

### Direct error URLs

`GET /error/{statusCode}` renders an Inertia error page for any status — useful for linking from emails, redirects, or testing.

```text
GET /error/403
  → Inertia.Render("Error/403", { status: 403, title: "...", message: "..." })
```

### Static fallback

If an exception occurs so early that Inertia can't render (e.g., DI resolution failure), `UseExceptionHandler` writes the contents of `wwwroot/error.html` with a 500 status. Keep this file lean — it must render without any server state.

## Raising Domain Errors

Throw the framework exceptions from services or endpoints; the handler takes care of the status code and response shape.

```csharp
public async Task<Product> GetProductAsync(ProductId id)
{
    var product = await db.Products.FindAsync(id);
    if (product is null)
    {
        throw new NotFoundException("Product", id);
    }
    return product;
}
```

```csharp
public async Task<Order> CancelOrderAsync(OrderId id, UserId actor)
{
    var order = await db.Orders.FindAsync(id);
    if (order is null)
    {
        throw new NotFoundException("Order", id);
    }
    if (order.OwnerId != actor)
    {
        throw new ForbiddenException("You cannot cancel another user's order.");
    }
    // ...
}
```

## React Error Components

`template/SimpleModule.Host/ClientApp/app.tsx` maps the `Error/*` component names to React components before any normal page resolution runs:

```tsx
const ERROR_PAGES: Record<string, { default: React.ComponentType }> = {
  'Error/404': { default: ErrorPage404 },
  'Error/403': { default: ErrorPage403 },
  'Error/500': { default: ErrorPage500 },
};

createInertiaApp({
  resolve: async (name) => {
    if (name in ERROR_PAGES) {
      return ERROR_PAGES[name];
    }
    // ...normal page resolution
  },
});
```

The default components come from `@simplemodule/ui`. To customize, swap in your own component for the relevant status code. The component receives `{ status, title, message }` via Inertia props.

## Customizing

- **Change messages** — override `ErrorMessages` constants, or pass custom `title`/`message` via a new exception type.
- **Add a status code** — create a new exception mapping in `GlobalExceptionHandler` and a matching `Error/{code}` React component in `ERROR_PAGES`.
- **Static HTML fallback** — edit `template/SimpleModule.Host/wwwroot/error.html`. Keep it self-contained (inlined CSS, no external assets) so it works when the pipeline is degraded.

## Testing Error Pages

Assert on the status code and, for Inertia flows, the component name:

```csharp
[Fact]
public async Task Missing_product_returns_404_problem_details()
{
    using var client = factory.CreateAuthenticatedClient();

    var response = await client.GetAsync("/api/products/99999");

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
    problem!.Title.Should().Be("Resource not found");
}
```

For Inertia pages, send `X-Inertia: true` and assert on the JSON component field.

## Next Steps

- [Endpoints](/guide/endpoints) — how validation errors become 400 responses
- [Permissions](/guide/permissions) — how `RequirePermission` interacts with 403 responses
