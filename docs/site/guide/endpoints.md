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

Here is a complete set of API endpoints for a Customers module. The module's `RoutePrefix` is `"/api/customers"`, so `"/"` maps to `/api/customers` and `"/{id}"` maps to `/api/customers/{id}`.

**GET all customers:**

```csharp
public class GetAllEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/",
                (ICustomerContracts customerContracts) =>
                    CrudEndpoints.GetAll(customerContracts.GetAllCustomersAsync)
            )
            .RequirePermission(CustomersPermissions.View);
}
```

**GET by ID:**

```csharp
public class GetByIdEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/{id}",
                (CustomerId id, ICustomerContracts customerContracts) =>
                    CrudEndpoints.GetById(
                        () => customerContracts.GetCustomerByIdAsync(id))
            )
            .RequirePermission(CustomersPermissions.View);
}
```

**POST (create):**

```csharp
public class CreateEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                "/",
                async (
                    CreateCustomerRequest request,
                    IValidator<CreateCustomerRequest> validator,
                    ICustomerContracts customerContracts
                ) =>
                {
                    var validation = await validator.ValidateAsync(request);
                    if (!validation.IsValid)
                    {
                        throw new ValidationException(validation.ToValidationErrors());
                    }

                    return await CrudEndpoints.Create(
                        () => customerContracts.CreateCustomerAsync(request),
                        p => $"{CustomersConstants.RoutePrefix}/{p.Id}"
                    );
                }
            )
            .RequirePermission(CustomersPermissions.Create);
}
```

**PUT (update):**

```csharp
public class UpdateEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut(
                "/{id}",
                async (
                    CustomerId id,
                    UpdateCustomerRequest request,
                    IValidator<UpdateCustomerRequest> validator,
                    ICustomerContracts customerContracts
                ) =>
                {
                    var validation = await validator.ValidateAsync(request);
                    if (!validation.IsValid)
                    {
                        throw new ValidationException(validation.ToValidationErrors());
                    }

                    return await CrudEndpoints.Update(
                        () => customerContracts.UpdateCustomerAsync(id, request));
                }
            )
            .RequirePermission(CustomersPermissions.Update);
}
```

**DELETE:**

```csharp
public class DeleteEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(
                "/{id}",
                (CustomerId id, ICustomerContracts customerContracts) =>
                    CrudEndpoints.Delete(
                        () => customerContracts.DeleteCustomerAsync(id))
            )
            .RequirePermission(CustomersPermissions.Delete);
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
                async (ICustomerContracts customers) =>
                    Inertia.Render(
                        "Customers/Browse",
                        new { customers = await customers.GetAllCustomersAsync() }
                    )
            )
            .AllowAnonymous();
    }
}
```

The first argument to `Inertia.Render` is the component name (e.g., `"Customers/Browse"`). This must match an entry in the module's `Pages/index.ts` registry on the frontend side.

### Example: Create View with Form Handling

View endpoints often handle both the GET (render the form) and POST (process the submission):

```csharp
public class CreateEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/create", () => Inertia.Render("Customers/Create"));

        app.MapPost(
                "/",
                async (
                    [FromForm] string name,
                    [FromForm] string email,
                    ICustomerContracts customers
                ) =>
                {
                    var request = new CreateCustomerRequest
                    {
                        Name = name, Email = email
                    };
                    await customers.CreateCustomerAsync(request);
                    return TypedResults.Redirect("/customers/manage");
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
            async (CustomerId id, ICustomerContracts customers) =>
            {
                var customer = await customers.GetCustomerByIdAsync(id);
                if (customer is null)
                    return TypedResults.NotFound();
                return Inertia.Render("Customers/Edit", new { customer });
            }
        );

        app.MapPost(
                "/{id}",
                async (
                    CustomerId id,
                    [FromForm] string name,
                    [FromForm] string email,
                    ICustomerContracts customers
                ) =>
                {
                    var request = new UpdateCustomerRequest
                    {
                        Name = name, Email = email
                    };
                    await customers.UpdateCustomerAsync(id, request);
                    return TypedResults.Redirect($"/customers/{id}/edit");
                }
            )
            .DisableAntiforgery();

        app.MapDelete(
            "/{id}",
            async (CustomerId id, ICustomerContracts customers) =>
            {
                await customers.DeleteCustomerAsync(id);
                return TypedResults.Redirect("/customers/manage");
            }
        );
    }
}
```

