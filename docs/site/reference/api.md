---
outline: deep
---

# API Reference

Complete reference for SimpleModule's public interfaces, attributes, types, and generated extension methods.

## Interfaces

### IModule

The core module interface. All modules must implement this interface (typically via a class decorated with `[Module]`). All methods have default (empty) implementations -- override only what you need.

```csharp
namespace SimpleModule.Core;

public interface IModule
{
    virtual void ConfigureServices(IServiceCollection services, IConfiguration configuration) { }
    virtual void ConfigureEndpoints(IEndpointRouteBuilder endpoints) { }
    virtual void ConfigureMiddleware(IApplicationBuilder app) { }
    virtual void ConfigureMenu(IMenuBuilder menus) { }
    virtual void ConfigurePermissions(PermissionRegistryBuilder builder) { }
    virtual void ConfigureSettings(ISettingsBuilder settings) { }
    virtual void ConfigureFeatureFlags(IFeatureFlagBuilder builder) { }
    virtual void ConfigureAgents(IAgentBuilder builder) { }
    virtual void ConfigureRateLimits(IRateLimitBuilder builder) { }
    virtual void ConfigureHost(IHost host) { }
    virtual Task OnStartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    virtual Task OnStopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    virtual Task<ModuleHealthStatus> CheckHealthAsync(CancellationToken cancellationToken) =>
        Task.FromResult(ModuleHealthStatus.Healthy);
}
```

| Method | Called When | Purpose |
|--------|-----------|---------|
| `ConfigureServices` | Application startup | Register DI services and configuration |
| `ConfigureEndpoints` | Application startup | Manual endpoint registration (escape hatch) |
| `ConfigureMiddleware` | Application startup | Register ASP.NET middleware |
| `ConfigureMenu` | Application startup | Add navigation menu items |
| `ConfigurePermissions` | Application startup | Register permission definitions |
| `ConfigureSettings` | Application startup | Register settings definitions |
| `ConfigureFeatureFlags` | Application startup | Register feature flag definitions |
| `ConfigureAgents` | Application startup | Register AI agent definitions |
| `ConfigureRateLimits` | Application startup | Register rate limit policies |
| `ConfigureHost` | After host is built | Configure host-level integrations (TickerQ, DB init) |
| `OnStartAsync` | After services registered | One-time async initialization |
| `OnStopAsync` | Graceful shutdown | Cleanup, flush buffers, drain work |
| `CheckHealthAsync` | Health check endpoint | Report module health status |

::: info
`ConfigureEndpoints` is an escape hatch. If your module has `IEndpoint`/`IViewEndpoint` implementations, the source generator registers them automatically. Only override `ConfigureEndpoints` for non-standard routing needs.
:::

---

### IEndpoint

Defines an API endpoint. Implementations are auto-discovered by the source generator and mapped to route groups based on the module's `RoutePrefix`.

```csharp
namespace SimpleModule.Core;

public interface IEndpoint
{
    void Map(IEndpointRouteBuilder app);
}
```

**Usage:**

```csharp
public sealed class GetProducts : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/", async (IProductContracts products) =>
        {
            var result = await products.GetAllAsync();
            return TypedResults.Ok(result);
        });
    }
}
```

---

### IViewEndpoint

Defines a view (page) endpoint that renders an Inertia.js page. Implementations are auto-discovered and mapped to route groups based on the module's `ViewPrefix`.

```csharp
namespace SimpleModule.Core;

public interface IViewEndpoint
{
    void Map(IEndpointRouteBuilder app);
}
```

**Usage:**

```csharp
public sealed class Browse : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/", async (IProductContracts products, HttpContext context) =>
        {
            var result = await products.GetAllAsync();
            return Inertia.Render("Products/Browse", new { products = result });
        });
    }
}
```

::: warning
Every `IViewEndpoint` must have a corresponding entry in the module's `Pages/index.ts`. See the [Pages Registry Pattern](/guide/modules#pages-registry) for details.
:::

---

### IEvent

Marker interface for event types. All events published through `IMessageBus` should implement this interface.

```csharp
namespace SimpleModule.Core.Events;

public interface IEvent;
```

**Usage:**

```csharp
public sealed record OrderCreatedEvent(int OrderId, string CustomerName) : IEvent;
```

---

