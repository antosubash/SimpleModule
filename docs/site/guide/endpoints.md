---
outline: deep
---

# Endpoints

Endpoints are the HTTP entry points into your module. SimpleModule provides two endpoint interfaces: `IEndpoint` for API endpoints that return JSON, and `IViewEndpoint` for Inertia view endpoints that render React pages. Both are auto-discovered by the source generator -- you never register them manually.

## `IEndpoint` -- API Endpoints

The `IEndpoint` interface has a single method:

```csharp
public interface IEndpoint
{
    void Map(IEndpointRouteBuilder app);
}
```

Inside `Map`, you use ASP.NET Minimal API methods (`MapGet`, `MapPost`, `MapPut`, `MapDelete`) to define your route. The `app` parameter is already scoped to your module's `RoutePrefix`, so you define routes relative to that prefix.

### Example: Full CRUD

Here is the complete set of API endpoints from the Products module. The module's `RoutePrefix` is `"/api/products"`, so `"/"` maps to `/api/products` and `"/{id}"` maps to `/api/products/{id}`.

**GET all products:**

```csharp
public class GetAllEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/",
                (IProductContracts productContracts) =>
                    CrudEndpoints.GetAll(productContracts.GetAllProductsAsync)
            )
            .RequirePermission(ProductsPermissions.View);
}
```

**GET by ID:**

```csharp
public class GetByIdEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/{id}",
                (ProductId id, IProductContracts productContracts) =>
                    CrudEndpoints.GetById(
                        () => productContracts.GetProductByIdAsync(id))
            )
            .RequirePermission(ProductsPermissions.View);
}
```

**POST (create):**

```csharp
public class CreateEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                "/",
                (CreateProductRequest request, IProductContracts productContracts) =>
                {
                    var validation = CreateRequestValidator.Validate(request);
                    if (!validation.IsValid)
                    {
                        throw new ValidationException(validation.Errors);
                    }

                    return CrudEndpoints.Create(
                        () => productContracts.CreateProductAsync(request),
                        p => $"{ProductsConstants.RoutePrefix}/{p.Id}"
                    );
                }
            )
            .RequirePermission(ProductsPermissions.Create);
}
```

**PUT (update):**

```csharp
public class UpdateEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut(
                "/{id}",
                (ProductId id, UpdateProductRequest request,
                 IProductContracts productContracts) =>
                {
                    var validation = UpdateRequestValidator.Validate(request);
                    if (!validation.IsValid)
                    {
                        throw new ValidationException(validation.Errors);
                    }

                    return CrudEndpoints.Update(
                        () => productContracts.UpdateProductAsync(id, request));
                }
            )
            .RequirePermission(ProductsPermissions.Update);
}
```

**DELETE:**

```csharp
public class DeleteEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(
                "/{id}",
                (ProductId id, IProductContracts productContracts) =>
                    CrudEndpoints.Delete(
                        () => productContracts.DeleteProductAsync(id))
            )
            .RequirePermission(ProductsPermissions.Delete);
}
```

## `IViewEndpoint` -- Inertia View Endpoints

The `IViewEndpoint` interface is identical in shape to `IEndpoint`:

```csharp
public interface IViewEndpoint
{
    void Map(IEndpointRouteBuilder app);
}
```

The difference is semantic. View endpoints use `Inertia.Render()` to return server-side props that hydrate React components. They are grouped under the module's `ViewPrefix` instead of `RoutePrefix`, and they are excluded from API documentation (Swagger/OpenAPI).

### Example: Browse View

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
            )
            .AllowAnonymous();
    }
}
```

The first argument to `Inertia.Render` is the component name (e.g., `"Products/Browse"`). This must match an entry in the module's `Pages/index.ts` registry on the frontend side.

### Example: Create View with Form Handling

View endpoints often handle both the GET (render the form) and POST (process the submission):

```csharp
public class CreateEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/create", () => Inertia.Render("Products/Create"));

        app.MapPost(
                "/",
                async (
                    [FromForm] string name,
                    [FromForm] decimal price,
                    IProductContracts products
                ) =>
                {
                    var request = new CreateProductRequest
                    {
                        Name = name, Price = price
                    };
                    await products.CreateProductAsync(request);
                    return TypedResults.Redirect("/products/manage");
                }
            )
            .DisableAntiforgery();
    }
}
```

### Example: Edit View with GET, POST, and DELETE

```csharp
public class EditEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/{id}/edit",
            async (ProductId id, IProductContracts products) =>
            {
                var product = await products.GetProductByIdAsync(id);
                if (product is null)
                    return TypedResults.NotFound();
                return Inertia.Render("Products/Edit", new { product });
            }
        );

        app.MapPost(
                "/{id}",
                async (
                    ProductId id,
                    [FromForm] string name,
                    [FromForm] decimal price,
                    IProductContracts products
                ) =>
                {
                    var request = new UpdateProductRequest
                    {
                        Name = name, Price = price
                    };
                    await products.UpdateProductAsync(id, request);
                    return TypedResults.Redirect($"/products/{id}/edit");
                }
            )
            .DisableAntiforgery();

        app.MapDelete(
            "/{id}",
            async (ProductId id, IProductContracts products) =>
            {
                await products.DeleteProductAsync(id);
                return TypedResults.Redirect("/products/manage");
            }
        );
    }
}
```

::: warning
When adding a new `IViewEndpoint`, you **must** also register the corresponding component in your module's `Pages/index.ts`. The Inertia component name in `Inertia.Render("Products/Edit", ...)` must have a matching key in the pages record. If you forget, the page will silently 404 on the client side with no error.

Run `npm run validate-pages` to verify all endpoints have matching frontend entries.
:::

## Auto-Discovery

The source generator automatically discovers all classes implementing `IEndpoint` or `IViewEndpoint` in your module's assembly. You do not need to register them anywhere.

The generated code creates route groups with the appropriate prefixes:

- `IEndpoint` classes are grouped under the module's `RoutePrefix` (e.g., `/api/products`) with `RequireAuthorization()` applied by default
- `IViewEndpoint` classes are grouped under the module's `ViewPrefix` (e.g., `/products`) with `RequireAuthorization()` and `ExcludeFromDescription()` applied by default

To allow anonymous access to a specific endpoint, chain `.AllowAnonymous()` after the route definition.

## Parameter Binding

SimpleModule endpoints use ASP.NET Minimal API parameter binding. Understanding the implicit binding rules saves you from writing unnecessary attributes.

### Binding Source Priority

| HTTP Method | Binding order (implicit) |
|-------------|--------------------------|
| GET, HEAD, OPTIONS, DELETE | Route > Query > Header > DI services |
| POST, PUT, PATCH | Route > Query > Header > Body (JSON) > DI services |

### Implicit Binding (No Attribute Needed)

Most parameters bind automatically without any attributes:

```csharp
// Route parameter: int id binds from {id} in the route template
app.MapGet("/{id}", (ProductId id) => ...);

