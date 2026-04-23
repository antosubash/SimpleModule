---
outline: deep
---

# Modules

A **module** is the fundamental building block of a SimpleModule application. Each module is a self-contained feature unit that encapsulates its own services, endpoints, database context, menu items, permissions, and settings. Modules are discovered at compile time by a Roslyn source generator -- there is no reflection at runtime.

## What Is a Module?

A module is a class that implements the `IModule` interface and is decorated with the `[Module]` attribute. It groups related functionality into a single cohesive unit that can be developed, tested, and reasoned about independently.

For example, a Products module owns everything related to products: the database table, the API endpoints, the React views, the menu entries, and the permission definitions. Other modules interact with Products only through its **contracts** interface.

## The `IModule` Interface

The `IModule` interface defines a set of lifecycle hooks, all with default (no-op) implementations. You only override the ones you need:

```csharp
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

| Method | Purpose |
|--------|---------|
| `ConfigureServices` | Register DI services, database contexts, and module-specific configuration |
| `ConfigureEndpoints` | **Escape hatch** for non-standard route registration. Most modules do not need this -- endpoints are auto-discovered |
| `ConfigureMiddleware` | Add module-specific middleware to the ASP.NET pipeline |
| `ConfigureMenu` | Register navigation items in the menu system |
| `ConfigurePermissions` | Define module-specific permissions for authorization |
| `ConfigureSettings` | Register configurable settings for the module |
| `ConfigureFeatureFlags` | Register feature flag definitions |
| `ConfigureAgents` | Register AI agent definitions |
| `ConfigureRateLimits` | Register rate limit policies |
| `ConfigureHost` | Configure host-level integrations after the host is built (e.g., TickerQ, database initialization) |
| `OnStartAsync` | One-time async initialization after all services are registered |
| `OnStopAsync` | Graceful shutdown cleanup |
| `CheckHealthAsync` | Report per-module health status |

::: tip
All methods are `virtual` with default no-op implementations. You only need to override the hooks your module actually uses.
:::

## The `[Module]` Attribute

The `[Module]` attribute provides metadata that the source generator uses to wire up your module:

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ModuleAttribute : Attribute
{
    public string Name { get; }
    public string Version { get; }
    public string RoutePrefix { get; set; } = "";
    public string ViewPrefix { get; set; } = "";

    public ModuleAttribute(string name, string version = "1.0.0")
    {
        Name = name;
        Version = version;
    }
}
```

| Property | Description |
|----------|-------------|
| `Name` | The unique module name (e.g., `"Products"`). Used for database schema isolation and logging. |
| `Version` | Semantic version string. Defaults to `"1.0.0"`. |
| `RoutePrefix` | Base path for API endpoints (e.g., `"/api/products"`). All `IEndpoint` implementations are grouped under this prefix. |
| `ViewPrefix` | Base path for view (Inertia) endpoints (e.g., `"/products"`). All `IViewEndpoint` implementations are grouped under this prefix. |

## Full Example: The Products Module

Here is the Products module from the framework's reference implementation:

```csharp
// ProductsConstants.cs
namespace SimpleModule.Products;

public static class ProductsConstants
{
    public const string ModuleName = "Products";
    public const string RoutePrefix = "/api/products";
}
```

```csharp
// ProductsModule.cs
[Module(
    ProductsConstants.ModuleName,
    RoutePrefix = ProductsConstants.RoutePrefix,
    ViewPrefix = "/products"
)]
public class ProductsModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<ProductsDbContext>(
            configuration, ProductsConstants.ModuleName);
    }

    public void ConfigureMenu(IMenuBuilder menus)
    {
        menus.Add(new MenuItem
        {
            Label = "Products",
            Url = "/products/browse",
            Icon = """<svg class="w-4 h-4" ...>...</svg>""",
            Order = 30,
            Section = MenuSection.Navbar,
            RequiresAuth = false,
        });

        menus.Add(new MenuItem
        {
            Label = "Manage Products",
            Url = "/products/manage",
            Icon = """<svg class="w-4 h-4" ...>...</svg>""",
            Order = 31,
            Section = MenuSection.Navbar,
        });
    }
}
```

