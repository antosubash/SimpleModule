---
outline: deep
---

# Project Structure

A SimpleModule solution follows a consistent directory layout that separates the framework, your feature modules, frontend packages, and the host application.

::: info Two layouts
This page shows two different directory layouts:

- **CLI-scaffolded projects** (what `sm new project` generates for you) use `src/modules/` for feature modules.
- **The SimpleModule framework repository** itself uses `modules/` at the repo root.

The examples below are labelled so you know which is which. If you are building an application with SimpleModule, the CLI-scaffolded layout is the one that matters.
:::

## Top-Level Layout (CLI-scaffolded project)

When you run `sm new project MyApp`, the resulting solution looks like this:

```
MyApp/
├── src/
│   ├── modules/                    # Your feature modules
│   │   ├── SimpleModule.Customers/
│   │   │   ├── src/
│   │   │   │   ├── SimpleModule.Customers/
│   │   │   │   └── SimpleModule.Customers.Contracts/
│   │   │   └── tests/
│   │   │       └── SimpleModule.Customers.Tests/
│   │   └── ...
│   └── MyApp.Host/                 # Host application
│       ├── ClientApp/
│       ├── Program.cs
│       └── wwwroot/
├── MyApp.slnx                      # Solution file
├── package.json                    # Root npm workspace config
└── Directory.Build.props           # Shared MSBuild properties
```

The CLI consumes the framework packages from NuGet, so `framework/`, `packages/`, and `cli/` are not present in a scaffolded app.

## Top-Level Layout (framework repository)

The SimpleModule framework repository itself lays the source out differently, because it hosts the framework packages alongside a reference host and demo modules:

```
SimpleModule/
├── framework/                    # Core framework packages
│   ├── SimpleModule.Core/
│   ├── SimpleModule.Generator/
│   ├── SimpleModule.Database/
│   ├── SimpleModule.Hosting/
│   ├── SimpleModule.Storage/      # Storage provider abstraction
│   ├── SimpleModule.Storage.Local/ # Local filesystem storage
│   ├── SimpleModule.Storage.S3/    # AWS S3 storage
│   └── SimpleModule.Storage.Azure/ # Azure Blob storage
├── modules/                      # Demo / built-in modules (framework repo only)
│   ├── Admin/
│   ├── AuditLogs/
│   ├── BackgroundJobs/
│   ├── Dashboard/
│   ├── Email/
│   ├── FeatureFlags/
│   ├── FileStorage/
│   ├── Localization/
│   ├── OpenIddict/
│   ├── Permissions/
│   ├── RateLimiting/
│   ├── Settings/
│   ├── Tenants/
│   └── Users/
├── packages/                     # Frontend npm packages
│   ├── SimpleModule.Client/
│   ├── SimpleModule.UI/
│   ├── SimpleModule.Theme.Default/
│   └── SimpleModule.TsConfig/
├── template/
│   └── SimpleModule.Host/        # Reference host application
│       ├── ClientApp/
│       ├── Program.cs
│       └── wwwroot/
├── cli/
│   └── SimpleModule.Cli/         # The sm CLI tool
├── tools/                        # Non-module .NET utilities (dev-time tooling)
│   └── SimpleModule.DevTools/
├── tests/                        # Framework-level test projects
├── SimpleModule.slnx             # Solution file
├── package.json                  # Root npm workspace config
└── Directory.Build.props         # Shared MSBuild properties
```

## Framework Projects

The `framework/` directory contains the core packages that power SimpleModule. You reference these but rarely modify them.

### SimpleModule.Core

The foundation. Defines all the interfaces and attributes that modules implement:

