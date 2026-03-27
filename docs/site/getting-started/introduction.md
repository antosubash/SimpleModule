---
outline: deep
---

# Introduction

SimpleModule is a **modular monolith framework for .NET** that combines compile-time module discovery, database schema isolation, and a modern React frontend into a single, cohesive development experience.

You build feature modules as self-contained units. A Roslyn source generator wires them together at compile time. No reflection, no runtime scanning, no XML configuration. Everything is statically known before your app starts.

## Why a Modular Monolith?

Most teams face a choice between two extremes: a traditional monolith where everything is tangled together, or microservices where everything is distributed. Modular monoliths give you a middle path that captures the best of both.

### Over Traditional Monoliths

A traditional monolith starts simple but degrades as it grows. Feature code bleeds across boundaries. A change in billing breaks product search. Nobody knows which database tables belong to which feature.

SimpleModule enforces structure from day one:

- **Module isolation** -- each module gets its own database schema, its own permissions, its own settings. You cannot accidentally reach into another module's internals.
- **Contract-based communication** -- modules talk through explicit interfaces and events. Dependencies are visible in the project graph, not hidden in shared database tables.
- **Independent development** -- teams can work on separate modules without merge conflicts in shared code. Each module has its own test project, its own frontend bundle, its own release cycle.

### Over Microservices

Microservices solve real problems but introduce operational complexity that many teams don't need:

- **No network boundaries** -- method calls between modules are in-process. No serialization overhead, no retry logic, no service discovery.
- **Shared transactions** -- when two modules need to participate in the same operation, they share a database transaction. No distributed sagas, no eventual consistency headaches.
- **Simpler operations** -- one deployment artifact, one process to monitor, one log stream to search. You don't need Kubernetes to ship a CRUD app.
- **Easy extraction** -- if a module outgrows the monolith, the contract boundary is already in place. Moving to a separate service means swapping the in-process contract implementation for an HTTP client.

## Key Features

### Compile-Time Discovery

SimpleModule includes a Roslyn incremental source generator that scans your assemblies at build time. It finds:

- Classes decorated with `[Module]` that implement `IModule`
- Endpoint classes implementing `IEndpoint` or `IViewEndpoint`
- Data transfer objects marked with `[Dto]`

The generator emits extension methods -- `AddModules()`, `MapModuleEndpoints()`, `CollectModuleMenuItems()` -- that your host app calls in `Program.cs`. There is no reflection at runtime. The generated code is plain C# that you can inspect in your IDE.

```csharp
// Program.cs — calls generated extension methods
var builder = WebApplication.CreateBuilder(args);
builder.AddModules();         // registers all module services

var app = builder.Build();
app.MapModuleEndpoints();     // maps all discovered endpoints
app.CollectModuleMenuItems(); // builds the navigation menu
```

### React + Inertia.js Frontend

Each module ships its own React pages as a Vite library-mode bundle. The host app uses Blazor SSR to deliver the initial HTML with serialized props, then React hydrates on the client.

This means:

- **Server-driven navigation** -- Inertia.js handles page transitions. No client-side router to configure.
- **Module-scoped bundles** -- each module builds a `{ModuleName}.pages.js` file. The host app dynamically imports the right bundle based on the route.
- **Full React ecosystem** -- use any React library. The framework doesn't limit what you can do on the client.

```typescript
// modules/Products/src/Products/Pages/index.ts
export const pages: Record<string, any> = {
  'Products/Browse': () => import('../Views/Browse'),
  'Products/Manage': () => import('../Views/Manage'),
  'Products/Create': () => import('../Views/Create'),
};
```

### Module Isolation

Every module is a self-contained unit with clear boundaries:

| Concern | Isolation mechanism |
|---------|-------------------|
| Database | Separate schema (PostgreSQL/SQL Server) or table prefix (SQLite) |
| Permissions | Module-scoped permission definitions |
| Settings | Module-scoped settings with typed access |
| Menus | Each module registers its own menu items |
| Frontend | Separate Vite bundle per module |
| Communication | Contracts (interfaces) and events only |

::: warning No Backdoors
Modules cannot reference each other's implementation projects. They depend on `.Contracts` projects only, which contain interfaces and `[Dto]` types. The compiler enforces this boundary.
:::

### CLI Tooling

The `sm` command-line tool handles scaffolding and project health:

```bash
sm new project MyApp         # scaffold a new SimpleModule solution
sm new module Products       # create a module with contracts, endpoints, tests
sm new feature Products/Browse  # add a feature to an existing module
sm doctor --fix              # validate project structure, auto-fix issues
```

### Multi-Provider Database

SimpleModule supports multiple database providers with automatic schema isolation:

| Provider | Isolation strategy | Use case |
|----------|-------------------|----------|
| SQLite | Table prefixes (`Products_Items`) | Local development, testing |
| PostgreSQL | Schemas (`products.items`) | Production |
| SQL Server | Schemas (`products.items`) | Production |

Each module registers its database context through `ModuleDbContextInfo`. The framework handles schema creation and provider-specific configuration.

## How It Works

The core workflow is straightforward:

**1. Define a module**

```csharp
[Module("Products", RoutePrefix = "products")]
public sealed class ProductsModule : IModule
{
    public static void ConfigureServices(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IProductContracts, ProductService>();
    }
}
```

**2. Add endpoints**

```csharp
public sealed class BrowseProducts : IViewEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/", Handler);

    private static IResult Handler(IProductContracts products) =>
        Inertia.Render("Products/Browse", new
        {
            Products = products.GetAll()
        });
}
```

**3. Build the project**

The Roslyn source generator runs during compilation. It discovers `ProductsModule`, finds `BrowseProducts`, and generates the wiring code. You can see the generated files in your IDE under Analyzers.

**4. Everything is registered**

The generated `AddModules()` method calls `ProductsModule.ConfigureServices()`. The generated `MapModuleEndpoints()` method maps `BrowseProducts` under the `/products` route prefix. The generated `CollectModuleMenuItems()` method gathers any menu items the module registered. All at compile time. All type-safe.

::: tip Zero Configuration
You don't write registration code, startup configuration, or reflection-based discovery logic. Add a class, implement an interface, build. The generator handles the rest.
:::

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Runtime | .NET 10 |
| Frontend | React 19, Inertia.js |
| Server rendering | Blazor SSR |
| Build tooling | Vite, Tailwind CSS 4 |
| Source generation | Roslyn incremental generators |
| Component library | Radix UI |
| Testing | xUnit.v3, FluentAssertions, Bogus |
| Database | SQLite, PostgreSQL, SQL Server via EF Core |
| CLI | System.CommandLine |

## Next Steps

- [Quick Start](./quick-start) -- install the CLI and create your first project in under five minutes
- [Project Structure](./project-structure) -- understand how a SimpleModule solution is organized
- [Modules](/guide/modules) -- deep dive into the module system