This module:
1. Registers its database context in `ConfigureServices`
2. Adds two menu items in `ConfigureMenu`
3. Does **not** override `ConfigureEndpoints` -- its endpoints are auto-discovered

## Compile-Time Discovery

SimpleModule uses a Roslyn incremental source generator (`IIncrementalGenerator` targeting netstandard2.0) to discover modules at compile time. When the host project builds, the generator scans all referenced assemblies for:

- Classes decorated with `[Module]` that implement `IModule`
- Classes implementing `IEndpoint` (API endpoints)
- Classes implementing `IViewEndpoint` (Inertia view endpoints)
- Types marked with `[Dto]` (data transfer objects)
- Contract interfaces following the `I<Name>Contracts` pattern

### Generated Code

The source generator emits several extension methods as plain C# code:

**`AddModules()`** -- Instantiates each discovered module and calls `ConfigureServices` in topologically sorted order (respecting inter-module dependencies). Also auto-registers contract implementations:

```csharp
// Generated: ModuleExtensions.g.cs
public static class ModuleExtensions
{
    internal static readonly ProductsModule _productsModule = new();
    internal static readonly OrdersModule _ordersModule = new();

    public static IServiceCollection AddModules(
        this IServiceCollection services, IConfiguration configuration)
    {
        // Phase 1: No dependencies
        ((IModule)_productsModule).ConfigureServices(services, configuration);

        // Phase 2: Depends on Products
        ((IModule)_ordersModule).ConfigureServices(services, configuration);

        // Auto-discovered contract implementations
        services.AddScoped<IProductContracts, ProductService>();
        services.AddScoped<IOrderContracts, OrderService>();

        return services;
    }
}
```

**`MapModuleEndpoints()`** -- Creates route groups using each module's `RoutePrefix` and `ViewPrefix`, then maps all discovered endpoint classes:

```csharp
// Generated: EndpointExtensions.g.cs
public static WebApplication MapModuleEndpoints(this WebApplication app)
{
    // Auto-registered endpoints for Products
    {
        var group = app.MapGroup("/api/products")
            .WithTags("Products").RequireAuthorization();
        new GetAllEndpoint().Map(group);
        new GetByIdEndpoint().Map(group);
        new CreateEndpoint().Map(group);
        new UpdateEndpoint().Map(group);
        new DeleteEndpoint().Map(group);
    }

    // Auto-registered view endpoints for Products
    {
        var viewGroup = app.MapGroup("/products")
            .WithTags("Products").ExcludeFromDescription()
            .RequireAuthorization();
        new BrowseEndpoint().Map(viewGroup);
        new ManageEndpoint().Map(viewGroup);
        // ...
    }

    return app;
}
```

**`CollectModuleMenuItems()`** -- Calls `ConfigureMenu` on each module to build the menu registry:

```csharp
// Generated: MenuExtensions.g.cs
public static IServiceCollection CollectModuleMenuItems(
    this IServiceCollection services)
{
    var menus = new MenuBuilder();
    ((IModule)ModuleExtensions._productsModule).ConfigureMenu(menus);
    ((IModule)ModuleExtensions._ordersModule).ConfigureMenu(menus);
    services.AddSingleton<IMenuRegistry>(new MenuRegistry(menus.ToList()));
    return services;
}
```

::: tip
You can inspect the generated code in your IDE by navigating to the `SimpleModule.Generator` analyzer output. It is plain C# with no hidden magic.
:::

## Module Lifecycle and Registration Order

The source generator performs a topological sort of modules based on their dependencies (determined by project references between contracts). This ensures that when module B depends on module A's contracts, module A is always registered first.

The lifecycle during application startup:

1. **`AddSimpleModule()`** is called on the `WebApplicationBuilder`
2. Infrastructure services are registered (Wolverine messaging, FusionCache, Inertia, etc.)
3. **`AddModules()`** calls `ConfigureServices` on each module in dependency order
4. Contract implementations are auto-registered as scoped services
5. Permissions are collected from all modules
6. Menu items are collected from all modules
7. **`UseSimpleModule()`** configures the middleware pipeline
8. Module middleware is applied (for modules that override `ConfigureMiddleware`)
9. **`MapModuleEndpoints()`** maps all discovered API and view endpoints

