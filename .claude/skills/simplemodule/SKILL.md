---
name: simplemodule
description: >
  Comprehensive guide for working with the SimpleModule modular monolith framework.
  Use when creating modules, adding features, writing endpoints, configuring services,
  working with the event bus, permissions, menus, settings, database contexts, or
  understanding the project architecture. Triggers on: "add module", "new module",
  "add feature", "create endpoint", "module architecture", "how does SimpleModule work",
  "event bus", "permissions", "menu", "settings", "database context", "contracts",
  "IModule", "IEndpoint", "IViewEndpoint", "Inertia", "CrudEndpoints",
  "debug module", "review module", "module status", "SM00", "source generator diagnostic",
  "module not found", "page 404", "event bus", "settings", "IEventBus", "SettingDefinition".
---

# SimpleModule Framework Guide

## Architecture Overview

SimpleModule is a **modular monolith** for .NET 10 with compile-time module discovery via Roslyn source generators. Frontend uses React 19 + Inertia.js served via Blazor SSR.

**Key principle:** Single deployment, shared database, compile-time safety over runtime discipline, convention over configuration.

## Project Layout

```
modules/{Name}/
  src/SimpleModule.{Name}.Contracts/    # Public API (interfaces, DTOs, events, constants)
  src/SimpleModule.{Name}/              # Implementation (module class, services, endpoints, views)
  tests/SimpleModule.{Name}.Tests/      # Tests (unit + integration)
framework/                              # Core, Database, Generator, Hosting, Blazor, Storage
template/SimpleModule.Host/             # Host app calling generated extension methods
```

## Module Anatomy

Every module has two assemblies plus optional tests:

### 1. Contracts Assembly (`SimpleModule.{Name}.Contracts`)

**Purpose:** Public API that other modules can depend on. Never expose internals.

**Contains:**
- `{Name}Constants.cs` — module name and route prefix
- `I{Name}Contracts.cs` — service interface for cross-module use
- DTOs marked with `[Dto]` — auto-generates TypeScript interfaces
- Value objects using Vogen (`[ValueObject<int>]`) for type-safe IDs
- Domain events implementing `IEvent`
- Permission constants implementing `IModulePermissions`

**Project file pattern:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Library</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\..\..\framework\SimpleModule.Core\SimpleModule.Core.csproj" />
  </ItemGroup>
</Project>
```

### 2. Implementation Assembly (`SimpleModule.{Name}`)

**Contains:**
- `{Name}Module.cs` — implements `IModule`, decorated with `[Module]`
- `{Name}ContractsService.cs` — implements `I{Name}Contracts`
- `{Name}DbContext.cs` — EF Core context (if module has data)
- `Endpoints/{Name}/` — API endpoint classes implementing `IEndpoint`
- `Views/` — view endpoint classes implementing `IViewEndpoint` + React `.tsx` components
- `Pages/index.ts` — page registry mapping route names to React components
- `EntityConfigurations/` — EF Core entity configurations
- `vite.config.ts` and `package.json` — frontend build config

**Project file pattern:**
```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <ProjectReference Include="..\..\..\..\framework\SimpleModule.Core\SimpleModule.Core.csproj" />
    <ProjectReference Include="..\..\..\..\framework\SimpleModule.Database\SimpleModule.Database.csproj" />
    <ProjectReference Include="..\SimpleModule.{Name}.Contracts\SimpleModule.{Name}.Contracts.csproj" />
  </ItemGroup>
