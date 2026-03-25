---
name: minimal-api
description: >
  Write correct ASP.NET Minimal API endpoints in the SimpleModule modular monolith.
  Use when creating, modifying, or reviewing any IEndpoint or IViewEndpoint implementation,
  or when writing endpoint code that uses parameter binding, authorization, validation,
  CrudEndpoints helpers, Inertia.Render, or form handling. Triggers on: "add endpoint",
  "create endpoint", "new endpoint", "API endpoint", "view endpoint", "MapGet", "MapPost",
  "MapPut", "MapDelete", "Inertia.Render", route handling, parameter binding questions,
  or any work touching files in Endpoints/ or Views/ directories.
---

# SimpleModule Minimal API Endpoints

## Two Endpoint Types

| Interface | Purpose | Auto-applied by generator |
|-----------|---------|--------------------------|
| `IEndpoint` | API (JSON) | `.WithTags("Module")` + `.RequireAuthorization()` on route group |
| `IViewEndpoint` | Inertia views (SSR) | `.WithTags("Module")` + `.ExcludeFromDescription()` + `.RequireAuthorization()` on group |

Both implement `void Map(IEndpointRouteBuilder app)`. The source generator discovers them and maps them to route groups using the module's `RoutePrefix` / `ViewPrefix`.

## Endpoint File Structure

One endpoint per file. Class name = `{Action}Endpoint`. Place in:
- `Endpoints/{Feature}/` for `IEndpoint`
- `Views/` for `IViewEndpoint`

## Parameter Binding Rules

### Implicit binding (no attribute needed)
- **Route params**: `app.MapGet("/{id}", (ProductId id, ...) => ...)`
- **Query params** (GET): simple types bind from query string
- **JSON body** (POST/PUT/DELETE): complex types bind from request body
- **DI services**: auto-injected when registered in container
- **Special types**: `HttpContext`, `ClaimsPrincipal`, `CancellationToken` -- auto-bound

### Explicit attributes required
- **`[FromForm]`** -- ALWAYS required for form data. Never implicit. Add `.DisableAntiforgery()` for form posts without CSRF tokens.
- **`[AsParameters]`** -- bind a class from multiple sources (route + query + header). Use for GET endpoints with multiple query filters.
- **`[FromQuery]`** -- only when name conflicts with route param
- **`[FromHeader(Name = "X-Header")]`** -- for HTTP headers
- **`[FromRoute]`** -- only when disambiguation needed

### Anti-patterns (NEVER do these)
```csharp
// BAD: manual form reading
var form = await context.Request.ReadFormAsync();
var name = form["name"].ToString();

// BAD: manual JSON deserialization
var body = await JsonSerializer.DeserializeAsync<MyType>(context.Request.Body);

// BAD: [FromServices] on DI types (auto-detected, attribute is noise)
([FromServices] IProductContracts products) => ...
```

## API Endpoint Patterns

Use `CrudEndpoints` helpers from Core for standard operations. See [references/patterns.md](references/patterns.md) for all patterns.

**Quick reference:**
```csharp
// GET all
CrudEndpoints.GetAll(contracts.GetAllAsync)

// GET by id (returns 404 if null)
CrudEndpoints.GetById(() => contracts.GetByIdAsync(id))

// POST create (returns 201 with Location header)
CrudEndpoints.Create(() => contracts.CreateAsync(request), p => $"/api/things/{p.Id}")

// PUT update
CrudEndpoints.Update(() => contracts.UpdateAsync(id, request))

// DELETE
CrudEndpoints.Delete(() => contracts.DeleteAsync(id))
```

## View Endpoint Patterns

```csharp
// Render page with props (props serialize to camelCase)
Inertia.Render("Module/PageName", new { items = await svc.GetAllAsync() })

// Form: GET renders form, POST processes submission
app.MapGet("/create", () => Inertia.Render("Module/Create"));
app.MapPost("/", async ([FromForm] string name, [FromForm] decimal price, ISvc svc) => {
    await svc.CreateAsync(new CreateRequest { Name = name, Price = price });
    return Results.Redirect("/module/manage");
}).DisableAntiforgery();
```

**Critical**: Every `IViewEndpoint` with `Inertia.Render("Module/Page", ...)` MUST have a matching entry in `Pages/index.ts`. Run `npm run validate-pages` to verify.

**Note**: CA1812 ("internal class never instantiated") is suppressed via `.editorconfig` for `Endpoints/` and `Views/` directories. No `[SuppressMessage]` attributes needed on payload classes.

## Authorization

```csharp
// Permission-based (preferred for API endpoints)
.RequirePermission(ModulePermissions.Create)

// Role-based (for admin views)
.RequireAuthorization(policy => policy.RequireRole("Admin"))

// Public access
.AllowAnonymous()
```

Note: `.RequireAuthorization()` is already applied to the route group by the source generator. Endpoint-level auth narrows or overrides this.

## Validation

Use validator classes for complex validation, inline checks for simple cases:

```csharp
// Validator class pattern
var validation = CreateRequestValidator.Validate(request);
if (!validation.IsValid)
    throw new ValidationException(validation.Errors);

// Simple inline
if (string.IsNullOrWhiteSpace(request.Name))
    throw new ArgumentException("Name is required.", nameof(request));
```

## Response Types

Prefer `TypedResults.*` for OpenAPI documentation:

```csharp
TypedResults.Ok(data)              // 200
TypedResults.Created(uri, data)    // 201
TypedResults.NoContent()           // 204
TypedResults.NotFound()            // 404
Results.Redirect(url)              // 302 (for view redirects)
Results.File(bytes, type, name)    // file download
```

## Detailed Patterns

For payload transformation, route groups, file downloads, `[AsParameters]`, and advanced scenarios, see [references/patterns.md](references/patterns.md).
