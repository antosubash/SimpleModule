# Module Constitution

The authoritative rules for building modules on SimpleModule and contributing to the framework itself. This document governs architectural decisions, module boundaries, data ownership, communication patterns, and compiler-enforced constraints.

**Audience:** Developers building modules and contributors to the framework (Core, Generator, Database).

**Assumption:** This is a small team deploying everything as one unit. Modules exist for code clarity and ownership, not for independent deployment or microservice extraction.

---

## 1. Founding Principles

- **Small team, single deployment.** All modules deploy together. One migration history, one release, one rollback.
- **Shared database.** All modules share one database via the unified HostDbContext. Schema and table-prefix isolation is for cleanliness, not independence. This is a deliberate design choice that simplifies operations and enables cross-module queries when needed.
- **Modular for clarity, not for microservices.** Boundaries organize code, enforce ownership, and prevent spaghetti. They are not for independent scaling. The contracts pattern makes future extraction possible, but do not design for it now.
- **Compile-time safety over runtime discipline.** If a rule can be enforced by the source generator (SM diagnostics), it must be. Do not rely on code review for what the compiler can catch.
- **Convention over configuration.** Modules follow predictable patterns so the codebase reads like one person wrote it.

---

## 2. Module Boundaries

### What a Module Owns

- Its **implementation assembly** (`SimpleModule.{Name}`) -- entities, services, DbContext, endpoints, views, event handlers
- Its **contracts assembly** (`SimpleModule.{Name}.Contracts`) -- the public API surface: contract interface, DTOs, request/response types, value objects, events, permissions
- Its **frontend assets** -- React components, Vite config, page registry
- Its **menu items** -- registered via `ConfigureMenu`

### What a Module Exposes

Only what is in Contracts. Everything else is internal.

- One primary contract interface (`I{Name}Contracts`)
- DTOs and request types marked with `[Dto]` (auto-generates TypeScript types)
- Value object IDs via Vogen with EF Core converters (Vogen is an allowed dependency in Contracts projects)
- Events implementing `IEvent`
- Permission constants via `IModulePermissions` (place in Contracts if other modules need to reference them)

### Module Lifecycle Hooks

All hooks are optional. All have default no-op implementations.

1. **ConfigureServices** -- register DI services, DbContext, hosted services
2. **ConfigureEndpoints** -- escape hatch for non-standard routes (standard endpoints are auto-discovered)
3. **ConfigureMiddleware** -- register ASP.NET middleware
4. **ConfigureMenu** -- register navigation menu items
5. **ConfigurePermissions** -- register authorization permissions
6. **ConfigureSettings** -- register runtime-configurable settings
7. **ConfigureFeatureFlags** -- register feature flag definitions
8. **ConfigureAgents** -- register AI agent definitions
9. **ConfigureRateLimits** -- register rate limit policies
10. **ConfigureHost** -- configure host-level integrations (e.g., TickerQ, database initialization) after the host is built
11. **OnStartAsync** -- one-time initialization after all services are registered
12. **OnStopAsync** -- graceful shutdown cleanup
13. **CheckHealthAsync** -- report module health status (Healthy, Degraded, Unhealthy)

### Module Options

Modules can expose configurable behavior via the `IModuleOptions` marker interface. The source generator auto-discovers these classes and generates typed `Configure{Module}()` extension methods on `SimpleModuleOptions`.

```csharp
// Module defines options
public class ProductsModuleOptions : IModuleOptions
{
    public int DefaultPageSize { get; set; } = 10;
    public int MaxPageSize { get; set; } = 100;
}

// Host app configures them
builder.AddSimpleModule(o =>
{
    o.ConfigureProducts(p => p.MaxPageSize = 50);
});

// Module reads them via IOptions<T>
public class BrowseEndpoint(IOptions<ProductsModuleOptions> options) : IViewEndpoint { ... }
```

