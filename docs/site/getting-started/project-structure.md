---
outline: deep
---

# Project Structure

A SimpleModule solution follows a consistent directory layout that separates the framework, your feature modules, frontend packages, and the host application.

## Top-Level Layout

```
MyApp/
├── framework/                    # Core framework packages
│   ├── SimpleModule.Core/
│   ├── SimpleModule.Generator/
│   ├── SimpleModule.Database/
│   ├── SimpleModule.Blazor/
│   ├── SimpleModule.Hosting/
│   ├── SimpleModule.DevTools/
│   ├── SimpleModule.Agents/       # AI agent runtime and registry
│   ├── SimpleModule.AI.Anthropic/ # Claude API provider
│   ├── SimpleModule.AI.OpenAI/    # OpenAI API provider
│   ├── SimpleModule.AI.AzureOpenAI/ # Azure OpenAI provider
│   ├── SimpleModule.AI.Ollama/    # Ollama local model provider
│   ├── SimpleModule.Rag/          # RAG pipeline and knowledge store
│   ├── SimpleModule.Rag.StructuredRag/      # Structured RAG pipeline
│   ├── SimpleModule.Rag.VectorStore.InMemory/ # In-memory vector store (dev)
│   ├── SimpleModule.Rag.VectorStore.Postgres/ # PostgreSQL vector store
│   ├── SimpleModule.Storage/      # Storage provider abstraction
│   ├── SimpleModule.Storage.Local/ # Local filesystem storage
│   ├── SimpleModule.Storage.S3/    # AWS S3 storage
│   └── SimpleModule.Storage.Azure/ # Azure Blob storage
├── modules/                      # Your feature modules
│   ├── Admin/
│   ├── Agents/
│   ├── AuditLogs/
│   ├── BackgroundJobs/
│   ├── Dashboard/
│   ├── FeatureFlags/
│   ├── FileStorage/
│   ├── Localization/
│   ├── Marketplace/
│   ├── OpenIddict/
│   ├── Orders/
│   ├── PageBuilder/
│   ├── Permissions/
│   ├── Products/
│   ├── Rag/
│   ├── Settings/
│   ├── Tenants/
│   └── Users/
├── packages/                     # Frontend npm packages
│   ├── SimpleModule.Client/
│   ├── SimpleModule.UI/
│   └── SimpleModule.Theme.Default/
├── template/
│   └── SimpleModule.Host/        # The host application
│       ├── ClientApp/
│       ├── Program.cs
│       └── wwwroot/
├── cli/
│   └── SimpleModule.Cli/         # The sm CLI tool
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
- **`IEventBus`** -- publish events for cross-module communication.
- **`IEventHandler<T>`** -- handle events from other modules.
- **`Inertia`** -- server-side helpers for rendering React pages with props.

### SimpleModule.Generator

A Roslyn incremental source generator targeting **netstandard2.0** (required by the compiler toolchain). It runs at build time and scans referenced assemblies to discover:

- Classes with `[Module]` implementing `IModule`
- Classes implementing `IEndpoint` or `IViewEndpoint`
- Types decorated with `[Dto]`

It generates:

| Generated method | Purpose |
|-----------------|---------|
| `AddModules()` | Calls each module's `ConfigureServices` |
| `MapModuleEndpoints()` | Maps all discovered endpoints with route prefixes |
| `CollectModuleMenuItems()` | Builds the navigation menu from module registrations |
| JSON serializer contexts | AOT-friendly serialization for `[Dto]` types |
| TypeScript interfaces | Embedded TS definitions extracted by build tooling |
| Razor component discovery | Assembly metadata for Blazor SSR |

::: tip Inspecting Generated Code
In Visual Studio or Rider, expand **Dependencies > Analyzers > SimpleModule.Generator** to see exactly what code the generator produces. This is useful for debugging registration issues.
:::

### SimpleModule.Database

Multi-provider database support built on EF Core. Handles:

- **Provider abstraction** -- SQLite, PostgreSQL, and SQL Server behind a unified configuration API
- **Schema isolation** -- each module's tables are automatically namespaced (table prefixes for SQLite, schemas for PostgreSQL/SQL Server)
- **`ModuleDbContextInfo`** -- metadata that each module registers to declare its database context and schema name

### SimpleModule.Blazor

The Blazor SSR shell that serves as the bridge between ASP.NET and React. It renders the initial HTML page with serialized Inertia props, which React then hydrates on the client side.

### SimpleModule.Hosting

Module registration infrastructure. Provides the runtime plumbing that the generated `AddModules()` and `MapModuleEndpoints()` methods call into. Handles service collection extensions, endpoint routing integration, and module lifecycle management.

### SimpleModule.DevTools

Development utilities including hot reload support, diagnostic middleware, and developer experience tooling that is excluded from production builds.

### SimpleModule.Agents

AI agent runtime and orchestration. Provides `IAgentRegistry` for agent discovery, `AgentChatService` for chat (streaming and non-streaming), `IAgentToolProvider` with `[AgentTool]` attribute for tool discovery, and middleware for rate limiting, token tracking, and guardrails (PII redaction, prompt injection detection).

### SimpleModule.AI.*

AI provider integrations implementing `IChatClient` from Microsoft.Extensions.AI:

- **SimpleModule.AI.Anthropic** -- Claude API via the Anthropic SDK
- **SimpleModule.AI.OpenAI** -- OpenAI API
- **SimpleModule.AI.AzureOpenAI** -- Azure OpenAI Service
- **SimpleModule.AI.Ollama** -- Ollama for local model inference

### SimpleModule.Rag

Retrieval-augmented generation pipeline. Defines `IRagPipeline` for querying a knowledge base and `IKnowledgeStore` for indexing documents. Includes `KnowledgeIndexingHostedService` for background indexing with deduplication.

- **SimpleModule.Rag.StructuredRag** -- Structured RAG implementation (table, graph, algorithm, catalogue, chunk formats)
- **SimpleModule.Rag.VectorStore.InMemory** -- In-memory vector store for development and testing
- **SimpleModule.Rag.VectorStore.Postgres** -- PostgreSQL-backed vector store for production

### SimpleModule.Storage

File storage abstraction with `IStorageProvider` interface (save, get, delete, exists, list). Three provider implementations:

- **SimpleModule.Storage.Local** -- local filesystem storage
- **SimpleModule.Storage.S3** -- AWS S3 and S3-compatible services
- **SimpleModule.Storage.Azure** -- Azure Blob Storage

## Module Structure

Every module follows a **three-project pattern**: implementation, contracts, and tests.

```
modules/Products/
├── src/
│   ├── Products/                        # Implementation (private)
│   │   ├── Products.csproj
│   │   ├── ProductsModule.cs            # Module class with [Module] attribute
│   │   ├── Data/
│   │   │   ├── ProductsDbContext.cs      # EF Core DbContext
│   │   │   └── Entities/                # Database entities (internal)
│   │   ├── Endpoints/
│   │   │   └── Products/
│   │   │       ├── BrowseProducts.cs    # GET /products
│   │   │       ├── CreateProduct.cs     # POST /products/create
│   │   │       └── ManageProduct.cs     # GET /products/{id}
│   │   ├── Services/
│   │   │   └── ProductService.cs        # IProductContracts implementation
│   │   ├── Pages/
│   │   │   └── index.ts                 # React page registry
│   │   ├── Views/
│   │   │   ├── Browse.tsx               # React page components
│   │   │   ├── Create.tsx
│   │   │   └── Manage.tsx
│   │   ├── vite.config.ts               # Vite library mode config
│   │   └── package.json                 # npm package with peer deps
│   └── Products.Contracts/              # Public API (shared)
│       ├── Products.Contracts.csproj
│       ├── IProductContracts.cs          # Contract interface
│       └── Dtos/
│           └── ProductDto.cs             # [Dto] types
└── tests/
    └── Products.Tests/                  # Test project
        ├── Products.Tests.csproj
        └── Endpoints/
            └── BrowseProductsTests.cs