</Project>
```

## Module Class Pattern

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;
using SimpleModule.Core.Menu;
using SimpleModule.{Name}.Contracts;

namespace SimpleModule.{Name};

[Module({Name}Constants.ModuleName, RoutePrefix = {Name}Constants.RoutePrefix, ViewPrefix = "/{name}")]
public class {Name}Module : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<I{Name}Contracts, {Name}ContractsService>();
        services.AddModuleDbContext<{Name}DbContext>(configuration, {Name}Constants.ModuleName);
    }

    public void ConfigureMenu(IMenuBuilder menus)
    {
        menus.Add(new MenuItem
        {
            Label = "{Name}",
            Url = "/{name}",
            Icon = """<svg class="w-4 h-4" ...>...</svg>""",
            Order = 50,
            Section = MenuSection.AppSidebar,
        });
    }

    public void ConfigurePermissions(PermissionRegistryBuilder builder)
    {
        builder.AddPermissions<{Name}Permissions>();
    }
}
```

### IModule Lifecycle Hooks (all optional)

| Method | Purpose |
|--------|---------|
| `ConfigureServices(IServiceCollection, IConfiguration)` | DI registration |
| `ConfigureEndpoints(IEndpointRouteBuilder)` | Escape hatch for non-standard routes |
| `ConfigureMiddleware(IApplicationBuilder)` | Module-specific middleware |
| `ConfigureMenu(IMenuBuilder)` | Navigation items |
| `ConfigurePermissions(PermissionRegistryBuilder)` | Permission definitions |
| `ConfigureSettings(ISettingsBuilder)` | Application settings |
| `OnStartAsync(CancellationToken)` | Startup initialization |
| `OnStopAsync(CancellationToken)` | Graceful shutdown cleanup |
| `CheckHealthAsync(CancellationToken)` | Health status reporting |

## Endpoint Patterns

See [references/endpoints.md](references/endpoints.md) for complete endpoint patterns.

## Database & Data Access

See [references/database.md](references/database.md) for database patterns.

## Frontend Integration

See [references/frontend.md](references/frontend.md) for React/Inertia patterns.

## Testing

See [references/testing.md](references/testing.md) for test patterns.

## Cross-Module Communication

See [references/cross-module.md](references/cross-module.md) for events, contracts, and permissions.

## Integration Checklist

When adding a new module, ensure:

1. Contracts project created with constants, interface, and DTOs
2. Implementation project uses `Microsoft.NET.Sdk.Razor` with `<FrameworkReference Include="Microsoft.AspNetCore.App" />`
3. Module class decorated with `[Module]` and implements `IModule`
4. `ProjectReference` added to `template/SimpleModule.Host/SimpleModule.Host.csproj`
5. Both projects added to `SimpleModule.slnx` under `/modules/{Name}/` folder
6. Every `IViewEndpoint` with `Inertia.Render()` has a matching entry in `Pages/index.ts`
7. Run `dotnet build` to verify source generator picks up the module
8. Run `npm run validate-pages` to verify page registry matches endpoints

## Common Pitfalls

| Symptom | Cause | Fix |
|---------|-------|-----|
| Module not discovered at build | Missing `[Module]` attribute or wrong project not referenced in Host | Add `[Module]` to module class; add `<ProjectReference>` to Host `.csproj` |
| Page navigates but shows blank | Missing `Pages/index.ts` entry | Add `'Module/Page': () => import('../Views/Page')` to Pages/index.ts |
| SM0011 diagnostic | Direct impl→impl project reference | Reference only the `.Contracts` project; inject `I{Name}Contracts` interface |
| SM0014 diagnostic | No public interfaces in Contracts assembly | Add `I{Name}Contracts` to the Contracts project |
| SM0025 diagnostic | No implementation for contract interface | Create `{Name}ContractsService` implementing `I{Name}Contracts` and register in `ConfigureServices` |
| SM0041/SM0042 diagnostic | View endpoint misconfigured | Ensure `Inertia.Render` name is prefixed with module name; add `ViewPrefix` to `[Module]` attribute |
| `TreatWarningsAsErrors` build failure | Nullable, unused variable, or analyzer warning | Fix the warning; suppress in `.editorconfig` only if genuinely intentional |
| Event handler never called | Handler not registered in DI | Add `services.AddScoped<IEventHandler<MyEvent>, MyHandler>()` in `ConfigureServices` |
| Cross-module data wrong | Injecting impl class directly | Always inject `I{Name}Contracts` interface, never the concrete service class |