**Rules:**
- At most one `IModuleOptions` class per module (SM0044 warns on duplicates).
- Options classes may live in the module assembly or its Contracts assembly.
### What a Module Must Never Expose

- Entity classes
- DbContext or DbSet types
- Internal services
- EF Core configurations

### Structural Rules

- Every module has both a Contracts project and an implementation project.
- The module class is decorated with `[Module]` and implements `IModule`.
- Module name, route prefix, and view prefix should be defined in a constants class (convention, not enforced by diagnostic).
- One module, one DbContext (if data is needed).
- SM0043 warns if a module class overrides no lifecycle hooks (indicating a likely empty or placeholder module).

---

## 3. Dependencies

### Allowed

- Module implementation --> own Contracts project
- Module implementation --> other modules' Contracts projects (never their implementations)
- Module implementation --> framework projects (Core, Database)
- Contracts --> Core only (plus Vogen for value objects)

### Forbidden

- Module --> another module's implementation (SM0011, error)
- Circular contract references (SM0010, error)
- Contracts --> Database or any framework project beyond Core

### Dependency Direction Principle

Dependencies flow one way: **implementation --> contracts --> core**. Never sideways (implementation --> implementation). Never backwards (contracts --> implementation).

### Resolving Circular Dependencies

- Use the event bus: module B publishes an event, module A handles it. No reference needed.
- Extract shared concepts into a third Contracts project, or rethink ownership.

### DI Injection Rules

- Inject contract interfaces, never concrete services.
- Framework services (`IEventBus`, `ISettingsContracts`) are injected directly.
- DbContext is injected only within the owning module's implementation.

---

## 4. Data Ownership

### Each Module Owns Its Data Exclusively

- Entities live in the implementation assembly, never in Contracts.
- Only the owning module's service layer may read or write its entities.
- Other modules access data through the contract interface.

### DbContext Rules

- Register with `AddModuleDbContext<TContext>(configuration, ModuleName)`.
- Call `ApplyModuleSchema()` in `OnModelCreating`.
- Use entity configurations via `IEntityTypeConfiguration<T>`.
- Register Vogen value object converters in `ConfigureConventions`.
- One DbContext per module, maximum.

### Entity Configuration

- One configuration per entity (SM0007 prevents duplicates).
- No entity in two modules' DbSets with different types (SM0001).
- Orphaned configurations warned (SM0006).

### Schema Isolation

Schema isolation is automatic and provider-dependent:

- **PostgreSQL / SQL Server:** schema per module (lowercase name)
- **SQLite:** table prefix per module

This is cosmetic organization -- all modules share one connection.

### Forbidden

- Injecting HostDbContext in module code
- Raw SQL referencing another module's tables
- Foreign keys across module boundaries -- use IDs and validate via contracts
- Sharing entity types between modules -- use DTOs in Contracts for shared shapes

---

## 5. Communication

### Contract Interfaces

- One primary interface per module: `I{Name}Contracts`
- The only way other modules interact with your data or behavior
- SM0012 warns at 15+ methods, SM0013 errors at 20+
- Exactly one implementation required (SM0025/SM0026), public and non-abstract (SM0028/SM0029)

### Events

- Cross-module notifications use `IEventBus.PublishAsync<T>()`
- Events are defined in the publishing module's Contracts project
- Any module can handle any event via `IEventHandler<T>`
- Handlers currently execute sequentially in registration order; all run even if some fail. Design handlers as order-independent for forward compatibility.
- Failures are collected into `AggregateException`
- Handlers should be stateless, independent, and idempotent

### PublishAsync vs PublishInBackground

- **`PublishAsync<T>()`** -- synchronous dispatch. The caller blocks until all handlers complete. Exceptions propagate to the caller.
- **`PublishInBackground<T>()`** -- fire-and-forget. Failures are logged, not thrown. No cancellation token is accepted, so callers cannot cancel in-flight background work, and handlers may be interrupted on host shutdown. Use for notifications where the caller must not block or fail (audit logging, cache invalidation).