### IMessageBus (Wolverine)

Publishing is provided by **[Wolverine](https://wolverinefx.net/)**'s `IMessageBus` (namespace `Wolverine`), registered by `AddSimpleModuleInfrastructure()`. Inject it like any other scoped service.

```csharp
using Wolverine;

public sealed class CreateOrder : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/", async (CreateOrderRequest request,
            IOrderContracts orders, IMessageBus bus) =>
        {
            var order = await orders.CreateAsync(request);
            await bus.PublishAsync(new OrderCreatedEvent(order.Id, request.CustomerName));
            return TypedResults.Created($"/{order.Id}", order);
        });
    }
}
```

Handlers are discovered by Wolverine's naming convention — a class with a `Handle` / `Consume` / `HandleAsync` method taking the event as its first parameter. See [Events](/guide/events) for the full guide.

```csharp
public sealed class OrderCreatedAuditHandler(IAuditContext audit)
{
    public Task Handle(OrderCreatedEvent evt, CancellationToken ct) =>
        audit.LogAsync("Order created", evt.OrderId.ToString(), ct);
}
```

`Lazy<IMessageBus>` is also registered to let services break factory-lambda cycles.

---

### IMenuBuilder

Fluent interface for adding navigation menu items during module initialization.

```csharp
namespace SimpleModule.Core.Menu;

public interface IMenuBuilder
{
    IMenuBuilder Add(MenuItem item);
}
```

---

### IMenuRegistry

Read-only access to registered menu items at runtime, grouped by section.

```csharp
namespace SimpleModule.Core.Menu;

public interface IMenuRegistry
{
    IReadOnlyList<MenuItem> GetItems(MenuSection section);
}
```

---

### ISettingsBuilder

Interface for registering module settings definitions during module initialization.

```csharp
namespace SimpleModule.Core.Settings;

public interface ISettingsBuilder
{
    ISettingsBuilder Add(SettingDefinition definition);
}
```

---

### IModulePermissions

Marker interface for permission classes. Implementations are auto-discovered by the source generator. Permission classes must be `sealed` and contain only `public const string` fields.

```csharp
namespace SimpleModule.Core.Authorization;

public interface IModulePermissions;
```

**Usage:**

```csharp
public sealed class ProductPermissions : IModulePermissions
{
    public const string View = "Products.View";
    public const string Create = "Products.Create";
    public const string Edit = "Products.Edit";
    public const string Delete = "Products.Delete";
}
```

## Attributes

### [Module]

Marks a class as a module. Required for source generator discovery.

```csharp
namespace SimpleModule.Core;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ModuleAttribute : Attribute
{
    public string Name { get; }
    public string Version { get; }
    public string RoutePrefix { get; set; } = "";
    public string ViewPrefix { get; set; } = "";

    public ModuleAttribute(string name, string version = "1.0.0") { ... }
}
```

| Property | Description | Default |
|----------|-------------|---------|
| `Name` | Module display name (required) | -- |
| `Version` | Module version | `"1.0.0"` |
| `RoutePrefix` | URL prefix for API endpoints | `""` |
| `ViewPrefix` | URL prefix for view endpoints | `""` |

**Usage:**

```csharp
[Module("Products", RoutePrefix = "/api/products", ViewPrefix = "/products")]
public sealed class ProductsModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register module services
    }
}
```

---

### [Dto]

Marks a type for TypeScript generation. Not required for types in `*.Contracts` assemblies (those are included by convention).

```csharp
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Struct,
    AllowMultiple = false, Inherited = false
)]
public sealed class DtoAttribute : Attribute { }
```

---

### [NoDtoGeneration]

Excludes a public type in a Contracts assembly from automatic DTO/TypeScript generation.

```csharp
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface,
    AllowMultiple = false, Inherited = false
)]
public sealed class NoDtoGenerationAttribute : Attribute { }
```

---

### [RequirePermission]

Declares that an endpoint requires specific permissions. Used by the source generator to apply authorization metadata.

```csharp
namespace SimpleModule.Core.Authorization;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class RequirePermissionAttribute : Attribute
{
    public string[] Permissions { get; }

    public RequirePermissionAttribute(params string[] permissions) { ... }
}
```

**Usage:**

```csharp
[RequirePermission(ProductPermissions.Create)]
public sealed class CreateProduct : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/", async (CreateProductRequest request, IProductContracts products) =>
        {
            var product = await products.CreateAsync(request);
            return TypedResults.Created($"/{product.Id}", product);
        });
    }
}
```

## Types

### PagedResult\<T\>

Standard wrapper for paginated query results.

```csharp
namespace SimpleModule.Core;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
```

---

### MenuItem

Represents a navigation menu entry.

```csharp
namespace SimpleModule.Core.Menu;

public sealed class MenuItem
{
    public required string Label { get; init; }
    public required string Url { get; init; }
    public string Icon { get; init; } = "";
    public int Order { get; init; }
    public MenuSection Section { get; init; } = MenuSection.Navbar;
    public bool RequiresAuth { get; init; } = true;
    public string? Group { get; init; }

    // When set, only users with at least one of these roles see the item.
    // Empty list means visible to all authenticated users.
    public IReadOnlyList<string> Roles { get; init; } = [];

    // When set, only users whose permission claims satisfy the requirement
    // see the item (supports wildcards via PermissionMatcher). Admin role
    // bypasses this check. Null means no permission gating.
    public string? RequiredPermission { get; init; }
}
```

---

### MenuSection

Enum defining where a menu item appears in the UI.

```csharp
namespace SimpleModule.Core.Menu;

public enum MenuSection
{
    Navbar,
    UserDropdown,
    AdminSidebar,
    AppSidebar,
}
```

---

### ModuleDbContextInfo

Associates a module name with its DbContext type for schema isolation.

```csharp
namespace SimpleModule.Database;

public sealed record ModuleDbContextInfo(string ModuleName, Type DbContextType);
```

---

### PermissionRegistry

Read-only registry of all permissions in the application. Registered as a singleton.

```csharp
namespace SimpleModule.Core.Authorization;

public sealed class PermissionRegistry
{
    public IReadOnlySet<string> AllPermissions { get; }
    public IReadOnlyDictionary<string, IReadOnlyList<string>> ByModule { get; }
}
```

---

### PermissionRegistryBuilder

Builder for constructing the `PermissionRegistry` during startup. Passed to `IModule.ConfigurePermissions`.

```csharp
namespace SimpleModule.Core.Authorization;

public sealed class PermissionRegistryBuilder
{
    public void AddPermissions<T>() where T : class { ... }
    public void AddPermission(string permission) { ... }
    public PermissionRegistry Build() { ... }
}
```

Permissions follow the convention `ModuleName.Action` (e.g., `Products.Create`). The module prefix is extracted from the first segment before the `.`.

---

### SettingDefinition

Defines a configurable setting for a module.

```csharp
namespace SimpleModule.Core.Settings;

public class SettingDefinition
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Group { get; set; }
    public SettingScope Scope { get; set; }
    public string? DefaultValue { get; set; }
    public SettingType Type { get; set; }
}
```

## Generated Extension Methods

These extension methods are generated by the source generator and called in `Program.cs`:

### AddModules

```csharp
public static IServiceCollection AddModules(
    this IServiceCollection services,
    IConfiguration configuration)
```

Registers all discovered modules' services in dependency order. This includes:
- Calling `ConfigureServices` on each module (topologically sorted)
- Registering auto-discovered contract implementations as scoped services
- Building and registering the `PermissionRegistry`
- Configuring JSON serializer options with the generated type info resolver

### MapModuleEndpoints

```csharp
public static WebApplication MapModuleEndpoints(this WebApplication app)
```

Maps all discovered `IEndpoint` and `IViewEndpoint` implementations. API endpoints are grouped by `RoutePrefix` with authorization required. View endpoints are grouped by `ViewPrefix` and excluded from API descriptions.

Modules that override `ConfigureEndpoints` are called separately (the escape hatch).

### CollectModuleMenuItems

```csharp
public static IServiceCollection CollectModuleMenuItems(
    this IServiceCollection services)
```

Invokes `ConfigureMenu` on all modules that implement it and registers the resulting `IMenuRegistry` as a singleton.

## Next Steps

- [Configuration Reference](/reference/configuration) -- all framework configuration options
- [Source Generator](/advanced/source-generator) -- how these extension methods are generated
- [Modules](/guide/modules) -- practical guide to using these interfaces
