# Endpoint Patterns Reference

## Two Endpoint Types

| Interface | Purpose | Auto-applied by generator |
|-----------|---------|--------------------------|
| `IEndpoint` | API (JSON) | `.WithTags("Module")` + `.RequireAuthorization()` on route group |
| `IViewEndpoint` | Inertia views (SSR) | `.WithTags("Module")` + `.ExcludeFromDescription()` + `.RequireAuthorization()` on group |

Both implement `void Map(IEndpointRouteBuilder app)`. The source generator discovers them and maps them to route groups using the module's `RoutePrefix` / `ViewPrefix`.

## File Structure

One endpoint per file. Class name = `{Action}Endpoint`. Place in:
- `Endpoints/{Feature}/` for `IEndpoint`
- `Pages/` for `IViewEndpoint` (co-located with its `.tsx` component; optionally grouped in feature subfolders)

## API Endpoints with CrudEndpoints Helper

Every endpoint declares a `public const string Route` (enforced by **SM0054**) and passes it to the `MapXxx` call. Route values are centralized in a `Routes` nested class on the module's constants (e.g. `ProductsConstants.Routes.GetAll`).

```csharp
// GET all — returns 200 OK
public class GetAllEndpoint : IEndpoint
{
    public const string Route = ProductsConstants.Routes.GetAll;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(Route, (IProductContracts contracts) =>
            CrudEndpoints.GetAll(contracts.GetAllProductsAsync))
        .RequirePermission(ProductsPermissions.View);
}

// GET by ID — returns 200 OK or 404 NotFound
public class GetByIdEndpoint : IEndpoint
{
    public const string Route = ProductsConstants.Routes.GetById;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(Route, (ProductId id, IProductContracts contracts) =>
            CrudEndpoints.GetById(() => contracts.GetProductByIdAsync(id)))
        .RequirePermission(ProductsPermissions.View);
}

// POST create — returns 201 Created with Location header
public class CreateEndpoint : IEndpoint
{
    public const string Route = ProductsConstants.Routes.Create;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(Route, async (CreateProductRequest request, IValidator<CreateProductRequest> validator, IProductContracts contracts) =>
        {
            var validation = await validator.ValidateAsync(request);
            if (!validation.IsValid) throw new Core.Exceptions.ValidationException(validation.ToValidationErrors());
            return await CrudEndpoints.Create(
                () => contracts.CreateProductAsync(request),
                p => $"{ProductsConstants.RoutePrefix}/{p.Id}");
        })
        .RequirePermission(ProductsPermissions.Create);
}

// PUT update — returns 200 OK
public class UpdateEndpoint : IEndpoint
{
    public const string Route = ProductsConstants.Routes.Update;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut(Route, async (ProductId id, UpdateProductRequest request, IValidator<UpdateProductRequest> validator, IProductContracts contracts) =>
        {
            var validation = await validator.ValidateAsync(request);
            if (!validation.IsValid) throw new Core.Exceptions.ValidationException(validation.ToValidationErrors());
            return await CrudEndpoints.Update(() => contracts.UpdateProductAsync(id, request));
        })
        .RequirePermission(ProductsPermissions.Update);
}

// DELETE — returns 204 NoContent
public class DeleteEndpoint : IEndpoint
{
    public const string Route = ProductsConstants.Routes.Delete;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(Route, (ProductId id, IProductContracts contracts) =>
            CrudEndpoints.Delete(() => contracts.DeleteProductAsync(id)))
        .RequirePermission(ProductsPermissions.Delete);
}
```

`CrudEndpoints` also provides `Restore(() => contracts.RestoreAsync(id))` and `ForceDelete(() => contracts.ForceDeleteAsync(id))` for soft-delete-aware modules.

## View Endpoints with Inertia

```csharp
// Browse page (public)
public class BrowseEndpoint : IViewEndpoint
{
    public const string Route = ProductsConstants.Routes.Browse;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(Route, async (IProductContracts products) =>
            Inertia.Render("Products/Browse",
                new { products = await products.GetAllProductsAsync() }))
        .AllowAnonymous();
}

// Form page with GET (render) + POST (submit)
public class CreateEndpoint : IViewEndpoint
{
    public const string Route = ProductsConstants.Routes.CreatePage;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(Route, () => Inertia.Render("Products/Create"));

        // Bind + validate a [FormRequest] directly (see "Form Binding" below).
        app.MapPost("/products", async (CreateProductFormRequest form, IProductContracts products) =>
        {
            await products.CreateProductAsync(new CreateProductRequest { Name = form.Name, Price = form.Price });
            return Results.Redirect("/products/manage");
        });
    }
}
```

A view endpoint with several routes (GET page + POST submit + DELETE) still declares a single `public const string Route` for the primary route; the remaining `MapXxx` calls may use literals or additional consts.

## Form Binding (FormRequest)