```

### Implementation Project (`Products/`)

This is the private implementation. No other module should reference this project directly. It contains:

- **Module class** -- the `[Module]`-decorated class that registers services
- **Endpoints** -- classes implementing `IEndpoint` or `IViewEndpoint`, auto-discovered by the generator
- **Data layer** -- EF Core DbContext, entities, and migrations (all `internal`)
- **Services** -- implementations of the contract interface
- **Frontend** -- React pages, Vite config, and the page registry

The `.csproj` file uses `Microsoft.NET.Sdk` with a framework reference to ASP.NET:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <ProjectReference Include="../../framework/SimpleModule.Core/SimpleModule.Core.csproj" />
    <ProjectReference Include="../Products.Contracts/Products.Contracts.csproj" />
  </ItemGroup>
</Project>
```

### Contracts Project (`Products.Contracts/`)

The public face of the module. Other modules depend on this project when they need to interact with Products. It contains only:

- **Contract interface** (`IProductContracts`) -- methods other modules can call
- **DTO types** marked with `[Dto]` -- data shapes shared across module boundaries

```csharp
// IProductContracts.cs
public interface IProductContracts
{
    Task<List<ProductDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<ProductDto?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken);
}
```

```csharp
// Dtos/ProductDto.cs
[Dto]
public sealed record ProductDto(int Id, string Name, decimal Price, string? Description);

[Dto]
public sealed record CreateProductRequest(string Name, decimal Price, string? Description);
```

