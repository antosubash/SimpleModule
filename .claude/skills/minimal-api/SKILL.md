---
name: minimal-api
description: >
  Write correct ASP.NET Minimal API endpoints in the SimpleModule modular monolith.
  Use when creating, modifying, or reviewing any IEndpoint or IViewEndpoint implementation,
  or when writing endpoint code that uses parameter binding, authorization, validation,
  CrudEndpoints helpers, Inertia.Render, or form handling. Triggers on: "add endpoint",
  "create endpoint", "new endpoint", "API endpoint", "view endpoint", "MapGet", "MapPost",
  "MapPut", "MapDelete", "Inertia.Render", route handling, parameter binding questions,
  "FormRequest", "Route const", "IAuthorizer", "policy", "RequirePermission",
  or any work touching files in Endpoints/, Pages/, or FormRequests/ directories.
---

# SimpleModule Minimal API Endpoints

## Two Endpoint Types

| Interface | Purpose | Auto-applied by generator |
|-----------|---------|--------------------------|
| `IEndpoint` | API (JSON) | `.WithTags("Module")` + `.RequireAuthorization()` on route group |
| `IViewEndpoint` | Inertia views (SSR) | `.WithTags("Module")` + `.ExcludeFromDescription()` + `.RequireAuthorization()` on group |

Both implement `void Map(IEndpointRouteBuilder app)`. The source generator discovers them and maps them to route groups using the module's `RoutePrefix` / `ViewPrefix`.

## Endpoint File Structure

One endpoint per file (enforced by **SM0049**). Class name = `{Action}Endpoint`. Place in:
- `Endpoints/{Feature}/` for `IEndpoint`
- `Pages/` for `IViewEndpoint` (co-located with its `.tsx` component; optionally grouped in feature subfolders)

### Route const (required)

Every endpoint declares a `public const string Route` and passes it to its `MapXxx` call — enforced by **SM0054**. Route values live in a `Routes` nested class on the module constants:

```csharp
public class GetAllProductsEndpoint : IEndpoint
{
    public const string Route = ProductsConstants.Routes.GetAll;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(Route, (IProductContracts contracts) =>
            CrudEndpoints.GetAll(contracts.GetAllAsync))
        .RequirePermission(ProductsPermissions.View);
}
```

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

// Soft-delete-aware modules also have (204 on success, 404 if no rows affected):
CrudEndpoints.Restore(() => contracts.RestoreAsync(id))
CrudEndpoints.ForceDelete(() => contracts.ForceDeleteAsync(id))
```

## View Endpoint Patterns

```csharp
// Render page with props (props serialize to camelCase)
Inertia.Render("Module/PageName", new { items = await svc.GetAllAsync() })

// Form: GET renders form, POST binds + validates a [FormRequest] directly
app.MapGet(Route, () => Inertia.Render("Module/Create"));
app.MapPost("/things", async (CreateThingFormRequest form, ISvc svc) => {
    await svc.CreateAsync(new CreateRequest { Name = form.Name, Price = form.Price });
    return Results.Redirect("/module/manage");
});
```

### Form binding with `FormRequest<TSelf>`

Form posts bind a `[FormRequest]` class — `sealed partial`, extending `FormRequest<TSelf>` (**SM0056** / **SM0057**), in the module's `FormRequests/` folder. The framework hydrates it from the form body, runs `Prepare()`, then validates via `ConfigureRules` before the handler runs (no manual `IValidator`, no `.DisableAntiforgery()`):

```csharp
using FluentValidation;
using SimpleModule.Core.FormRequests;

[FormRequest]
public sealed partial class CreateThingFormRequest : FormRequest<CreateThingFormRequest>
{
    public string Name { get; set; } = "";
    public decimal Price { get; set; }

    public override void Prepare() => Name = Name.Trim();

    protected override void ConfigureRules(RuleConfigurator<CreateThingFormRequest> rules) =>
        rules.RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
}
```

For one-off forms, a private `sealed record` of `[FromForm]` fields bound with `[AsParameters]` (plus manual `IValidator`) still works — see [references/patterns.md](references/patterns.md).

**Critical**: Every `IViewEndpoint` with `Inertia.Render("Module/Page", ...)` MUST have a matching entry in `Pages/index.ts`. Run `npm run validate-pages` to verify.

**Note**: CA1812 ("internal class never instantiated") is suppressed via `.editorconfig` for `Endpoints/` and `Pages/` (`*Endpoint.cs`) directories. No `[SuppressMessage]` attributes needed on payload classes.

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

For **per-resource** rules (ownership, tenancy, state) beyond the coarse permission gate, inject `IAuthorizer` and call it after loading the resource — it dispatches to the resource's `IPolicy<TResource>` (deny-wins; throws `ForbiddenException` / `NotFoundException` on deny):

```csharp
await authorizer.AuthorizeAsync(user, PolicyActions.Update, product);
```

See the `simplemodule` skill's "Policies" section (diagnostics SM0058–SM0061).

## Validation

Use [FluentValidation](https://docs.fluentvalidation.net/). Define a `sealed` validator extending `AbstractValidator<T>`, register it once per module with `services.AddValidatorsFromAssemblyContaining<ThisModule>()` in `ConfigureServices`, then inject `IValidator<TRequest>` into the endpoint handler:

```csharp
// Validator (lives next to the request DTO or in a Validators/ folder)
public sealed class CreateRequestValidator : AbstractValidator<CreateRequest>
{
    public CreateRequestValidator() =>
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
}

// In the endpoint lambda (async — inject IValidator<TRequest>)
async (CreateRequest request, IValidator<CreateRequest> validator, IThingContracts contracts) =>
{
    var validation = await validator.ValidateAsync(request);
    if (!validation.IsValid)
        throw new Core.Exceptions.ValidationException(validation.ToValidationErrors());
    // ...
}
```

`ToValidationErrors()` (in `SimpleModule.Core.Validation`) converts FluentValidation's `ValidationResult` into the `Dictionary<string, string[]>` that `ValidationException` and the RFC 7807 response writer expect. For trivial guards, a plain `ArgumentException` is still fine.

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