- **`IModule`** -- the module contract. Implement this to define a module.
- **`[Module]` attribute** -- marks a class as a module with metadata (name, route prefix).
- **`IEndpoint` / `IViewEndpoint`** -- endpoint contracts. `IEndpoint` for API routes, `IViewEndpoint` for pages that render React views.
- **`[Dto]` attribute** -- marks types for source generator processing (JSON serialization, TypeScript extraction).
- **`IMenuRegistry`** -- register navigation menu items from your module.
- **`IEvent`** -- marker interface for events used in cross-module communication (publishing uses Wolverine's `IMessageBus`; see [Events](/guide/events)).
- **`Inertia`** -- server-side helpers for rendering React pages with props.

### SimpleModule.Generator

A Roslyn incremental source generator targeting **netstandard2.0** (required by the compiler toolchain). It runs at build time and scans referenced assemblies to discover:

- Classes with `[Module]` implementing `IModule`
- Classes implementing `IEndpoint` or `IViewEndpoint`
- Types decorated with `[Dto]`

It generates:

| Generated method | Purpose |
|-----------------|---------|
| `AddModules()` | Calls each module's `ConfigureServices`. Invoked by `builder.AddSimpleModule()`. |
| `MapModuleEndpoints()` | Maps all discovered endpoints with route prefixes. Invoked by `app.UseSimpleModule()`. |
| `CollectModuleMenuItems()` | Builds the navigation menu from module registrations. Invoked by `app.UseSimpleModule()`. |
| JSON serializer contexts | AOT-friendly serialization for `[Dto]` types |
| TypeScript interfaces | Embedded TS definitions extracted by build tooling |
| View page registry | Maps view endpoints to React components |

::: tip User-facing entrypoints
You call `builder.AddSimpleModule()` and `await app.UseSimpleModule()` from your host. These wrappers in `SimpleModule.Hosting` delegate to the generated methods above, so you do not invoke `AddModules()`, `MapModuleEndpoints()`, or `CollectModuleMenuItems()` directly.
:::

::: tip Inspecting Generated Code
In Visual Studio or Rider, expand **Dependencies > Analyzers > SimpleModule.Generator** to see exactly what code the generator produces. This is useful for debugging registration issues.
:::

### SimpleModule.Database

Multi-provider database support built on EF Core. Handles:

- **Provider abstraction** -- SQLite, PostgreSQL, and SQL Server behind a unified configuration API
- **Schema isolation** -- each module's tables are automatically namespaced (table prefixes for SQLite, schemas for PostgreSQL/SQL Server)
- **`ModuleDbContextInfo`** -- metadata that each module registers to declare its database context and schema name

### SimpleModule.Hosting

Module registration infrastructure and Inertia page rendering. Exposes the two user-facing entry points that your host's `Program.cs` calls -- `builder.AddSimpleModule()` and `await app.UseSimpleModule()` -- which in turn invoke the generated `AddModules()`, `MapModuleEndpoints()`, and `CollectModuleMenuItems()` methods. Handles service collection extensions, endpoint routing integration, module lifecycle management, and renders the static HTML shell with embedded JSON props for React hydration.

### SimpleModule.Storage

File storage abstraction with `IStorageProvider` interface (save, get, delete, exists, list). Three provider implementations:

- **SimpleModule.Storage.Local** -- local filesystem storage
- **SimpleModule.Storage.S3** -- AWS S3 and S3-compatible services
- **SimpleModule.Storage.Azure** -- Azure Blob Storage

## Tools

The `tools/` directory holds non-module .NET utilities consumed by the host or the framework bootstrap. They are not modules and do not go under `modules/`.

### SimpleModule.DevTools

Development utilities including hot reload support, diagnostic middleware, and developer experience tooling. The host does not need explicit dev-only wiring in `Program.cs` -- DevTools is imported via the `SimpleModule.Hosting.targets` MSBuild import in the host's csproj and activates automatically in development builds.

## Module Structure

Every module follows a **three-project pattern**: implementation, contracts, and tests. Project directories and assembly names use the `SimpleModule.{Name}` prefix (enforced by diagnostic SM0052).

```
modules/Customers/                        # (framework repo layout -- use src/modules/SimpleModule.Customers/ in a CLI-scaffolded app)
├── src/
│   ├── SimpleModule.Customers/                        # Implementation (private)
│   │   ├── SimpleModule.Customers.csproj
│   │   ├── CustomersModule.cs                         # Module class with [Module] attribute
│   │   ├── CustomersDbContext.cs                      # EF Core DbContext (module root)
│   │   ├── CustomerService.cs                         # ICustomerContracts implementation (module root)
│   │   ├── EntityConfigurations/                      # IEntityTypeConfiguration<T> classes
│   │   ├── Endpoints/
│   │   │   └── Customers/
│   │   │       ├── BrowseCustomers.cs                 # GET /customers
│   │   │       ├── CreateCustomer.cs                  # POST /customers/create
│   │   │       └── ManageCustomer.cs                  # GET /customers/{id}
│   │   ├── Pages/                                     # React components live alongside their view endpoints
│   │   │   ├── index.ts                               # React page registry
│   │   │   ├── Browse.tsx                             # React page component
│   │   │   ├── BrowseEndpoint.cs                      # Matching IViewEndpoint
│   │   │   ├── Create.tsx
│   │   │   ├── CreateEndpoint.cs
│   │   │   ├── Edit.tsx
│   │   │   ├── EditEndpoint.cs
│   │   │   ├── Manage.tsx
│   │   │   └── ManageEndpoint.cs
│   │   ├── vite.config.ts                             # Vite library mode config
│   │   └── package.json                               # npm package with peer deps
│   └── SimpleModule.Customers.Contracts/              # Public API (shared)
│       ├── SimpleModule.Customers.Contracts.csproj
│       ├── ICustomerContracts.cs                      # Contract interface
│       ├── Customer.cs                                # [Dto] public record
│       ├── CreateCustomerRequest.cs                   # [Dto] request shape
│       ├── UpdateCustomerRequest.cs                   # [Dto] request shape
│       ├── CustomerId.cs                              # Strongly-typed id
│       ├── CustomersConstants.cs                      # Shared constants
│       └── Events/                                    # Cross-module event records
└── tests/
    └── SimpleModule.Customers.Tests/                  # Test project
        ├── SimpleModule.Customers.Tests.csproj
        └── Endpoints/
            └── BrowseCustomersTests.cs
```

There is no separate `Views/` directory -- React components (`*.tsx`) live directly in `Pages/` next to their matching `*Endpoint.cs` view endpoints. Likewise the DbContext and the contracts service implementation sit at the module root rather than inside `Data/` or `Services/` folders.

### Implementation Project (`SimpleModule.Customers/`)

This is the private implementation. No other module should reference this project directly. It contains:

- **Module class** -- the `[Module]`-decorated class that registers services
- **Endpoints** -- classes implementing `IEndpoint` or `IViewEndpoint`, auto-discovered by the generator
- **Data layer** -- EF Core DbContext at the module root with `EntityConfigurations/` for `IEntityTypeConfiguration<T>` classes (all `internal`)
- **Services** -- implementation of the contract interface, kept at the module root
- **Frontend** -- React pages, Vite config, and the page registry

The `.csproj` file uses `Microsoft.NET.Sdk` with a framework reference to ASP.NET and references `SimpleModule.Hosting` (which transitively brings in `SimpleModule.Core`). Real modules reference `SimpleModule.Hosting` rather than `SimpleModule.Core` directly:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <ProjectReference Include="../../../../framework/SimpleModule.Hosting/SimpleModule.Hosting.csproj" />
    <ProjectReference Include="../SimpleModule.Customers.Contracts/SimpleModule.Customers.Contracts.csproj" />
  </ItemGroup>
</Project>
```

### Contracts Project (`SimpleModule.Customers.Contracts/`)

The public face of the module. Other modules depend on this project when they need to interact with Customers. Types live at the root of the contracts project (no `Dtos/` folder). It contains:

- **Contract interface** (`ICustomerContracts`) -- methods other modules can call
- **Public record types** marked with `[Dto]` -- `Customer`, `CreateCustomerRequest`, `UpdateCustomerRequest`, strongly-typed ids such as `CustomerId`, and shared constants in `CustomersConstants`
- **`Events/`** -- cross-module event records published through the event bus

```csharp
// ICustomerContracts.cs
public interface ICustomerContracts
{
    Task<List<Customer>> GetAllAsync(CancellationToken cancellationToken);
    Task<Customer?> GetByIdAsync(CustomerId id, CancellationToken cancellationToken);
    Task<Customer> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken);
}
```

```csharp
// Customer.cs
[Dto]
public sealed record Customer(CustomerId Id, string Name, string Email, string? Notes);