### When to Use Which Communication Pattern

- **Contracts** -- caller needs a response
- **Events** -- caller does not care who listens

### Forbidden

- Direct service injection across modules (only contract interfaces)
- Shared mutable state
- Database-level integration (triggers, views, cross-module queries)

---

## 6. Endpoints

### Two Types

- **`IEndpoint`** -- API endpoints returning JSON
- **`IViewEndpoint`** -- view endpoints returning `Inertia.Render()`

### Routing

- API endpoints live under `RoutePrefix`, view endpoints under `ViewPrefix`.
- View page names must match the module name prefix (SM0041).
- No duplicate view page names (SM0015).
- Modules with view endpoints must define `ViewPrefix` (SM0042).

### REST Conventions

- **GET** for reads
- **POST** for creates
- **PUT** for updates
- **DELETE** for deletes
- Use `CrudEndpoints` helpers for consistent status codes: 200 (OK), 201 (Created), 204 (No Content), 404 (Not Found)
- For non-standard responses, write custom handlers.

### Parameter Binding

- Implicit binding by default -- do not manually read request body or form data for scalars.
- `[FromForm]` is required for form submissions.
- `ReadFormAsync()` only for multi-value form fields (arrays from repeated keys).
- `[FromServices]` is unnecessary noise; DI services are injected automatically.

### Authorization

- `.RequirePermission()` with permission constants.
- `.AllowAnonymous()` for public endpoints.
- Default is authorized.

### Escape Hatch

Override `ConfigureEndpoints` on the module class for non-standard routes.

---

## 7. Frontend

### Page Registry

- Every `IViewEndpoint` must have a matching entry in `Pages/index.ts`.
- The key must match the component name passed to `Inertia.Render()`.
- Missing entries cause silent client-side 404s with no error in the console.
- Run `npm run validate-pages` to verify all endpoints have matching page entries.

### Dynamic Imports

- **Within a module build:** Dynamic imports follow Rollup's default behavior (code splitting is supported within the module's bundle).
- **Between modules:** ClientApp lazy-loads each module's `pages.js` bundle at runtime. This is how the system works -- each module is a separate entry point.

### Build System

- Each module has a `vite.config.ts` using `defineModuleConfig()`.
- Output: `{ModuleName}.pages.js` in `wwwroot/`.
- Library mode -- React, React-DOM, and @inertiajs/react are externalized.

### TypeScript Types

- `[Dto]` types auto-generate TypeScript interfaces via the source generator.
- Do not hand-write types that mirror DTOs.

### Package Structure

- React and Inertia declared as `peerDependencies`.
- Use `@simplemodule/ui` for shared components.
- Use `@simplemodule/theme-default` for styling.

### Linting and Formatting

- Biome configured at repo root.
- Single quotes, semicolons, 2-space indent, trailing commas, 100-char line width.
- `npm run check` to verify, `npm run check:fix` to auto-fix.

---

## 8. Permissions & Authorization

### Defining Permissions

- Sealed class implementing `IModulePermissions` (SM0032 enforces sealed).
- `public const string` fields only (SM0027).
- Values follow the pattern `{ModuleName}.{Action}` (SM0031, SM0034).
- No duplicate values (SM0033).

### Registering Permissions

- Override `ConfigurePermissions` on the module class and call `builder.AddPermissions<T>()`.

### Applying Permissions

- `.RequirePermission()` on endpoints.
- `.AllowAnonymous()` for public endpoints.
- Never hardcode permission strings -- always use the permission constants.

### Rules

- Permissions are owned by the defining module.
- Other modules may reference permission constants from Contracts.
- Not every module needs permissions.

---

## 9. Settings & Configuration

### Defining Settings

Override `ConfigureSettings` on the module class.

### Scopes

| Scope | Purpose |
|-------|---------|
| **System** | Global settings that affect infrastructure (database, caching, external service configuration) |
| **Application** | Application-wide preferences visible to all users (site title, default language) |
| **User** | Per-user preferences (theme, notification settings) |