::: warning
When adding a new `IViewEndpoint`, you **must** also register the corresponding component in your module's `Pages/index.ts`. The Inertia component name in `Inertia.Render("Customers/Edit", ...)` must have a matching key in the pages record. If you forget, the page will silently 404 on the client side with no error.

Run `npm run validate-pages` to verify all endpoints have matching frontend entries.
:::

## Auto-Discovery

The source generator automatically discovers all classes implementing `IEndpoint` or `IViewEndpoint` in your module's assembly. You do not need to register them anywhere.

The generated code creates route groups with the appropriate prefixes:

- `IEndpoint` classes are grouped under the module's `RoutePrefix` (e.g., `/api/customers`) with `RequireAuthorization()` applied by default
- `IViewEndpoint` classes are grouped under the module's `ViewPrefix` (e.g., `/customers`) with `RequireAuthorization()` and `ExcludeFromDescription()` applied by default

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
app.MapGet("/{id}", (CustomerId id) => ...);

// Query parameter: string? search binds from ?search=...
app.MapGet("/", (string? search, int page = 1) => ...);

// JSON body: complex type binds from request body for POST/PUT
app.MapPost("/", (CreateCustomerRequest request) => ...);

// DI services: auto-injected when registered in the container
app.MapGet("/", (ICustomerContracts customers) => ...);

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
    [FromForm] string email,
    ICustomerContracts customers) => ...);
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
app.MapPost("/", async (CreateCustomerRequest request,
                        ICustomerContracts customers) => ...);

// API: route param + body + DI
app.MapPut("/{id}", async (int id, UpdateCustomerRequest request,
                           ICustomerContracts customers) => ...);

// View: scalar form data requires [FromForm]
app.MapPost("/", async ([FromForm] string name,
                        [FromForm] string email,
                        ICustomerContracts customers) => ...);

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
app.MapGet("/", ([FromServices] ICustomerContracts customers) => ...);
// GOOD: DI services auto-inject
app.MapGet("/", (ICustomerContracts customers) => ...);
```

## Validation

Request validation uses **[FluentValidation](https://docs.fluentvalidation.net/)**. Write an `AbstractValidator<T>` for every request DTO that needs validation.

```csharp
using FluentValidation;

public sealed class CreateRequestValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Customer name is required.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("A valid email is required.");
    }
}
```

Register validators once in your module's `ConfigureServices`:

```csharp
services.AddValidatorsFromAssemblyContaining<CustomersModule>();
```

Endpoints inject `IValidator<T>`, call `ValidateAsync`, and convert failures to the framework's `ValidationException`:

```csharp
var validation = await validator.ValidateAsync(request);
if (!validation.IsValid)
{
    throw new ValidationException(validation.ToValidationErrors());
}
```

`ToValidationErrors()` (in `SimpleModule.Core.Validation`) converts FluentValidation failures into the shape `GlobalExceptionHandler` expects. See [Error Pages](/guide/error-pages) for the response format.

## File Organization

By convention, endpoints are organized in the module's directory structure:

```
modules/Customers/src/SimpleModule.Customers/
  Endpoints/
    Customers/
      GetAllEndpoint.cs
      GetByIdEndpoint.cs
      CreateEndpoint.cs
      CreateRequestValidator.cs    # AbstractValidator<CreateCustomerRequest>
      UpdateEndpoint.cs
      UpdateRequestValidator.cs    # AbstractValidator<UpdateCustomerRequest>
      DeleteEndpoint.cs
  Pages/
    BrowseEndpoint.cs              # IViewEndpoint — sits next to Browse.tsx
    Browse.tsx
    ManageEndpoint.cs
    Manage.tsx
    CreateEndpoint.cs
    Create.tsx
    EditEndpoint.cs
    Edit.tsx
    index.ts                       # Pages registry
```

- `Endpoints/` contains `IEndpoint` classes (API), organized by resource
- `Pages/` contains `IViewEndpoint` classes (Inertia pages) alongside their React `.tsx` components — there is no separate `Views/` directory
- Validators sit alongside their corresponding endpoint

## Next Steps

- [Contracts & DTOs](/guide/contracts) -- define shared types and cross-module interfaces
- [Inertia.js Integration](/guide/inertia) -- how view endpoints render React pages
- [Permissions](/guide/permissions) -- protect endpoints with claims-based authorization
