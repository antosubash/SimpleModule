# Cross-Module Communication

## Dependency Rules

```
Implementation → Contracts → Core (one direction only)
```

- **NEVER** depend on another module's implementation assembly
- **ALLOWED** depend on another module's Contracts assembly
- Cross-module calls go through contract interfaces only

## Contracts Interface

Each module exposes a service interface in its Contracts assembly:

```csharp
// In SimpleModule.Products.Contracts
public interface IProductContracts
{
    Task<IEnumerable<Product>> GetAllProductsAsync();
    Task<Product?> GetProductByIdAsync(ProductId id);
    Task<IEnumerable<Product>> GetProductsByIdsAsync(IEnumerable<ProductId> ids);
    Task<Product> CreateProductAsync(CreateProductRequest request);
    Task<Product> UpdateProductAsync(ProductId id, UpdateProductRequest request);
    Task DeleteProductAsync(ProductId id);
}
```

Other modules reference the Contracts project and inject `IProductContracts`:

```xml
<!-- In SimpleModule.Orders.csproj -->
<ProjectReference Include="..\..\..\Products\src\SimpleModule.Products.Contracts\SimpleModule.Products.Contracts.csproj" />
```

## Event Bus (Wolverine)

For decoupled cross-module communication without direct dependencies. The
framework uses [WolverineFx](https://wolverinefx.net/) for in-process
messaging; inject `IMessageBus` from `Wolverine`.

### Define Events (in Contracts)

```csharp
public record OrderCreatedEvent(OrderId OrderId, UserId UserId, decimal Total) : IEvent;
```

`IEvent` is a marker interface in `SimpleModule.Core.Events` — it costs
nothing and makes "this type is a domain event" visible at the type level.

### Publish Events

```csharp
using Wolverine;

public class OrderService(OrdersDbContext db, IMessageBus bus)
{
    public async Task CreateOrderAsync(CreateOrderRequest request)
    {
        // ... create order ...

        // Fire-and-forget: enqueues on the local queue, returns immediately.
        // Handler failures are isolated per handler chain.
        await bus.PublishAsync(new OrderCreatedEvent(order.Id, order.UserId, order.Total));

        // Inline with response: waits for the handler, propagates the first failure.
        await bus.InvokeAsync(new OrderCreatedEvent(order.Id, order.UserId, order.Total));
    }
}
```

### Handle Events

Wolverine auto-discovers handlers by convention: class name ending in
`Handler` or `Consumer`, method named `Handle`/`HandleAsync`/`Consume`/
`ConsumeAsync`, first parameter is the message type, remaining parameters
are resolved from DI.

```csharp
public class OrderCreatedHandler(ILogger<OrderCreatedHandler> logger)
{
    public Task Handle(OrderCreatedEvent evt, CancellationToken ct)
    {
        // React to the event (e.g., send notification, update stats)
        return Task.CompletedTask;
    }
}
```

No DI registration needed — Wolverine scans loaded assemblies at startup.
Non-conventional classes can opt in with `[WolverineHandler]`.

### Event Semantics
- Wolverine routes by runtime type (`message.GetType()`), not the compile-time `T`
- Each handler chain runs in its own scope with its own exception isolation
- For retry/error-queue policies, use `[RetryNow(...)]` or `chain.OnException(...)`
- Make handlers idempotent when possible — messages may be re-run
- For long-running work, prefer `IBackgroundJobs` or Wolverine's scheduled send

## Permissions

### Define Permissions (in Contracts)

```csharp
public sealed class ProductsPermissions : IModulePermissions
{
    public const string View = "Products.View";
    public const string Create = "Products.Create";
    public const string Update = "Products.Update";
    public const string Delete = "Products.Delete";
}
```

### Register in Module

```csharp
public void ConfigurePermissions(PermissionRegistryBuilder builder)
{
    builder.AddPermissions<ProductsPermissions>();
}
```

### Use on Endpoints

```csharp
app.MapGet("/", handler).RequirePermission(ProductsPermissions.View);
```

### Wildcard Matching
- `"Products.*"` matches all Products permissions
- `"*"` matches any single-segment permission
- Users with "Admin" role bypass all permission checks

## Settings

### Define Settings

```csharp
public void ConfigureSettings(ISettingsBuilder settings)
{
    settings.Add(new SettingDefinition
    {
        Key = "products.pageSize",
        DisplayName = "Default Page Size",
        Group = "Products",
        Scope = SettingScope.Application,
        Type = SettingType.Number,
        DefaultValue = "25",
    });
}
```

### Setting Scopes
- **System** — application-wide, typically immutable
- **Application** — tenant or deployment level
- **User** — individual user preferences

## Menu System

```csharp
public void ConfigureMenu(IMenuBuilder menus)
{
    menus.Add(new MenuItem
    {
        Label = "Products",
        Url = "/products",
        Icon = """<svg class="w-4 h-4" ...>...</svg>""",
        Order = 50,
        Section = MenuSection.AppSidebar,
        RequiresAuth = true,
        Group = "Catalog",
    });
}
```

### Menu Sections
- `MenuSection.Navbar` — top navigation bar
- `MenuSection.UserDropdown` — user profile dropdown
- `MenuSection.AdminSidebar` — admin panel sidebar
- `MenuSection.AppSidebar` — main app sidebar

## Result Type

For operations that can fail without exceptions:

```csharp
public async Task<Result<Product>> TryCreateAsync(CreateProductRequest request)
{
    var validation = CreateRequestValidator.Validate(request);
    if (!validation.IsValid)
        return Result<Product>.Fail("Validation failed", validation.Errors);

    var product = await CreateProductAsync(request);
    return Result<Product>.Ok(product);
}

// Consumer
var result = await contracts.TryCreateAsync(request);
if (result.IsFailure)
    return TypedResults.BadRequest(result.Error);
return TypedResults.Ok(result.Value);
```

## What Modules Should NEVER Expose
- Entity classes (use DTOs)
- DbContext
- Internal services
- EF Core configurations
