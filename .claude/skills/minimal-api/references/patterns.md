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

Each operation is a separate file in `Endpoints/{Feature}/` (one endpoint per file, **SM0049**). Every endpoint declares a `public const string Route` and passes it to its `MapXxx` call (**SM0054**); route literals live in a `Routes` nested class on the module constants.

### GetAllEndpoint.cs
```csharp
public class GetAllEndpoint : IEndpoint
{
    public const string Route = ProductsConstants.Routes.GetAll;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
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
    public const string Route = ProductsConstants.Routes.GetById;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
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
    public const string Route = ProductsConstants.Routes.Create;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                Route,
                async (
                    CreateProductRequest request,
                    IValidator<CreateProductRequest> validator,
                    IProductContracts productContracts
                ) =>
                {
                    var validation = await validator.ValidateAsync(request);
                    if (!validation.IsValid)
                        throw new Core.Exceptions.ValidationException(validation.ToValidationErrors());

                    return await CrudEndpoints.Create(
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
    public const string Route = ProductsConstants.Routes.Update;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut(
                Route,
                async (
                    ProductId id,
                    UpdateProductRequest request,
                    IValidator<UpdateProductRequest> validator,
                    IProductContracts productContracts
                ) =>
                {
                    var validation = await validator.ValidateAsync(request);
                    if (!validation.IsValid)
                        throw new Core.Exceptions.ValidationException(validation.ToValidationErrors());

                    return await CrudEndpoints.Update(() =>
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
    public const string Route = ProductsConstants.Routes.Delete;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(
                Route,
                (ProductId id, IProductContracts productContracts) =>
                    CrudEndpoints.Delete(() => productContracts.DeleteProductAsync(id))
            )
            .RequirePermission(ProductsPermissions.Delete);
}
```

For soft-delete-aware modules, `CrudEndpoints` also exposes `Restore(() => contracts.RestoreAsync(id))` and `ForceDelete(() => contracts.ForceDeleteAsync(id))`.

---

## View Endpoint with Form Submission

GET renders the form page; POST binds and validates the submission. The preferred binding is a `[FormRequest]` class — `sealed partial`, extending `FormRequest<TSelf>` (**SM0056** / **SM0057**), in the module's `FormRequests/` folder. It binds **directly** as a handler parameter: the framework hydrates it from the form body, runs `Prepare()`, then validates via `ConfigureRules` before the handler runs (no manual `IValidator`, no `.DisableAntiforgery()`).

```csharp
// FormRequests/CreateProductFormRequest.cs
using FluentValidation;
using SimpleModule.Core.FormRequests;

[FormRequest]
public sealed partial class CreateProductFormRequest : FormRequest<CreateProductFormRequest>
{
    public string Name { get; set; } = "";
    public decimal Price { get; set; }

    public override void Prepare() => Name = Name.Trim();

    protected override void ConfigureRules(RuleConfigurator<CreateProductFormRequest> rules)
    {
        rules.RuleFor(x => x.Name).NotEmpty().WithMessage("Product name is required.");
        rules.RuleFor(x => x.Price).GreaterThan(0).WithMessage("Price must be greater than zero.");
    }
}

// Pages/CreateEndpoint.cs
public class CreateEndpoint : IViewEndpoint
{
    public const string Route = ProductsConstants.Routes.CreatePage;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(Route, () => Inertia.Render("Products/Create"));

        app.MapPost(
                "/products",
                async (CreateProductFormRequest form, IProductContracts products) =>
                {
                    await products.CreateProductAsync(
                        new CreateProductRequest { Name = form.Name, Price = form.Price });
                    return Results.Redirect("/products/manage");
                }
            );
    }
}
```

### Alternative: inline `[FromForm]` record

For one-off forms, bind a private `sealed record` of `[FromForm]` fields with `[AsParameters]` and validate manually with `IValidator<T>` (as the Email module's `EditTemplateEndpoint` does):

```csharp
app.MapPost(
        "/templates/{id}",
        async (int id, [AsParameters] UpdateTemplateForm form,
               IValidator<UpdateEmailTemplateRequest> validator, IEmailContracts email) =>
        {
            var request = new UpdateEmailTemplateRequest { Name = form.Name, Subject = form.Subject };
            var validation = await validator.ValidateAsync(request);
            if (!validation.IsValid)
                throw new Core.Exceptions.ValidationException(validation.ToValidationErrors());
            await email.UpdateTemplateAsync(EmailTemplateId.From(id), request);
            return Results.Redirect("/email/templates");
        });

// private sealed record UpdateTemplateForm([FromForm] string Name, [FromForm] string Subject);
```

---

## Payload Transformation

When the frontend sends a different shape than the domain request, use a private payload class. CA1812 ("internal class never instantiated") is suppressed in `.editorconfig` for `Endpoints/` and `Pages/*Endpoint.cs` files, so a payload class defined inside an endpoint file needs no `[SuppressMessage]`.

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

Define as a `sealed` class implementing `IModulePermissions` in the Contracts assembly (the generator auto-discovers it; SM0032 fails the build if it isn't sealed):

```csharp
public sealed class ProductsPermissions : IModulePermissions
{
    public const string Create = "Products.Create";
    public const string View = "Products.View";
    public const string Update = "Products.Update";
    public const string Delete = "Products.Delete";
}
```

---

## Validation Classes

Use FluentValidation `AbstractValidator<T>`. The framework provides a `ToValidationErrors()` extension that converts FluentValidation's `ValidationResult` to the `Dictionary<string, string[]>` shape consumed by `ValidationException` and the RFC 7807 response writer.

```csharp
public sealed class CreateRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Product name is required.");
        RuleFor(x => x.Price).GreaterThan(0).WithMessage("Price must be greater than zero.");
    }
}
```

Register once per module in `ConfigureServices`:

```csharp
services.AddValidatorsFromAssemblyContaining<ThisModule>();
```

Usage in endpoint (async lambda, inject `IValidator<TRequest>`):

```csharp
async (
    CreateProductRequest request,
    IValidator<CreateProductRequest> validator,
    IProductContracts contracts
) =>
{
    var validation = await validator.ValidateAsync(request);
    if (!validation.IsValid)
        throw new Core.Exceptions.ValidationException(validation.ToValidationErrors());
    // ...
}
```