::: warning Contracts Are the Boundary
The contracts project must never reference the implementation project. It depends only on `SimpleModule.Core`. This ensures that modules cannot access each other's internals -- the compiler enforces the boundary.
:::

### Test Project (`Products.Tests/`)

An xUnit.v3 test project with access to the shared test infrastructure:

```csharp
public sealed class BrowseProductsTests(
    SimpleModuleWebApplicationFactory factory)
    : IClassFixture<SimpleModuleWebApplicationFactory>
{
    [Fact]
    public async Task BrowseProducts_WithProducts_ReturnsAll()
    {
        // Arrange
        var client = factory.CreateAuthenticatedClient();

        // Act
        var response = await client.GetAsync("/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

Test naming convention: `Method_Scenario_Expected` with underscores (configured in `.editorconfig`).

## The Host Application

The host app lives at `template/SimpleModule.Host/` and is the entry point that ties everything together.

### Program.cs

The host's `Program.cs` calls the generated extension methods:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Generated: registers all module services
builder.AddModules();

var app = builder.Build();

// Generated: maps all discovered endpoints with route prefixes
app.MapModuleEndpoints();

// Generated: collects menu items from all modules
app.CollectModuleMenuItems();

app.Run();
```

These three method calls replace what would otherwise be dozens of manual registration lines. The source generator produces them based on what it discovers in your module assemblies.

### ClientApp

The React entry point at `template/SimpleModule.Host/ClientApp/`:

```
ClientApp/
├── app.tsx              # Inertia bootstrap + page resolver
├── types/               # Generated TypeScript interfaces from [Dto] types
└── ...
```

The page resolver in `app.tsx` dynamically imports module bundles based on the route name. When Inertia navigates to `Products/Browse`, it resolves to `/_content/Products/Products.pages.js` and loads the corresponding React component.

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

## Cross-Module Communication

Modules communicate through two mechanisms:

### Contracts (Synchronous)

Module A depends on Module B's contracts project and calls its interface methods:

```
modules/Orders/src/Orders.csproj
  └── references → modules/Products/src/Products.Contracts/
```

```csharp
// In an Orders endpoint
public sealed class CreateOrder : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/", Handler);

    private static async Task<IResult> Handler(
        CreateOrderRequest request,
        IProductContracts products,    // injected from Products module
        IOrderContracts orders,
        CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
            return TypedResults.NotFound();

        var order = await orders.CreateAsync(request, cancellationToken);
        return TypedResults.Created($"/orders/{order.Id}", order);
    }
}
```

### Events (Asynchronous)

For loose coupling, modules publish events through `IEventBus`:

```csharp
// Publisher (in Orders module)
await eventBus.PublishAsync(new OrderCreatedEvent(order.Id, order.ProductId));

// Handler (in Products module, or any module)
public sealed class UpdateStockOnOrderCreated : IEventHandler<OrderCreatedEvent>
{
    public async Task HandleAsync(
        OrderCreatedEvent @event,
        CancellationToken cancellationToken)
    {
        // Reduce stock for the ordered product
    }
}
```

Event handlers run sequentially and failures are isolated -- if one handler throws, the others still execute.

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

The root `package.json` defines npm workspaces covering:

- `modules/*/src/*` -- module frontend code
- `packages/*` -- shared frontend packages
- `template/SimpleModule.Host/ClientApp` -- the host app's React entry point

This allows a single `npm install` at the root to resolve all dependencies, and commands like `npm run build` to build everything.

## Adding a New Module Manually

If you prefer not to use the CLI, here are the steps:

1. Create the directory structure under `modules/<Name>/`
2. Create the contracts project (`<Name>.Contracts.csproj`) referencing only `SimpleModule.Core`
3. Create the implementation project (`<Name>.csproj`) referencing Core and Contracts, with `<FrameworkReference Include="Microsoft.AspNetCore.App" />`
4. Create the test project (`<Name>.Tests.csproj`)
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
