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
npm run dev                          # start development (dotnet + all module watches, unminified JS)
npm run dev:build                    # build all modules in dev mode (unminified, with source maps)
npm run build                        # production build (minified, optimized)
npm run check                        # biome lint + format check
npm run check:fix                    # auto-fix lint + formatting
npm run lint                         # lint only
npm run format                       # format only (with write)
```

### Development Workflow

Run `npm run dev` to start the complete development environment:

```bash
npm run dev
# Starts:
# - dotnet run (ASP.NET backend on https://localhost:5001)
# - npm watch for all modules (unminified, with source maps)
# - npm watch for ClientApp (unminified, with source maps)
```

The orchestrator will coordinate all processes:
- **Edit a module file** → Vite rebuilds (fast, unminified, readable code)
- **Edit ClientApp** → Vite rebuilds (fast, unminified)
- **Browser refresh** → See changes immediately
- **Browser dev tools** → See original TypeScript thanks to source maps
- **Ctrl+C** → Gracefully stops all processes

### Build Modes

- **Development (`npm run build:dev`)** — unminified, source maps enabled, for local iteration
- **Production (`npm run build`)** — minified, optimized, for distribution and NuGet packages

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

### Event Handler Patterns & Exception Isolation

The EventBus implements **partial success semantics**: all handlers execute sequentially in registration order, even if some fail. Exceptions are collected and rethrown as `AggregateException` after all handlers complete.

**Handler Execution Guarantees:**

- All registered `IEventHandler<T>` implementations execute for each event
- Handlers run in registration order
- If handler A throws, handler B still runs
- Side effects from successful handlers are preserved
- All exceptions are collected and thrown together as `AggregateException`

**Handler Implementation Best Practices:**

- **Stateless**: Avoid storing mutable state; handlers may be called concurrently in future versions
- **Independent**: Don't rely on side effects from other handlers; they may execute in any order or be skipped
- **No throwing for expected failures**: Use result types or early returns instead; thrown exceptions interrupt and must be handled by the caller
- **Idempotent when possible**: The same event may be reprocessed in retry scenarios; design handlers to handle duplicate calls gracefully
- **Avoid long-running work**: For expensive operations, use background jobs or a reliable message queue instead of synchronous handlers

**Exception Handling Pattern:**

```csharp
public sealed class AuditLogEventHandler : IEventHandler<OrderCreatedEvent>
{
    public async Task HandleAsync(OrderCreatedEvent @event, CancellationToken cancellationToken)
    {
        try
        {
            // Log the event
            await _logger.LogEventAsync(@event, cancellationToken);
        }
        catch (Exception ex)
        {
            // Don't throw: audit logging must never disrupt the primary operation
            _logger.LogError(ex, "Failed to log event");
            // Optionally: queue for retry, alert, etc.
        }
    }
}
```

**Testing Partial Failure:**
Use `EventBusPartialFailureTests` as a reference. Key scenarios:

- Verify successful handlers complete their work even if others fail
- Verify exceptions are collected and rethrown as `AggregateException`
- Verify handler execution order is preserved even when some fail
- Verify cancellation tokens propagate to all handlers

## Pages Registry Pattern (Pages/index.ts)

When you add a new `IViewEndpoint`, you **must** register it in your module's `Pages/index.ts` immediately. This is a manual, critical step.

**Why:** The C# source generator discovers your new endpoint and validates it's properly decorated, but React needs a corresponding entry in the page registry. If you forget:

- The endpoint compiles and runs fine on the server
- Navigating to that page in React silently 404s client-side (no error in console, no error response shown to user)
- The developer won't know for hours or until QA finds it

**Pattern:**

```typescript
// modules/Products/src/Products/Pages/index.ts
export const pages: Record<string, any> = {
    "Products/Browse": () => import("../Views/Browse"),
    "Products/Manage": () => import("../Views/Manage"),
    "Products/Create": () => import("../Views/Create"),
};
```

**The Rule:** For every `IViewEndpoint` with `Inertia.Render("Products/Something", ...)`, add a matching entry in `pages`. The component name in Inertia.Render (e.g., `"Products/Manage"`) is your key.

**Validation:** After adding endpoints, run:

```bash
npm run validate-pages
```

This script checks that all C# endpoints have corresponding TypeScript entries. If mismatches are found, it logs them and exits with error code 1 (useful for CI).

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

### EF Core Interceptor DI Patterns

**Problem:** SaveChangesInterceptors registered in DI can cause circular dependencies when they depend on services that themselves depend on DbContext. For example: `SaveChangesInterceptor → ISettings → SettingsService → SettingsDbContext` creates a deadlock during construction.

**Solution:** Resolve runtime dependencies at interception time, not in the constructor.

**Pattern:**

```csharp
// CORRECT: Constructor has minimal dependencies (only required for interceptor itself)
public sealed class MyInterceptor(
    IServiceProvider? serviceProvider = null  // Injected via DI, nullable for optionality
) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    )
    {
        // Resolve optional services at interception time
        var settings = serviceProvider?.GetService<ISettingsContracts>();
        if (settings is not null)
        {
            // Use the service
        }
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}

