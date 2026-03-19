# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Is

Modular monolith framework for .NET with compile-time module discovery via Roslyn source generators. Frontend uses React 19 + Inertia.js served via Blazor SSR.

## Build & Run

```bash
dotnet build
dotnet run --project template/SimpleModule.Host     # runs on https://localhost:5001
```

## Frontend (npm workspaces)

```bash
npm install                          # install all workspace dependencies
npm run check                        # biome lint + format check
npm run check:fix                    # auto-fix lint + formatting
npm run lint                         # lint only
npm run format                       # format only (with write)
```

Workspaces: `modules/*/src/*`, `packages/*`, and `template/SimpleModule.Host/ClientApp`.

## Testing

```bash
dotnet test                                            # all tests
dotnet test --filter "FullyQualifiedName~ClassName"    # single test class
dotnet test --filter "FullyQualifiedName~MethodName"   # single test method
```

Test stack: xUnit.v3, FluentAssertions, Bogus, Microsoft.AspNetCore.Mvc.Testing. SQLite in-memory for unit tests, PostgreSQL for integration tests in CI.

## Architecture

### .NET Backend

- **SimpleModule.Core** — `IModule` interface, `[Module]` attribute, `IEndpoint` interface, `[Dto]` attribute, menu system (`IMenuRegistry`), event bus (`IEventBus`), Inertia integration.
- **SimpleModule.Generator** — Roslyn `IIncrementalGenerator` (netstandard2.0). Scans referenced assemblies for `[Module]` classes, `IEndpoint` implementors, and `[Dto]` types. Generates: `AddModules()`, `MapModuleEndpoints()`, `CollectModuleMenuItems()`, JSON serializers, TypeScript interface definitions, Razor component assembly discovery.
- **SimpleModule.Host** — Host app (net10.0). Calls generated extension methods in `Program.cs`. Custom Inertia middleware bridges Blazor SSR → React.

### Frontend (React + Inertia.js)

- **ClientApp** (`template/SimpleModule.Host/ClientApp/app.tsx`) — Inertia bootstrap. Resolves pages by splitting route name (e.g., `Products/Browse` → imports `/_content/Products/Products.pages.js`).
- **Module pages** — Each module builds its React pages via Vite in library mode → `{ModuleName}.pages.js` in module's `wwwroot/`. Entry point: `Pages/index.ts` exporting a `pages` record mapping route names to components.
- **Type generation** — `[Dto]` types → source generator embeds TS interfaces → `tools/extract-ts-types.mjs` writes `.ts` files to `ClientApp/types/`.

### Request Flow

1. ASP.NET route handler calls `Inertia.Render("Products/Browse", props)`
2. Inertia middleware renders Blazor SSR shell with JSON props
3. React ClientApp dynamically imports module's `pages.js` bundle
4. Component hydrates with server-provided props

## Key Constraints

- **Source generator must target netstandard2.0** with `IIncrementalGenerator` (not `ISourceGenerator`).
- Modules need `<FrameworkReference Include="Microsoft.AspNetCore.App" />`.
- **Module Vite builds use library mode** — externalize React, React-DOM, @inertiajs/react. Inline dynamic imports.
- **TreatWarningsAsErrors is enabled** globally via `Directory.Build.props` with `AnalysisLevel=latest-all` and `AnalysisMode=All`. Suppressed rules are listed in `.editorconfig`.

## C# Conventions (enforced by .editorconfig)

- **Naming**: Interfaces `IFoo`, public members `PascalCase`, private fields `_camelCase`, locals/params `camelCase`, constants `PascalCase`.
- **Style**: File-scoped namespaces (error), usings outside namespace (error), prefer `var`.
- **Tests**: Underscore method names allowed (`Method_Scenario_Expected`). CA2234 (Uri overload) and xUnit1051 (CancellationToken) suppressed in test projects.

## Module Communication

- **Contracts pattern** — each module has a `.Contracts` project with a public interface (e.g., `IProductContracts`) and `[Dto]` types. Other modules depend on contracts, never implementations.
- **Event bus** — `IEventBus.PublishAsync<T>()` broadcasts to all `IEventHandler<T>` implementations. Handler failures are isolated (collected in `AggregateException`).

## Test Infrastructure

- **`SimpleModule.Tests.Shared`** provides `SimpleModuleWebApplicationFactory` — in-memory SQLite, test auth scheme with `CreateAuthenticatedClient(params Claim[] claims)`, claims passed via `X-Test-Claims` header.
- **`FakeDataGenerators`** (Bogus) — pre-built fakers for all module DTOs and request types.
- CI runs tests against both SQLite and PostgreSQL.