## Creating a New Module

### Using the CLI

The fastest way to create a module:

```bash
sm new module Products
```

This scaffolds the full module structure with contracts, endpoints, tests, and frontend pages.

### Manual Creation

If you prefer to create a module by hand, follow this structure:

```
modules/Products/
  src/
    SimpleModule.Products.Contracts/
      SimpleModule.Products.Contracts.csproj  # References Core only
      IProductContracts.cs                    # Public API interface
      Product.cs                              # Shared DTO types
      ProductId.cs                            # Strongly-typed ID
    SimpleModule.Products/
      SimpleModule.Products.csproj            # References Core + Contracts + Database
      ProductsModule.cs                       # Module class with [Module] attribute
      ProductsConstants.cs                    # Module name and route prefix
      ProductsDbContext.cs                    # EF Core DbContext
      Endpoints/Products/                     # IEndpoint implementations
      Pages/                                  # IViewEndpoint classes next to React .tsx pages
      Pages/index.ts                          # React page registry
      vite.config.ts                          # Vite library mode build
      package.json                            # npm package with peer dependencies
  tests/
    SimpleModule.Products.Tests/              # xUnit test project
```

Key project file requirements:

**Contracts project** (`Microsoft.NET.Sdk`):
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="..\..\..\..\framework\SimpleModule.Core\SimpleModule.Core.csproj" />
  </ItemGroup>
</Project>
```

**Module project** (`Microsoft.NET.Sdk` with an explicit ASP.NET Core framework reference):
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <ProjectReference Include="..\..\..\..\framework\SimpleModule.Core\SimpleModule.Core.csproj" />
    <ProjectReference Include="..\..\..\..\framework\SimpleModule.Database\SimpleModule.Database.csproj" />
    <ProjectReference Include="..\SimpleModule.Products.Contracts\SimpleModule.Products.Contracts.csproj" />
  </ItemGroup>
</Project>
```

::: info
Project and folder names **must** begin with `SimpleModule.` (e.g. `SimpleModule.Products`, `SimpleModule.Products.Contracts`). This convention is enforced by source generator diagnostic **SM0052**. Modules ship static web assets via `Microsoft.NET.Sdk.StaticWebAssets` when they include JS/CSS, but plain `Microsoft.NET.Sdk` works for modules without frontend assets — no Razor SDK is required.
:::

::: warning
Modules **must** include `<FrameworkReference Include="Microsoft.AspNetCore.App" />` in their project file. Without it, ASP.NET types like `IEndpointRouteBuilder` will not be available.
:::

After creating the projects, remember to:

1. Add a `ProjectReference` from the host project (`template/SimpleModule.Host/SimpleModule.Host.csproj`) to your module
2. Add all new projects to the solution file (`SimpleModule.slnx`)

## The ConfigureEndpoints Escape Hatch

Most modules should **not** override `ConfigureEndpoints`. The source generator automatically discovers all `IEndpoint` and `IViewEndpoint` classes and maps them under the module's route prefix.

However, if you need non-standard routing (e.g., routes that don't fit the prefix pattern), you can override `ConfigureEndpoints`:

```csharp
[Module("Webhooks", RoutePrefix = "/api/webhooks")]
public class WebhooksModule : IModule
{
    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        // Custom routes that don't follow the standard prefix
        endpoints.MapPost("/hooks/stripe", StripeWebhookHandler.Handle);
        endpoints.MapPost("/hooks/github", GitHubWebhookHandler.Handle);
    }
}
```

::: danger
When you override `ConfigureEndpoints`, the auto-discovery of `IEndpoint` classes is **skipped** for that module. You are responsible for mapping all endpoints manually. `IViewEndpoint` classes are still auto-discovered regardless.
:::

## Next Steps

- [Endpoints](/guide/endpoints) -- API and view endpoint patterns
- [Contracts & DTOs](/guide/contracts) -- define the public interface between modules
- [Source Generator](/advanced/source-generator) -- how modules are discovered at compile time