For form posts, declare a `[FormRequest]` class in the module's `FormRequests/` folder. It must be `sealed partial` and extend `FormRequest<TSelf>` (enforced by **SM0056** / **SM0057**). Bind it **directly** as a handler parameter — the framework hydrates it from the form body, runs `Prepare()`, then validates via `ConfigureRules` before your handler executes. No manual `IValidator` call and no `.DisableAntiforgery()` needed.

```csharp
using FluentValidation;
using SimpleModule.Core.FormRequests;

namespace SimpleModule.Products.FormRequests;

[FormRequest]
public sealed partial class CreateProductFormRequest : FormRequest<CreateProductFormRequest>
{
    public string Name { get; set; } = "";
    public decimal Price { get; set; }

    public override void Prepare() => Name = Name.Trim();

    protected override void ConfigureRules(RuleConfigurator<CreateProductFormRequest> rules)
    {
        rules.RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
        rules.RuleFor(x => x.Price).GreaterThan(0).WithMessage("Price must be greater than zero.");
    }
}
```

For one-off forms you can still bind a private `sealed record` of `[FromForm]` fields via `[AsParameters]` and validate manually with `IValidator<T>`, but `FormRequest<TSelf>` is preferred for anything reused or validated.

## Parameter Binding Rules

### Implicit (no attribute needed)
- **Route params**: `(ProductId id, ...) => ...`
- **Query params** (GET): simple types bind from query string
- **JSON body** (POST/PUT/DELETE): complex types bind from request body
- **DI services**: auto-injected when registered
- **Special types**: `HttpContext`, `ClaimsPrincipal`, `CancellationToken`

### Explicit attributes required
- **`[FromForm]`** — ALWAYS required for form data. Add `.DisableAntiforgery()` for CSRF-free forms.
- **`[AsParameters]`** — bind a class from multiple sources (route + query + header)
- **`[FromQuery]`** — only when name conflicts with route param
- **`[FromHeader(Name = "X-Header")]`** — for HTTP headers

### Anti-patterns (NEVER do these)
```csharp
// BAD: manual form reading
var form = await context.Request.ReadFormAsync();

// BAD: manual JSON deserialization
var body = await JsonSerializer.DeserializeAsync<MyType>(context.Request.Body);

// BAD: [FromServices] on DI types (auto-detected)
([FromServices] IProductContracts products) => ...
```

## Authorization

```csharp
// Permission-based (preferred)
.RequirePermission(ModulePermissions.Create)

// Role-based
.RequireAuthorization(policy => policy.RequireRole("Admin"))

// Public access
.AllowAnonymous()
```

`.RequireAuthorization()` is already applied to the route group by the source generator.

### Instance-level authorization (policies)

`.RequirePermission(...)` is the coarse capability gate. For per-resource rules (ownership, tenancy, state), inject `IAuthorizer` and call it after loading the resource — it dispatches to the resource type's `IPolicy<TResource>` with deny-wins semantics:

```csharp
async (ProductId id, IAuthorizer authorizer, ClaimsPrincipal user, IProductContracts products) =>
{
    var product = await products.GetProductByIdAsync(id);
    if (product is null) return Results.NotFound();
    // Throws ForbiddenException (or NotFoundException for anti-enumeration actions) on deny.
    await authorizer.AuthorizeAsync(user, PolicyActions.Update, product);
    // ... proceed
}
```

See the SimpleModule skill's "Policies" section for declaring `IPolicy<TResource>` (diagnostics SM0058–SM0061).

## Validation

Use FluentValidation `AbstractValidator<T>`. Register via `services.AddValidatorsFromAssemblyContaining<YourModule>()` in `ConfigureServices`. Inject `IValidator<TRequest>` into the endpoint handler.

```csharp
public sealed class CreateRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Product name is required.");
        RuleFor(x => x.Price).GreaterThan(0).WithMessage("Price must be greater than zero.");
    }
}

// In the endpoint lambda:
async (
    CreateProductRequest request,
    IValidator<CreateProductRequest> validator,
    IProductContracts products
) =>
{
    var validation = await validator.ValidateAsync(request);
    if (!validation.IsValid)
        throw new Core.Exceptions.ValidationException(validation.ToValidationErrors());
    // ...
}
```

## Response Types

```csharp
TypedResults.Ok(data)              // 200
TypedResults.Created(uri, data)    // 201
TypedResults.NoContent()           // 204
TypedResults.NotFound()            // 404
Results.Redirect(url)              // 302 (for view redirects)
Results.File(bytes, type, name)    // file download
```

## Exception Handling

Exceptions are caught by `GlobalExceptionHandler` and mapped to HTTP responses:

| Exception | Status | Response |
|-----------|--------|----------|
| `ValidationException` | 400 | ProblemDetails with field errors |
| `NotFoundException` | 404 | ProblemDetails |
| `ConflictException` | 409 | ProblemDetails |
| Other | 500 | ProblemDetails |
