# SimpleModule

> **Experimental** — This project is vibe coded and under active development. APIs, conventions, and structure may change without notice. Use at your own risk.

A modular monolith framework for .NET 10 that uses Roslyn source generators to discover and wire up modules at compile time — no reflection, no manual registration. The frontend is React 19 + Inertia.js, rendered server-side through Blazor SSR and hydrated on the client.

## What it does

- **Compile-time module discovery** — A Roslyn `IIncrementalGenerator` scans referenced assemblies for `[Module]` classes, `IEndpoint`/`IViewEndpoint` implementors, and `[Dto]` types. It emits `AddModules()`, `MapModuleEndpoints()`, JSON serializer contexts, and TypeScript interface definitions. No startup reflection.
- **Module isolation** — Each module has its own database schema (or table prefix on SQLite), its own contracts project, and its own React page bundle. Modules communicate through contract interfaces and an async event bus — never by referencing each other's internals.
- **Full-stack type safety** — `[Dto]`-decorated C# types generate TypeScript interfaces that the React frontend imports. The source generator keeps both sides in sync automatically.
- **Inertia.js bridge** — Endpoints call `Inertia.Render("Module/Page", props)`. The Blazor SSR shell delivers initial HTML with JSON props, then React hydrates client-side. No separate API layer needed for page rendering.
- **Pluggable infrastructure** — File storage (Local, Azure Blob, S3), multi-provider database (SQLite, PostgreSQL, SQL Server), permission system, settings, audit logging, and OpenID Connect are all provided as optional modules.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) (LTS, for frontend builds)

## Getting Started

```bash
dotnet build
npm install
dotnet run --project SimpleModule.AppHost            # starts app + PostgreSQL via Aspire
```

The Aspire AppHost orchestrates the host app and a PostgreSQL container with pgAdmin. The Aspire dashboard gives you distributed traces, structured logs, and resource health out of the box.

To run without Aspire (uses SQLite, no containers needed):

```bash
dotnet run --project template/SimpleModule.Host      # https://localhost:5001
```

### Docker

```bash
docker compose up                                    # http://localhost:8080 with PostgreSQL 16
```

### Development

```bash
npm run dev
```

This starts the .NET backend and Vite watchers for all modules and the ClientApp in parallel. Edit a `.tsx` file, Vite rebuilds instantly (unminified, with source maps). Ctrl+C stops everything.

## Architecture

### Project Structure

```
SimpleModule.AppHost           # .NET Aspire orchestration (app + PostgreSQL + pgAdmin)
SimpleModule.ServiceDefaults   # Aspire service defaults (OpenTelemetry, health checks)
framework/
  SimpleModule.Core            # IModule, IEndpoint, IViewEndpoint, [Dto], [Module], IEventBus
  SimpleModule.Generator       # Roslyn IIncrementalGenerator (netstandard2.0)
  SimpleModule.Database        # Multi-provider DB (SQLite, PostgreSQL, SQL Server)
  SimpleModule.Blazor          # Blazor SSR shell for Inertia page rendering
  SimpleModule.Hosting         # ASP.NET host builder extensions
  SimpleModule.DevTools        # Developer tooling and diagnostics
  SimpleModule.Storage         # File storage abstraction with Local, Azure Blob, and S3 providers
modules/
  Admin, AuditLogs, Dashboard, FileStorage, Orders, PageBuilder,
  Products, Settings, Users
  OpenIddict                   # OpenID Connect / OAuth 2.0 via OpenIddict
  Permissions                  # RBAC and access control
packages/
  SimpleModule.Client          # Vite plugin + page resolution for module frontends
  SimpleModule.UI              # Radix UI component library with Tailwind
  SimpleModule.Theme.Default   # Tailwind CSS base theme
template/
  SimpleModule.Host            # Host app (net10.0) — wires modules via generated code
cli/
  SimpleModule.Cli             # `sm` CLI tool for scaffolding and validation
tests/                         # Framework tests, shared test infrastructure, and e2e tests
tools/                         # Build/dev orchestrators, type extraction, component scaffolding
```

### Request Flow

```
Browser request
  → ASP.NET route handler calls Inertia.Render("Products/Browse", { products })
  → Inertia middleware renders Blazor SSR shell with JSON props
  → Client loads React, dynamically imports module's Products.pages.js bundle
  → React component hydrates with server-provided props
```

Subsequent navigations are XHR — Inertia fetches JSON props and swaps the React component client-side without a full page reload.

### Module Anatomy

Each module follows the same structure. Using Products as an example:

```
modules/Products/
  src/
    Products.Contracts/        # Public interface + DTOs (what other modules depend on)
      IProductContracts.cs     # Service interface
      Product.cs               # [Dto] — generates TypeScript types
      ProductId.cs             # Strongly-typed ID
      CreateProductRequest.cs  # Request DTO
    Products/                  # Implementation (never referenced by other modules)
      ProductsModule.cs        # [Module("Products")] + IModule — DI, menus, permissions
      ProductsDbContext.cs     # Module-scoped EF Core context
      ProductService.cs        # Implements IProductContracts
      Endpoints/Products/      # IEndpoint classes (auto-discovered, auto-mapped)
      Views/                   # React .tsx pages
      Pages/index.ts           # Page registry — maps route names to lazy component imports
  tests/
    Products.Tests/            # xUnit tests for this module
```

### Module Communication

- **Contracts** — Modules expose a `.Contracts` project with a public interface (e.g., `IProductContracts`) and `[Dto]` types. Other modules depend on contract interfaces, never on implementation projects.
- **Event bus** — `IEventBus.PublishAsync<T>()` broadcasts to all `IEventHandler<T>` implementations. Handlers execute sequentially; failures are isolated and collected into an `AggregateException`.

### Database

Each module gets isolated storage — table prefixes on SQLite, separate schemas on PostgreSQL/SQL Server. Modules register a `ModuleDbContextInfo` and the framework handles schema creation. Uses `EnsureCreated()` by default; for production, use EF Core migrations per module.

## CLI

The `sm` tool scaffolds projects, modules, and features:

```bash
sm new project              # scaffold a new SimpleModule solution
sm new module <name>        # create a module with contracts, endpoints, tests, events
sm new feature <name>       # add a feature to an existing module
sm doctor [--fix]           # validate project structure, auto-fix issues
```

## Testing

```bash
dotnet test                                            # all tests
dotnet test --filter "FullyQualifiedName~ClassName"    # single test class
npm run test:e2e                                       # Playwright end-to-end tests
```

Unit and integration tests use xUnit.v3, FluentAssertions, Bogus, and NSubstitute. A shared `SimpleModuleWebApplicationFactory` provides in-memory SQLite, a test auth scheme, and `CreateAuthenticatedClient(params Claim[] claims)` for authenticated requests. CI also runs against PostgreSQL 16.

## Linting & Formatting

**C#** — `TreatWarningsAsErrors` with `AnalysisLevel=latest-all` and `AnalysisMode=All` enforced via `Directory.Build.props`. Style rules in `.editorconfig`.

**TypeScript/React** — [Biome](https://biomejs.dev/) with single quotes, semicolons, 2-space indent, trailing commas, 100-char line width.

```bash
npm run check               # Biome lint + format check + page registry validation
npm run check:fix           # auto-fix
```