// CreateCustomerRequest.cs
[Dto]
public sealed record CreateCustomerRequest(string Name, string Email, string? Notes);
```

::: warning Contracts Are the Boundary
The contracts project must never reference the implementation project. It depends only on `SimpleModule.Core`. This ensures that modules cannot access each other's internals -- the compiler enforces the boundary.
:::

### Test Project (`SimpleModule.Customers.Tests/`)

An xUnit.v3 test project with access to the shared test infrastructure:

```csharp
public sealed class BrowseCustomersTests(
    SimpleModuleWebApplicationFactory factory)
    : IClassFixture<SimpleModuleWebApplicationFactory>
{
    [Fact]
    public async Task BrowseCustomers_WithCustomers_ReturnsAll()
    {
        // Arrange
        var client = factory.CreateAuthenticatedClient();

        // Act
        var response = await client.GetAsync("/customers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

Test naming convention: `Method_Scenario_Expected` with underscores (configured in `.editorconfig`).

## The Host Application

The host app lives at `template/SimpleModule.Host/` and is the entry point that ties everything together.

### Program.cs

The host's `Program.cs` calls two user-facing entry points provided by `SimpleModule.Hosting`:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Registers all module services (calls the generated AddModules() internally)
builder.AddSimpleModule();

var app = builder.Build();

// Maps all discovered endpoints and collects module menu items
// (calls the generated MapModuleEndpoints() and CollectModuleMenuItems() internally)
await app.UseSimpleModule();

app.Run();
```

`AddSimpleModule` and `UseSimpleModule` wrap the generated `AddModules()`, `MapModuleEndpoints()`, and `CollectModuleMenuItems()` methods, so these two calls replace what would otherwise be dozens of manual registration lines. The source generator produces the underlying methods based on what it discovers in your module assemblies.

### ClientApp

The React entry point at `template/SimpleModule.Host/ClientApp/`:

```
ClientApp/
├── app.tsx              # Inertia bootstrap + page resolver
├── types/               # Generated TypeScript interfaces from [Dto] types
└── ...
```

The page resolver in `app.tsx` dynamically imports module bundles based on the route name. When Inertia navigates to `Customers/Browse`, it resolves to `/_content/Customers/Customers.pages.js` and loads the corresponding React component.

```typescript
// Simplified page resolution logic
const moduleName = pageName.split('/')[0];
const bundle = await import(`/_content/${moduleName}/${moduleName}.pages.js`);
const component = bundle.pages[pageName];
```

### wwwroot

Static assets and module page bundles are served from `wwwroot/`. Each module's Vite build outputs its `{ModuleName}.pages.js` bundle here (via the `_content/{ModuleName}/` convention).

## Frontend Packages

The `packages/` directory contains shared npm packages used by modules and the host app. These are managed via npm workspaces.

### @simplemodule/client

Vite plugin and utilities for module builds:

- **Vite plugin** -- configures library mode builds with the right externals (React, ReactDOM, @inertiajs/react)
- **Page resolution** -- utility for resolving page components from module bundles
- **Vendoring** -- handles shared dependency bundling

### @simplemodule/ui

A component library built on Radix UI with Tailwind CSS styling:

```tsx
import { Button } from '@simplemodule/ui/components';
import { cn } from '@simplemodule/ui/lib/utils';
```

Provides pre-built, accessible components: buttons, dialogs, tables, forms, dropdowns, and more. All styled with Tailwind CSS and customizable through the theme package.

### @simplemodule/theme-default

The default Tailwind CSS theme. Provides base styles, color tokens, and design system foundations that the UI components and your module pages consume.

### @simplemodule/tsconfig

Shared TypeScript base configuration (`packages/SimpleModule.TsConfig`) that modules and the host app extend from. Keeps compiler options consistent across every workspace.

## Cross-Module Communication

Modules communicate through two mechanisms:

### Contracts (Synchronous)

Module A depends on Module B's contracts project and calls its interface methods:

```
modules/Invoices/src/SimpleModule.Invoices/SimpleModule.Invoices.csproj
  └── references → modules/Customers/src/SimpleModule.Customers.Contracts/
```

```csharp
// In an Invoices endpoint
public sealed class CreateInvoice : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/", Handler);

    private static async Task<IResult> Handler(
        CreateInvoiceRequest request,
        ICustomerContracts customers,   // injected from Customers module
        IInvoiceContracts invoices,
        CancellationToken cancellationToken)
    {
        var customer = await customers.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null)
            return TypedResults.NotFound();

        var invoice = await invoices.CreateAsync(request, cancellationToken);
        return TypedResults.Created($"/invoices/{invoice.Id}", invoice);
    }
}
```

### Events (Asynchronous)

For loose coupling, modules publish events through Wolverine's `IMessageBus`:

```csharp
// Publisher (in Invoices module)
await bus.PublishAsync(new InvoiceCreatedEvent(invoice.Id, invoice.CustomerId));

// Handler (in Customers module, or any module) -- discovered by naming convention
public sealed class UpdateBalanceOnInvoiceCreated(ICustomerContracts customers)
{
    public Task Handle(InvoiceCreatedEvent evt, CancellationToken ct) =>
        customers.IncrementBalanceAsync(evt.CustomerId, ct);
}
```

Wolverine discovers handlers by naming convention (`*Handler`/`*Consumer` class with a `Handle`/`Consume` method). See [Events](/guide/events) for delivery semantics and best practices.

## Solution File

The `SimpleModule.slnx` file at the root ties every project together. When you use `sm new module`, it automatically adds the new module's three projects to the solution.

```bash
dotnet build              # builds everything
dotnet test               # tests everything
```

## Build Configuration

### Directory.Build.props

Shared MSBuild properties applied to all projects in the solution:

- **`TreatWarningsAsErrors`** enabled globally
- **`AnalysisLevel=latest-all`** with **`AnalysisMode=All`** for comprehensive code analysis
- Suppressed rules are listed in `.editorconfig`
- File-scoped namespaces enforced as errors

### npm Workspaces

The root `package.json` enumerates workspaces explicitly rather than using a blanket `packages/*` glob. In the framework repo the list is:

- `modules/*/src/*` -- module frontend code
- `packages/SimpleModule.Client`
- `packages/SimpleModule.Theme.Default`
- `packages/SimpleModule.TsConfig`
- `packages/SimpleModule.UI`
- `template/SimpleModule.Host/ClientApp` -- the host app's React entry point
- `tests/e2e`
- `tests/k6`
- `docs/site`
- `website`

This allows a single `npm install` at the root to resolve all dependencies, and commands like `npm run build` to build everything.

## Adding a New Module Manually

If you prefer not to use the CLI, here are the steps:

1. Create the directory structure under `src/modules/SimpleModule.<Name>/` (CLI-scaffolded app) or `modules/<Name>/` (framework repo)
2. Create the contracts project (`SimpleModule.<Name>.Contracts.csproj`) referencing only `SimpleModule.Core`
3. Create the implementation project (`SimpleModule.<Name>.csproj`) referencing `SimpleModule.Hosting` and the contracts project, with `<FrameworkReference Include="Microsoft.AspNetCore.App" />`
4. Create the test project (`SimpleModule.<Name>.Tests.csproj`)
5. Add a `[Module]` class implementing `IModule`
6. Add endpoints implementing `IEndpoint` or `IViewEndpoint`
7. Set up the frontend: `package.json`, `vite.config.ts`, `Pages/index.ts`, React components
8. Add a `ProjectReference` in `template/SimpleModule.Host/SimpleModule.Host.csproj`
9. Add all three projects to `SimpleModule.slnx`
10. Run `dotnet build` -- the source generator picks up the new module automatically

::: tip Use the CLI
`sm new module <Name>` does all of this in seconds and ensures nothing is missed. Use `sm doctor --fix` afterward to verify everything is wired correctly.
:::

## Next Steps

- [Modules](/guide/modules) -- how modules are defined and discovered
- [Endpoints](/guide/endpoints) -- API and view endpoint patterns
- [Contracts & DTOs](/guide/contracts) -- cross-module communication boundaries
