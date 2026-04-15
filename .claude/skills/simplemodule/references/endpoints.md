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
- `Views/` for `IViewEndpoint`

## API Endpoints with CrudEndpoints Helper

```csharp
// GET all — returns 200 OK
public class GetAllEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/", (IProductContracts contracts) =>
            CrudEndpoints.GetAll(contracts.GetAllProductsAsync))
        .RequirePermission(ProductsPermissions.View);
}

// GET by ID — returns 200 OK or 404 NotFound
public class GetByIdEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/{id}", (ProductId id, IProductContracts contracts) =>
            CrudEndpoints.GetById(() => contracts.GetProductByIdAsync(id)))
        .RequirePermission(ProductsPermissions.View);
}

// POST create — returns 201 Created with Location header
public class CreateEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/", (CreateProductRequest request, IProductContracts contracts) =>
        {
            var validation = CreateRequestValidator.Validate(request);
            if (!validation.IsValid) throw new ValidationException(validation.Errors);
            return CrudEndpoints.Create(
                () => contracts.CreateProductAsync(request),
                p => $"{ProductsConstants.RoutePrefix}/{p.Id}");
        })
        .RequirePermission(ProductsPermissions.Create);
}

// PUT update — returns 200 OK
public class UpdateEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/{id}", (ProductId id, UpdateProductRequest request, IProductContracts contracts) =>
        {
            var validation = UpdateRequestValidator.Validate(request);
            if (!validation.IsValid) throw new ValidationException(validation.Errors);
            return CrudEndpoints.Update(() => contracts.UpdateProductAsync(id, request));
        })
        .RequirePermission(ProductsPermissions.Update);
}

// DELETE — returns 204 NoContent
public class DeleteEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete("/{id}", (ProductId id, IProductContracts contracts) =>
            CrudEndpoints.Delete(() => contracts.DeleteProductAsync(id)))
        .RequirePermission(ProductsPermissions.Delete);
}
```

## View Endpoints with Inertia

```csharp
// Browse page (public)
public class BrowseEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/browse", async (IProductContracts products) =>
            Inertia.Render("Products/Browse",
                new { products = await products.GetAllProductsAsync() }))
        .AllowAnonymous();
}

// Form page with GET (render) + POST (submit)
public class CreateEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/create", () => Inertia.Render("Products/Create"));

        app.MapPost("/", async (
            [FromForm] string name,
            [FromForm] decimal price,
            IProductContracts products) =>
        {
            await products.CreateProductAsync(new CreateProductRequest { Name = name, Price = price });
            return Results.Redirect("/products/manage");
        }).DisableAntiforgery();
    }
}
```

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
