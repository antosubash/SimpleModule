---
name: simplemodule
description: >
  Comprehensive guide for working with the SimpleModule modular monolith framework.
  Use when creating modules, adding features, writing endpoints, configuring services,
  working with the event bus, permissions, menus, settings, database contexts, or
  understanding the project architecture. Triggers on: "add module", "new module",
  "add feature", "create endpoint", "module architecture", "how does SimpleModule work",
  "event bus", "permissions", "menu", "settings", "database context", "contracts",
  "IModule", "IEndpoint", "IViewEndpoint", "Inertia", "CrudEndpoints".
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

## Key Constraints

- Source generator targets **netstandard2.0** with `IIncrementalGenerator`
- Modules need `<FrameworkReference Include="Microsoft.AspNetCore.App" />`
- Module Vite builds use **library mode** — externalize React, React-DOM, @inertiajs/react
- **TreatWarningsAsErrors** is enabled globally
- File-scoped namespaces (error), usings outside namespace (error), prefer `var`
- Naming: interfaces `IFoo`, public `PascalCase`, private fields `_camelCase`