## Events Pattern

Define events in the **Contracts** assembly as `record` types:

```csharp
// In SimpleModule.{Name}.Contracts
public record OrderPlaced(OrderId OrderId, decimal Total) : IEvent;
```

Publish from a service:

```csharp
// Awaits all handlers before returning
await _eventBus.PublishAsync(new OrderPlaced(order.Id, order.Total), ct);

// Fire-and-forget (background task, not awaited)
_eventBus.PublishInBackground(new OrderPlaced(order.Id, order.Total));
```

Handle in any module:

```csharp
public sealed class SendConfirmationEmailHandler : IEventHandler<OrderPlaced>
{
    public async Task HandleAsync(OrderPlaced evt, CancellationToken ct)
    {
        // use evt.OrderId, evt.Total
    }
}
```

Register in `ConfigureServices`:

```csharp
services.AddScoped<IEventHandler<OrderPlaced>, SendConfirmationEmailHandler>();
```

**Semantics:** Handlers run sequentially. A failing handler throws `AggregateException`. Write handlers to be idempotent.

## Settings Pattern

Register setting definitions in `ConfigureSettings` on the module class:

```csharp
public void ConfigureSettings(ISettingsBuilder builder)
{
    builder.AddDefinition(new SettingDefinition
    {
        Key = "Orders.MaxItemsPerOrder",
        DisplayName = "Max Items Per Order",
        Group = "Orders",
        Scope = SettingScope.Application,   // System | Application | User
        Type = SettingType.Number,          // Text | Number | Bool | Json
        DefaultValue = "50",
    });
}
```

Read a setting in a service:

```csharp
var max = await _settings.GetSettingAsync<int>("Orders.MaxItemsPerOrder", SettingScope.Application);
```

**Scopes:** `System` = application-wide (e.g., feature flags), `Application` = per tenant/deployment, `User` = per authenticated user.

## Constitution Diagnostics (SM00xx)

The source generator enforces these rules at build time. Run `/debug-module {Name}` to check all at once.

| Code | Rule | Common cause |
|------|------|-------------|
| SM0001 | No duplicate DbSet property names across modules | Two modules declare a DbSet with the same property name |
| SM0007 | No duplicate entity configurations | Registered the same `IEntityTypeConfiguration<T>` twice |
| SM0010 | No circular module dependencies | Contract projects reference each other in a cycle |
| SM0011 | No direct module-to-module implementation references | Took a shortcut referencing another module's impl project |
| SM0014 | Contracts assembly has no public interfaces | Forgot to add `I{Name}Contracts` to the Contracts project |
| SM0015 | No duplicate view page names | Two `IViewEndpoint`s return the same Inertia component name |
| SM0025 | No implementation found for contract interface | Forgot to create the `{Name}ContractsService` class |
| SM0032 | Permission class must be `sealed` | Permissions class is not sealed |
| SM0040 | No duplicate module names | Two modules share the same `[Module]` name string |
| SM0041 | View page name must be prefixed with module name | Inertia component name doesn't start with the module name |
| SM0042 | `ViewPrefix` required when module has view endpoints | Module has `IViewEndpoint`s but no `ViewPrefix` on `[Module]` |
| SM0043 | Module must override at least one `IModule` method | Empty or placeholder module class |

For the full list of diagnostics, see `docs/CONSTITUTION.md`.

## Key Constraints

- Source generator targets **netstandard2.0** with `IIncrementalGenerator`
- Modules need `<FrameworkReference Include="Microsoft.AspNetCore.App" />`
- Module Vite builds use **library mode** — externalize React, React-DOM, @inertiajs/react
- **TreatWarningsAsErrors** is enabled globally
- File-scoped namespaces (error), usings outside namespace (error), prefer `var`
- Naming: interfaces `IFoo`, public `PascalCase`, private fields `_camelCase`
