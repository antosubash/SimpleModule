# Minimal API Endpoint Patterns Reference

## Table of Contents
- [Full CRUD Endpoint Set](#full-crud-endpoint-set)
- [View Endpoint with Form Submission](#view-endpoint-with-form-submission)
- [Payload Transformation](#payload-transformation)
- [AsParameters for Query Filtering](#asparameters-for-query-filtering)
- [Route Groups in Single Endpoint](#route-groups-in-single-endpoint)
- [Role-Based View Authorization](#role-based-view-authorization)
- [File Download](#file-download)
- [Custom Action Endpoints](#custom-action-endpoints)
- [HttpContext Access in Views](#httpcontext-access-in-views)
- [Strongly-Typed IDs](#strongly-typed-ids)
- [Permission Constants](#permission-constants)
- [Validation Classes](#validation-classes)

---

## Full CRUD Endpoint Set

Each operation is a separate file in `Endpoints/{Feature}/`.

### GetAllEndpoint.cs
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

### GetByIdEndpoint.cs
```csharp
public class GetByIdEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/{id}",
                (ProductId id, IProductContracts productContracts) =>
                    CrudEndpoints.GetById(() => productContracts.GetProductByIdAsync(id))
            )
            .RequirePermission(ProductsPermissions.View);
}
```

### CreateEndpoint.cs
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
                        throw new ValidationException(validation.Errors);

                    return CrudEndpoints.Create(
                        () => productContracts.CreateProductAsync(request),
                        p => $"{ProductsConstants.RoutePrefix}/{p.Id}"
                    );
                }
            )
            .RequirePermission(ProductsPermissions.Create);
}
```

### UpdateEndpoint.cs
```csharp
public class UpdateEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut(
                "/{id}",
                (ProductId id, UpdateProductRequest request, IProductContracts productContracts) =>
                {
                    var validation = UpdateRequestValidator.Validate(request);
                    if (!validation.IsValid)
                        throw new ValidationException(validation.Errors);

                    return CrudEndpoints.Update(() =>
                        productContracts.UpdateProductAsync(id, request)
                    );
                }
            )
            .RequirePermission(ProductsPermissions.Update);
}
```

### DeleteEndpoint.cs
```csharp
public class DeleteEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(
                "/{id}",
                (ProductId id, IProductContracts productContracts) =>
                    CrudEndpoints.Delete(() => productContracts.DeleteProductAsync(id))
            )
            .RequirePermission(ProductsPermissions.Delete);
}
```

---

## View Endpoint with Form Submission

GET renders the form page, POST handles the form data. Form data ALWAYS needs `[FromForm]`.

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
                    var request = new CreateProductRequest { Name = name, Price = price };
                    await products.CreateProductAsync(request);
                    return Results.Redirect("/products/manage");
                }
            )
            .DisableAntiforgery();
    }
}
```

---

## Payload Transformation

When the frontend sends a different shape than the domain request, use a private payload class. CA1812 is globally suppressed in `.editorconfig`, so no `[SuppressMessage]` is needed.

```csharp
public class CreateEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "/",
            async (CreateOrderPayload body, IOrderContracts orders) =>
            {
                var request = new CreateOrderRequest
                {
                    UserId = UserId.From(body.UserId),
                    Items = body.Items.Select(i => new OrderItem
                    {
                        ProductId = ProductId.From(i.ProductId),
                        Quantity = i.Quantity,
                    }).ToList(),
                };

                await orders.CreateOrderAsync(request);
                return Results.Redirect("/orders");
            }
        );
    }

    internal sealed class CreateOrderPayload
    {
        public string UserId { get; set; } = string.Empty;
        public List<OrderItemPayload> Items { get; set; } = new();
    }

    internal sealed class OrderItemPayload
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
```

---

## AsParameters for Query Filtering

Use `[AsParameters]` when a GET endpoint has multiple query/route parameters bundled in a class.

```csharp
public class GetAllEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/",
                async ([AsParameters] AuditQueryRequest request, IAuditLogContracts auditLogs) =>
                    TypedResults.Ok(await auditLogs.QueryAsync(request))
            )
            .RequirePermission(AuditLogsPermissions.View);
}
```

---

## Route Groups in Single Endpoint

When multiple related routes share config, use `MapGroup` inside `Map()`.

```csharp
public class AccountSecurityEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/Identity/Account/Manage")
            .WithTags(UsersConstants.ModuleName)
            .RequireAuthorization();

        group.MapPost("/TwoFactorAuthentication/forget-browser", async (...) => { });
        group.MapPost("/EnableAuthenticator", async (...) => { });
        group.MapPost("/Disable2fa", async (...) => { });
    }
}
```

---

## Role-Based View Authorization

```csharp
public class EditorEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/admin/pages/new",
                async (ITemplateContracts templates) =>
                    Inertia.Render("PageBuilder/Editor", new
                    {
                        page = (Page?)null,
                        templates = await templates.GetAllTemplatesAsync()
                    })
            )
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

        app.MapGet(
                "/admin/pages/{id}/edit",
                async (PageId id, IPageBuilderContracts pageBuilder) =>
                {
                    var page = await pageBuilder.GetPageByIdAsync(id);
                    if (page is null) return Results.NotFound();
                    return Inertia.Render("PageBuilder/Editor", new { page });
                }
            )
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
```

---

## File Download

```csharp
public class DownloadEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(
                "/download",
                async (ClaimsPrincipal principal, UserManager<ApplicationUser> userManager) =>
                {
                    var user = await userManager.GetUserAsync(principal);
                    if (user is null) return Results.NotFound();

                    var data = new Dictionary<string, string> { ["email"] = user.Email! };
                    return Results.File(
                        JsonSerializer.SerializeToUtf8Bytes(data),
                        "application/json",
                        "data.json"
                    );
                }
            )
            .RequireAuthorization();
    }
}
```

---

## Custom Action Endpoints

For non-CRUD operations (publish, archive, etc.):

```csharp
public class PublishEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                "/{id}/publish",
                async (PageId id, IPageBuilderContracts pageBuilder) =>
                {
                    var page = await pageBuilder.PublishPageAsync(id);
                    return TypedResults.Ok(page);
                }
            )
            .RequirePermission(PageBuilderPermissions.Publish);
}
```

---

## HttpContext Access in Views

```csharp
public class HomeEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/",
                (HttpContext context) =>
                {
                    var isAuthenticated = context.User?.Identity?.IsAuthenticated == true;
                    var displayName = context.User?.Identity?.Name ?? "User";
                    return Inertia.Render("Dashboard/Home", new { isAuthenticated, displayName });
                }
            )
            .AllowAnonymous();
    }
}
```

---

## Strongly-Typed IDs

Strongly-typed IDs (e.g., `ProductId`, `PageId`) work seamlessly as route parameters. The framework handles parsing automatically.

```csharp
app.MapGet("/{id}", (ProductId id, IProductContracts contracts) => ...)
app.MapDelete("/{id}", (PageId id, IPageBuilderContracts contracts) => ...)
```

---

## Permission Constants

Define in the module's constants class:

```csharp
public static class ProductsPermissions
{
    public const string Create = "Products.Create";
    public const string View = "Products.View";
    public const string Update = "Products.Update";
    public const string Delete = "Products.Delete";
}
```

---

## Validation Classes

```csharp
public static class CreateRequestValidator
{
    public static ValidationResult Validate(CreateProductRequest request) =>
        new ValidationBuilder()
            .AddErrorIf(
                string.IsNullOrWhiteSpace(request.Name),
                "Name",
                "Product name is required."
            )
            .AddErrorIf(request.Price <= 0, "Price", "Price must be greater than zero.")
            .Build();
}
```

Usage in endpoint:
```csharp
var validation = CreateRequestValidator.Validate(request);
if (!validation.IsValid)
    throw new ValidationException(validation.Errors);
```
