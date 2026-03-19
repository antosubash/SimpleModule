# SimpleModule

A modular monolith framework for .NET with compile-time module discovery via Roslyn source generators. Frontend uses React 19 + Inertia.js served via Blazor SSR.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) (for frontend builds)

## Getting Started

```bash
dotnet build
npm install
dotnet run --project template/SimpleModule.Host     # https://localhost:5001
```

### Docker

```bash
docker compose up
```

Runs the app on `http://localhost:8080` with PostgreSQL 16.

## Architecture

```
framework/
  SimpleModule.Core          # IModule, IEndpoint, [Dto], [Module], IEventBus, IMenuRegistry
  SimpleModule.Generator     # Roslyn source generator (netstandard2.0) — module/endpoint/DTO discovery
  SimpleModule.Database      # Multi-provider DB support (SQLite, PostgreSQL, SQL Server)
  SimpleModule.Blazor        # Blazor SSR shell for Inertia rendering
modules/
  Dashboard/                 # Dashboard module
  Products/                  # Products module (src + contracts + tests)
  Orders/                    # Orders module (src + contracts + tests)
  Users/                     # Users module (src + contracts + tests)
packages/
  SimpleModule.Client        # Vite plugin + page resolution for module frontends
  SimpleModule.UI            # Radix UI component library with Tailwind
  SimpleModule.Theme.Default # Tailwind CSS base theme
template/
  SimpleModule.Host          # Host app (net10.0) — wires modules via generated code
cli/
  SimpleModule.Cli           # `sm` CLI tool for scaffolding and validation
```

### How It Works

1. Modules are .NET class libraries decorated with `[Module("Name", RoutePrefix = "...")]` implementing `IModule`
2. The Roslyn source generator scans referenced assemblies at compile time and emits static registration code — no reflection needed
3. Endpoints implement `IEndpoint` and are auto-discovered and mapped
4. Each module builds its React pages via Vite in library mode into a `{ModuleName}.pages.js` bundle
5. Inertia.js bridges the .NET backend to the React frontend: endpoints call `Inertia.Render("Module/Page", props)`, the Blazor SSR shell delivers the initial HTML, and React hydrates on the client

### Module Communication

Modules communicate through two mechanisms:

- **Contracts** — each module has a `.Contracts` project exposing a public interface (e.g., `IProductContracts`) and shared `[Dto]` types. Modules depend on contract interfaces, never on other module implementations.
- **Event bus** — `IEventBus.PublishAsync<T>()` broadcasts events to all registered `IEventHandler<T>` implementations across modules.

### Database

Each module gets its own isolated storage — table prefixes on SQLite, separate schemas on PostgreSQL/SQL Server. Modules register a `ModuleDbContextInfo` and the framework handles schema creation.

## CLI

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
dotnet test --filter "FullyQualifiedName~MethodName"   # single test method
```

Tests use xUnit.v3, FluentAssertions, Bogus, and NSubstitute. Integration tests run against in-memory SQLite by default. CI also tests against PostgreSQL 16 using the `Database__DefaultConnection` environment variable.

## Linting & Formatting

**C#** — Analyzers enforced via `Directory.Build.props` (`TreatWarningsAsErrors`, `AnalysisLevel=latest-all`). Naming and style rules in `.editorconfig`.

**TypeScript/React** — [Biome](https://biomejs.dev/) configured at repo root. Single quotes, semicolons, 2-space indent, trailing commas, 100-char line width.

```bash
npm run check               # lint + format check
npm run check:fix           # auto-fix
```