## Frontend Packages (`packages/`)

- **@simplemodule/client** — Vite plugin for vendoring, page resolution utility.
- **@simplemodule/ui** — Radix UI component wrappers with Tailwind. Import components from `@simplemodule/ui/components`, utils from `@simplemodule/ui/lib/utils`.
- **@simplemodule/theme-default** — Tailwind CSS base theme.

## CLI (`sm` command)

```bash
sm new project              # scaffold new SimpleModule solution
sm new module <name>        # create module with contracts, endpoints, tests, events
sm new feature <name>       # add feature to existing module
sm doctor [--fix]           # validate project structure, auto-fix issues
```

## Database

- Multi-provider: SQLite (table prefixes), PostgreSQL/SQL Server (schemas per module).
- Each module registers `ModuleDbContextInfo` for schema isolation.
- Uses `EnsureCreated()` — for production migrations, use EF Core migrations per module.

## Adding a New Module

Use `sm new module <name>` (CLI) or manually:

1. Create `modules/<Name>/`
2. Create `modules/<Name>/src/<Name>.Contracts/` with:
   - `<Name>.Contracts.csproj` (references Core only, `Microsoft.NET.Sdk`)
   - `I<Name>Contracts.cs` — public interface for cross-module use
   - Shared DTO types marked with `[Dto]`
3. Create `modules/<Name>/src/<Name>/` with:
   - `<Name>.csproj` (references Core + Contracts; `Microsoft.NET.Sdk` with `<FrameworkReference Include="Microsoft.AspNetCore.App" />`)
   - `<Name>Module.cs` — implements `IModule` with `[Module("Name", RoutePrefix = "...")]`
   - `Endpoints/<Name>/` — endpoint classes implementing `IEndpoint` (auto-discovered)
   - `Pages/index.ts` — exports `pages` record mapping route names to React components
   - `vite.config.ts` — library mode build targeting `Pages/index.ts`
   - `package.json` — declare React/Inertia as peerDependencies
   - Register contract interface in `ConfigureServices`
   - **Escape hatch**: For non-standard routes, implement `ConfigureEndpoints` on the module class
4. Create `modules/<Name>/tests/<Name>.Tests/` with xUnit test project
5. Add `ProjectReference` to `template/SimpleModule.Host/SimpleModule.Host.csproj`
6. Add all projects to `SimpleModule.slnx`

## Minimal API Parameter Binding

Reference: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis?view=aspnetcore-10.0

### Binding Rules

- **Simple types** (`int`, `string`, etc.): Route → Query → Header (implicit)
- **Complex types** (classes/records): Body (JSON) for POST/PUT/DELETE, Query for GET (implicit)
- **DI services**: Auto-injected when registered in the container
- **Special types**: `HttpContext`, `HttpRequest`, `HttpResponse`, `CancellationToken`, `ClaimsPrincipal` are auto-bound

### When to Use Attributes

- `[FromForm]` — **required** for form data binding (never implicit)
- `[FromBody]` — explicit body binding (usually implicit for complex types in POST/PUT)
- `[FromQuery]` — when a parameter name conflicts with a route parameter
- `[FromRoute]` — when disambiguation is needed
- `[FromHeader(Name = "X-Header")]` — for HTTP headers
- `[FromServices]` — rarely needed (DI services are auto-detected)
- `[AsParameters]` — bind a complex type from multiple sources (route + query + header)

### Correct Patterns

```csharp
// API: complex type auto-binds from JSON body, service auto-injected
app.MapPost("/", async (CreateProductRequest request, IProductContracts products) => ...);

// API: route param + body + DI
app.MapPut("/{id}", async (int id, UpdateProductRequest request, IProductContracts products) => ...);

// View: form data requires [FromForm]
app.MapPost("/", async ([FromForm] string name, [FromForm] decimal price, IProductContracts products) => ...);
```

### Anti-patterns (Avoid)

```csharp
// BAD: manual form reading via HttpContext
app.MapPost("/", async (HttpContext context, IService svc) => {
    var form = await context.Request.ReadFormAsync(); // Don't do this
    var name = form["name"].ToString();
});

// BAD: manual JSON deserialization
app.MapPost("/", async (HttpContext context, IService svc) => {
    var body = await JsonSerializer.DeserializeAsync<MyType>(context.Request.Body); // Don't do this
});
```

## Linting & Formatting

Biome is configured at repo root (`biome.json`). Covers `modules/**`, `packages/**`, `template/**` except `**/wwwroot/**`. Settings: single quotes, semicolons always, 2-space indent, trailing commas, 100-char line width. Tailwind CSS directives enabled.