### Types

| Type | Description |
|------|-------------|
| **Text** | String value (single-line) |
| **Number** | Numeric value (integer or decimal) |
| **Bool** | Boolean (true/false) |
| **Json** | Arbitrary JSON value (for complex or structured settings) |

### Rules

- Namespace setting keys to the module.
- Settings are only for values configurable at runtime.
- For per-environment values, use `IConfiguration` and `appsettings.json`.
- Access other modules' settings through `ISettingsContracts`.

---

## 10. Testing

### Stack

xUnit.v3, FluentAssertions, Bogus, `SimpleModuleWebApplicationFactory`.

### Structure

- Test project per module at `modules/{Name}/tests/{Name}.Tests/`.
- Underscore method naming: `Method_Scenario_Expected`.

### Integration Tests

- `SimpleModuleWebApplicationFactory` provides in-memory SQLite and a test auth scheme.
- `CreateAuthenticatedClient(params Claim[] claims)` for authenticated requests.

### Fake Data

- `FakeDataGenerators` provides pre-built Bogus fakers for all module DTOs and request types.

### Database

- SQLite in-memory locally, PostgreSQL in CI.
- Both providers tested in CI.

### Rules

- Test through public API (endpoints and contract interfaces).
- Do not mock the database.
- Test cross-module interactions through contracts.
- Every module should have tests.

---

## 11. Compiler-Enforced Rules

All SM diagnostics are emitted by the Roslyn source generator at compile time. `TreatWarningsAsErrors` is enabled globally, so warnings are effectively errors unless suppressed in `.editorconfig`.

### Database & Entities

| Diagnostic | Severity | Rule |
|------------|----------|------|
| SM0001 | Error | No duplicate DbSet property names across modules |
| SM0003 | Error | Only one IdentityDbContext allowed |
| SM0005 | Error | IdentityDbContext must use three type arguments |
| SM0006 | Warning | Orphaned entity configuration (not referenced by any DbSet) |
| SM0007 | Error | No duplicate entity configurations |

### Module Structure

| Diagnostic | Severity | Rule |
|------------|----------|------|
| SM0002 | Warning | Module name must not be empty |
| SM0040 | Error | No duplicate module names |
| SM0043 | Warning | Module must override at least one IModule method |
| SM0044 | Warning | Multiple IModuleOptions for same module |

### Dependencies

| Diagnostic | Severity | Rule |
|------------|----------|------|
| SM0010 | Error | No circular module dependencies |
| SM0011 | Error | No direct module-to-module implementation references |

### Contracts

| Diagnostic | Severity | Rule |
|------------|----------|------|
| SM0012 | Warning | Contract interface has 15+ methods (consider splitting) |
| SM0013 | Error | Contract interface has 20+ methods (must split) |
| SM0014 | Error | Contracts assembly has no public interfaces |
| SM0025 | Error | No implementation found for contract interface |
| SM0026 | Error | Multiple implementations of contract interface |
| SM0028 | Error | Contract implementation must be public |
| SM0029 | Error | Contract implementation must not be abstract |
| SM0035 | Warning | DTO with no public properties |
| SM0038 | Warning | Infrastructure type found in Contracts assembly |

### Permissions

| Diagnostic | Severity | Rule |
|------------|----------|------|
| SM0027 | Error | Permission field must be const string |
| SM0031 | Warning | Permission value must follow the `Module.Action` pattern (exactly one dot) |
| SM0032 | Error | Permission class must be sealed |
| SM0033 | Error | No duplicate permission values |
| SM0034 | Warning | Permission value prefix must match the owning module name |

### View Endpoints

| Diagnostic | Severity | Rule |
|------------|----------|------|
| SM0015 | Error | No duplicate view page names |
| SM0041 | Warning | View page name must be prefixed with module name |
| SM0042 | Error | ViewPrefix required when module has view endpoints |

