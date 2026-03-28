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

## Event Bus

For decoupled cross-module communication without direct dependencies:

### Define Events (in Contracts)

```csharp
public record OrderCreatedEvent(OrderId OrderId, UserId UserId, decimal Total) : IEvent;
```

### Publish Events

```csharp
public class OrderService
{
    private readonly IEventBus _eventBus;

    public async Task CreateOrderAsync(CreateOrderRequest request)
    {
        // ... create order ...

        // Synchronous: waits for all handlers, collects exceptions
        await _eventBus.PublishAsync(new OrderCreatedEvent(order.Id, order.UserId, order.Total));

        // Fire-and-forget: returns immediately
        _eventBus.PublishInBackground(new OrderCreatedEvent(order.Id, order.UserId, order.Total));
    }
}
```

### Handle Events

```csharp
public class OrderCreatedHandler : IEventHandler<OrderCreatedEvent>
{
    public async Task HandleAsync(OrderCreatedEvent @event, CancellationToken cancellationToken)
    {
        // React to the event (e.g., send notification, update stats)
    }
}
```

### Event Semantics
- All handlers execute sequentially in registration order
- Handler failures are isolated — other handlers still run
- After all handlers, collected exceptions throw as `AggregateException`
- Make handlers idempotent when possible
- For long-running work, use background jobs instead

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