// WRONG: Constructor tries to resolve ISettingsContracts
public sealed class BadInterceptor(
    ISettingsContracts settings  // WRONG: Causes circular dependency!
) : SaveChangesInterceptor { }
```

**Key Guidelines:**

- Never inject services that transitively depend on DbContext into interceptor constructors
- Inject `IServiceProvider?` as an optional dependency if runtime service resolution is needed
- Resolve services only within `SavingChangesAsync`, `SavedChangesAsync`, or `SaveChangesFailed` methods
- The framework resolves all registered `ISaveChangesInterceptor` instances lazily during DbContext options construction

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

### 1. Plan Node Default

- Enter plan mode for ANY non-trivial task (3+ steps or architectural decisions)
- If something goes sideways, STOP and re-plan immediately - don't keep pushing
- Use plan mode for verification steps, not just building
- Write detailed specs upfront to reduce ambiguity

---

### 2. Subagent Strategy

- Use subagents liberally to keep main context window clean
- Offload research, exploration, and parallel analysis to subagents
- For complex problems, throw more compute at it via subagents
- One task per subagent for focused execution

---

### 3. Self-Improvement Loop

- After ANY correction from the user: update `tasks/lessons.md` with the pattern
- Write rules for yourself that prevent the same mistake
- Ruthlessly iterate on these lessons until mistake rate drops
- Review lessons at session start for relevant project

---

### 4. Verification Before Done

- Never mark a task complete without proving it works
- Diff behavior between main and your changes when relevant
- Ask yourself: "Would a staff engineer approve this?"
- Run tests, check logs, demonstrate correctness

---

### 5. Demand Elegance (Balanced)

- For non-trivial changes: pause and ask "is there a more elegant way?"
- If a fix feels hacky: "Knowing everything I know now, implement the elegant solution"
- Skip this for simple, obvious fixes - don't over-engineer
- Challenge your own work before presenting it

---

### 6. Autonomous Bug Fixing

- When given a bug report: just fix it. Don't ask for hand-holding
- Point at logs, errors, failing tests - then resolve them
- Zero context switching required from the user
- Go fix failing CI tests without being told how

---

## Task Management

1. **Plan First**: Write plan to `tasks/todo.md` with checkable items
2. **Verify Plan**: Check in before starting implementation
3. **Track Progress**: Mark items complete as you go
4. **Explain Changes**: High-level summary at each step
5. **Document Results**: Add review section to `tasks/todo.md`
6. **Capture Lessons**: Update `tasks/lessons.md` after corrections

---

## Core Principles

- **Simplicity First**: Make every change as simple as possible. Impact minimal code
- **No Laziness**: Find root causes. No temporary fixes. Senior developer standards
- Don't claude to the commit messages