### Interceptors

| Diagnostic | Severity | Rule |
|------------|----------|------|
| SM0039 | Warning | Interceptor has transitive DbContext dependency (resolve at interception time) |

### Feature Flags

| Diagnostic | Severity | Rule |
|------------|----------|------|
| SM0045 | Error | Feature class must be sealed |
| SM0046 | Warning | Feature field must follow `ModuleName.FeatureName` pattern |
| SM0047 | Error | No duplicate feature names across modules |
| SM0048 | Error | Feature field must be a public const string |

### Endpoints

| Diagnostic | Severity | Rule |
|------------|----------|------|
| SM0049 | Error | Each endpoint must be in its own file |
| SM0054 | Info | Endpoint should declare a `public const string Route` field |

### Module Metadata

| Diagnostic | Severity | Rule |
|------------|----------|------|
| SM0052 | Error | Module assembly name must follow `SimpleModule.{ModuleName}` convention |
| SM0053 | Error | Module must have a matching `SimpleModule.{ModuleName}.Contracts` assembly |

---

## 12. Framework Contributor Guidelines

### Source Generator

- Target netstandard2.0 with `IIncrementalGenerator`.
- Use incremental pipeline for caching.
- New diagnostics use the next available SM number after the highest existing one.
- All diagnostics must have tests.

### Adding a Diagnostic

1. Define the descriptor in `DiagnosticEmitter.cs`.
2. Add detection logic in discovery or emission.
3. Add positive and negative test cases.
4. Document in this Constitution, Section 11.
5. Use Error severity for runtime breakage, Warning for code smells.

### HostDbContext Generation

- The generator merges all module DbSets into one context.
- Entity-to-module mapping drives schema isolation.
- Test against all modules before merging changes.

### Database Framework

- `ModuleDbContextOptionsBuilder` handles provider detection and routing.
- Interceptors are resolved lazily to avoid circular DI.
- `ApplyModuleSchema` must handle PostgreSQL, SQL Server, and SQLite.

### Database Migrations

- One migration history shared by all modules. Run `dotnet ef migrations add <Name> --project template/SimpleModule.Host` to create a migration.
- The unified `HostDbContext` (source-generated) owns all DbSets across modules. Migrations target this context.
- When two modules add migrations concurrently, resolve conflicts by regenerating the later migration against the merged model snapshot.
- SQLite uses table prefixes (`{ModuleName}_`) for logical isolation. PostgreSQL and SQL Server use schema isolation (`{ModuleName}.`).
- Never modify or delete existing migrations that have been applied in production. Add corrective migrations instead.

### Logging Conventions

- Inject `ILogger<T>` via primary constructor. Use the module's service class as the type parameter.
- Use source-generated logging via `[LoggerMessage]` attribute for all log messages. This is required by the `partial class` pattern and produces high-performance, zero-allocation log calls.
- **Log levels**: `Debug` for lifecycle events (module started/stopped). `Information` for successful operations (entity created/updated/deleted). `Warning` for expected failures (not found, validation). `Error` for unexpected failures (exceptions, infrastructure).
- **Structured fields**: Always include entity IDs and names as named parameters (e.g., `{ProductId}`, `{ProductName}`). The runtime logging infrastructure adds correlation IDs via `System.Diagnostics.Activity.Current.TraceId`.
- Do not log sensitive data (passwords, tokens, PII). The AuditLogs module handles redaction for audit trails separately.

### Core Framework

- All `IModule` methods must have default implementations.
- New lifecycle hooks require a default no-op.
- Keep Contracts dependencies minimal.

### Build and CI

- `TreatWarningsAsErrors` is enabled globally via `Directory.Build.props`.
- `AnalysisLevel=latest-all`, `AnalysisMode=All`.
- Suppressed rules live in `.editorconfig`.
- Tests run against both SQLite and PostgreSQL in CI.