// Query parameter: string? search binds from ?search=...
app.MapGet("/", (string? search, int page = 1) => ...);

// JSON body: complex type binds from request body for POST/PUT
app.MapPost("/", (CreateProductRequest request) => ...);

// DI services: auto-injected when registered in the container
app.MapGet("/", (IProductContracts products) => ...);

// Special types: auto-bound by the framework
app.MapGet("/", (HttpContext context, CancellationToken ct,
                 ClaimsPrincipal user) => ...);
```

::: tip
DI services are auto-injected. You do **not** need `[FromServices]` -- it is noise.
:::

### When Attributes Are Required

**`[FromForm]`** is required for scalar form data. It is never implicit:

```csharp
// CORRECT: scalar form fields require [FromForm]
app.MapPost("/", async (
    [FromForm] string name,
    [FromForm] decimal price,
    IProductContracts products) => ...);
```

**`[FromQuery]`** when a parameter name conflicts with a route parameter, or to rename:

```csharp
app.MapGet("/{id}", (int id, [FromQuery(Name = "v")] int? version) => ...);
```

**`[FromHeader]`** for HTTP headers:

```csharp
app.MapGet("/", ([FromHeader(Name = "X-Tenant-Id")] string tenantId) => ...);
```

### Optional Parameters

Parameters are **required by default**. A missing required parameter returns 400 Bad Request. Make parameters optional with:

```csharp
// Nullable type: null if missing
app.MapGet("/", (int? page) => ...);

// Default value: uses default if missing
app.MapGet("/", (int page = 1, int pageSize = 25) => ...);

// Arrays from query strings: missing key gives empty array
app.MapGet("/tags", (int[] ids) => ...);  // ?ids=1&ids=2
```

### Form Binding Limitations

::: danger
`[FromForm]` does **not** support `List<string>` or `string[]` from repeated form keys in Minimal APIs. Use `ReadFormAsync()` instead:

```csharp
app.MapPost("/", async (HttpContext context) =>
{
    var form = await context.Request.ReadFormAsync();
    var permissions = form["permissions"]
        .Where(p => !string.IsNullOrWhiteSpace(p))
        .Select(p => p!)
        .ToList();
});
```
:::

## Correct Patterns

```csharp
// API: complex type auto-binds from JSON body, service auto-injected
app.MapPost("/", async (CreateProductRequest request,
                        IProductContracts products) => ...);

// API: route param + body + DI
app.MapPut("/{id}", async (int id, UpdateProductRequest request,
                           IProductContracts products) => ...);

// View: scalar form data requires [FromForm]
app.MapPost("/", async ([FromForm] string name,
                        [FromForm] decimal price,
                        IProductContracts products) => ...);

// Query: arrays bind from repeated keys
app.MapGet("/tags", (int[] q) => $"tag1: {q[0]}, tag2: {q[1]}");
```

## Anti-Patterns (Avoid)

```csharp
// BAD: manual form reading for scalar values (use [FromForm] instead)
app.MapPost("/", async (HttpContext context) =>
{
    var form = await context.Request.ReadFormAsync();
    var name = form["name"].ToString(); // Use [FromForm] string name
});

// BAD: manual JSON deserialization (let model binding handle it)
app.MapPost("/", async (HttpContext context) =>
{
    var body = await JsonSerializer.DeserializeAsync<MyType>(
        context.Request.Body);
});

// BAD: [FromServices] is unnecessary noise
app.MapGet("/", ([FromServices] IProductContracts products) => ...);
// GOOD: DI services auto-inject
app.MapGet("/", (IProductContracts products) => ...);
```

## File Organization

By convention, endpoints are organized in the module's directory structure:

```
modules/Products/src/Products/
  Endpoints/
    Products/
      GetAllEndpoint.cs
      GetByIdEndpoint.cs
      CreateEndpoint.cs
      CreateRequestValidator.cs
      UpdateEndpoint.cs
      UpdateRequestValidator.cs
      DeleteEndpoint.cs
  Views/
    BrowseEndpoint.cs
    ManageEndpoint.cs
    CreateEndpoint.cs
    EditEndpoint.cs
```

- `Endpoints/` contains `IEndpoint` classes (API), organized by resource
- `Views/` contains `IViewEndpoint` classes (Inertia pages)
- Validators sit alongside their corresponding endpoint
