# Module Constitution Design Spec

**Date:** 2026-03-27
**Status:** Draft
**Output:** `docs/CONSTITUTION.md`

## Goal

Create a single authoritative document — the Constitution — that codifies all rules for building modules and contributing to the framework. Designed for a small team with a single deployment unit and shared database.

After writing, CLAUDE.md will be trimmed to reference the Constitution for architectural rules, keeping only build commands, tooling, and AI-assistant instructions.

## Audience

- Developers building modules on top of SimpleModule
- Contributors to the framework itself (Core, Generator, Database)

## Key Assumption

This is a small team deploying everything as one unit. Modules exist for code clarity and ownership, not for independent deployment or microservice extraction.

## Document Structure

### 1. Founding Principles

The preamble — assumptions everything else builds on:

- **Small team, single deployment** — all modules deploy together. One migration history, one release, one rollback.
- **Shared database** — all modules share one database via the unified HostDbContext. Schema/table-prefix isolation is for cleanliness, not independence. Deliberate choice, not a limitation.
- **Modular for clarity, not for microservices** — boundaries organize code, enforce ownership, prevent spaghetti. Not for independent scaling. The contracts pattern makes future extraction possible but don't design for it now.
- **Compile-time safety over runtime discipline** — if a rule can be enforced by the source generator (SM diagnostics), it should be. Don't rely on code review for what the compiler can catch.
- **Convention over configuration** — modules follow predictable patterns so the codebase reads like one person wrote it.

### 2. Module Boundaries

**What a module owns:**
- Its implementation assembly (`SimpleModule.{Name}`) — entities, services, DbContext, endpoints, views, event handlers
- Its contracts assembly (`SimpleModule.{Name}.Contracts`) — the public API surface: contract interface, DTOs, request/response types, value objects, events, permissions
- Its frontend assets — React components, Vite config, page registry
- Its menu items — registered via `ConfigureMenu`

**What a module exposes:**
- Only what's in Contracts. Everything else is internal.
- One primary contract interface (`I{Name}Contracts`)
- DTOs and request types marked with `[Dto]` (auto-generates TypeScript types)
- Value object IDs via Vogen with EF Core converters (Vogen is an allowed dependency in Contracts projects)
- Events implementing `IEvent`
- Permission constants via `IModulePermissions`

**Module lifecycle hooks (all optional, all have default no-ops):**
- `ConfigureServices` — register DI services, DbContext, hosted services
- `ConfigureEndpoints` — escape hatch for non-standard routes (standard endpoints are auto-discovered)
- `ConfigureMiddleware` — register ASP.NET middleware
- `ConfigureMenu` — register navigation menu items
- `ConfigurePermissions` — register authorization permissions
- `ConfigureSettings` — register runtime-configurable settings
- `OnStartAsync` — one-time initialization after all services are registered
- `OnStopAsync` — graceful shutdown cleanup
- `CheckHealthAsync` — report module health status (Healthy, Degraded, Unhealthy)

**What a module must never expose:**
- Entity classes
- DbContext or DbSet types
- Internal services
- EF Core configurations

**Structural rules:**
- Every module has both a Contracts project and an implementation project
- The module class is decorated with `[Module]` and implements `IModule`
- Module name, route prefix, and view prefix should be defined in a constants class (convention, not enforced by diagnostic)
- One module, one DbContext (if data is needed)
- SM0043 warns if a module class overrides nothing

### 3. Dependencies

**Allowed:**
- Module → own Contracts project
- Module → other module's Contracts project (never implementation)
- Module → framework projects (Core, Database)
- Contracts → Core only (plus Vogen for value objects)

**Forbidden:**
- Module → another module's implementation (SM0011, error)
- Circular contract references (SM0010, error)
- Contracts → Database or any framework project beyond Core

**Resolving circular dependencies:**
- Use the event bus: B publishes an event, A handles it. No reference needed.
- Extract shared concepts into a third Contracts project, or rethink ownership.

**Dependency direction principle:**
- Dependencies flow one way: implementation → contracts → core
- Never sideways (implementation → implementation)
- Never backwards (contracts → implementation)

**DI injection rules:**
- Inject contract interfaces, never concrete services
- Framework services (`IEventBus`, `ISettingsContracts`) are injected directly
- DbContext is injected only within the owning module's implementation

### 4. Data Ownership

**Each module owns its data exclusively:**
- Entities live in the implementation assembly, never in Contracts
- Only the owning module's service layer may read or write its entities
- Other modules access data through the contract interface

**DbContext rules:**
- Register with `AddModuleDbContext<TContext>(configuration, ModuleName)`
- Call `ApplyModuleSchema()` in `OnModelCreating`
- Use entity configurations via `IEntityTypeConfiguration<T>`
- Register Vogen value object converters in `ConfigureConventions`
- One DbContext per module, maximum

**Entity configuration:**
- One configuration per entity (SM0007 prevents duplicates)
- No entity in two modules' DbSets with different types (SM0001)
- Orphaned configurations warned (SM0006)

**Schema isolation (automatic):**
- PostgreSQL/SQL Server: schema per module (lowercase name)
- SQLite: table prefix per module
- Cosmetic organization, not security — all modules share one connection

**Forbidden:**
- Injecting HostDbContext in module code
- Raw SQL referencing another module's tables
- Foreign keys across module boundaries — use IDs and validate via contracts
- Sharing entity types between modules — use DTOs in Contracts for shared shapes

### 5. Communication

**Contract interfaces:**
- One primary interface per module: `I{Name}Contracts`
- The only way other modules interact with your data or behavior
- SM0012 warns at 15+ methods, SM0013 errors at 20+
- Exactly one implementation required (SM0025/SM0026), public and non-abstract (SM0028/SM0029)

**Events:**
- Cross-module notifications use `IEventBus.PublishAsync<T>()`
- Events defined in the publishing module's Contracts project
- Any module can handle any event via `IEventHandler<T>`
- Handlers currently execute sequentially in registration order, all run even if some fail. Design handlers as order-independent for forward compatibility.
- Failures collected into `AggregateException`
- `PublishAsync<T>()` — synchronous dispatch, caller blocks until all handlers complete, exceptions propagate
- `PublishInBackground<T>()` — fire-and-forget, failures logged not thrown. Use for notifications where the caller must not block or fail (audit logging, cache invalidation).
- Handlers should be stateless, independent, and idempotent

**When to use which:**
- Contracts — caller needs a response
- Events — caller doesn't care who listens

**Forbidden:**
- Direct service injection across modules (only contract interfaces)
- Shared mutable state
- Database-level integration (triggers, views, cross-module queries)

### 6. Endpoints

**Two types:**
- `IEndpoint` — API endpoints returning JSON
- `IViewEndpoint` — view endpoints returning `Inertia.Render()`

**Routing:**
- API endpoints under `RoutePrefix`, view endpoints under `ViewPrefix`
- Standard REST conventions: GET for reads, POST for creates, PUT for updates, DELETE for deletes. Use `CrudEndpoints` helpers for consistent status codes (200, 201, 204, 404).
- View page names must match module name prefix (SM0041)
- No duplicate view page names (SM0015)
- Modules with view endpoints must define ViewPrefix (SM0042)

**Parameter binding:**
- Implicit binding by default — don't manually read request body or form data for scalars
- `[FromForm]` required for form submissions
- `ReadFormAsync()` only for multi-value form fields
- `[FromServices]` is unnecessary noise

**Authorization:**
- `.RequirePermission()` with permission constants
- `.AllowAnonymous()` for public endpoints
- Default is authorized

**CrudEndpoints helper:**
- Standard CRUD responses with consistent status codes
- For non-standard responses, write custom handlers

**Escape hatch:**
- Override `ConfigureEndpoints` on the module class for non-standard routes

### 7. Frontend

**Page registry:**
- Every `IViewEndpoint` must have a matching entry in `Pages/index.ts`
- Key must match the component name in `Inertia.Render()`
- Missing entries cause silent client-side 404s
- Run `npm run validate-pages` to verify

**Build system:**
- Each module has a `vite.config.ts` using `defineModuleConfig()`
- Output: `{ModuleName}.pages.js` in `wwwroot/`
- Library mode — React, React-DOM, @inertiajs/react externalized
- Dynamic imports within module builds follow Rollup's default behavior (code splitting supported)
- Dynamic imports between modules are how the system works — ClientApp lazy-loads each module's `pages.js` bundle

**TypeScript types:**
- `[Dto]` types auto-generate TypeScript interfaces
- Don't hand-write types that mirror DTOs

**Package structure:**
- React and Inertia as `peerDependencies`
- Use `@simplemodule/ui` for shared components
- Use `@simplemodule/theme-default` for styling

**Linting and formatting:**
- Biome configured at repo root
- Single quotes, semicolons, 2-space indent, trailing commas, 100-char width
- `npm run check` to verify, `npm run check:fix` to auto-fix

### 8. Permissions & Authorization

**Defining permissions:**
- Sealed class implementing `IModulePermissions`
- `public const string` fields only (SM0027)
- Values follow `{ModuleName}.{Action}` (SM0031, SM0034)
- Class must be sealed (SM0032)
- No duplicate values (SM0033)

**Registering:**
- Override `ConfigurePermissions`, call `builder.AddPermissions<T>()`

**Applying:**
- `.RequirePermission()` on endpoints
- `.AllowAnonymous()` for public endpoints
- Never hardcode permission strings

**Rules:**
- Permissions owned by the defining module
- Other modules may reference permission constants from Contracts
- Not every module needs permissions

### 9. Settings & Configuration

**Defining settings:**
- Override `ConfigureSettings` on the module class

**Scopes:**
- `System` — global settings that affect infrastructure (database, caching, external service config)
- `Application` — application-wide preferences visible to all users (site title, default language)
- `User` — per-user preferences (theme, notification settings)

**Types:** Text, Number, Bool, Json

**Rules:**
- Namespace setting keys to the module
- Only for values configurable at runtime
- For per-environment values, use `IConfiguration` and `appsettings.json`
- Access other modules' settings through `ISettingsContracts`

### 10. Testing

**Stack:** xUnit.v3, FluentAssertions, Bogus, `SimpleModuleWebApplicationFactory`

**Structure:**
- Test project per module at `modules/{Name}/tests/{Name}.Tests/`
- Underscore method naming: `Method_Scenario_Expected`

**Integration tests:**
- `SimpleModuleWebApplicationFactory` with in-memory SQLite and test auth
- `CreateAuthenticatedClient(params Claim[] claims)` for auth

**Fake data:**
- `FakeDataGenerators` for pre-built Bogus fakers

**Database:**
- SQLite in-memory locally, PostgreSQL in CI
- Both providers tested in CI

**Rules:**
- Test through public API (endpoints and contract interfaces)
- Don't mock the database
- Test cross-module interactions through contracts
- Every module should have tests

### 11. Compiler-Enforced Rules

Reference table of all SM diagnostics:

**Database & Entities:**
- SM0001 (Error): No duplicate DbSet property names across modules
- SM0003 (Error): Only one IdentityDbContext
- SM0005 (Error): IdentityDbContext must use three type arguments
- SM0006 (Warning): Orphaned entity configuration
- SM0007 (Error): No duplicate entity configurations

**Module Structure:**
- SM0002 (Warning): Module name must not be empty
- SM0040 (Error): No duplicate module names
- SM0043 (Warning): Module must override at least one IModule method

**Dependencies:**
- SM0010 (Error): No circular module dependencies
- SM0011 (Error): No direct module-to-module implementation references

**Contracts:**
- SM0012 (Warning): Contract interface 15+ methods
- SM0013 (Error): Contract interface 20+ methods
- SM0014 (Error): Contracts assembly has no public interfaces
- SM0025 (Error): No implementation for contract interface
- SM0026 (Error): Multiple implementations of contract interface
- SM0028 (Error): Implementation must be public
- SM0029 (Error): Implementation must not be abstract
- SM0035 (Warning): DTO with no public properties
- SM0038 (Warning): Infrastructure type in Contracts

**Permissions:**
- SM0027 (Error): Permission field must be const string
- SM0031 (Warning): Permission value naming pattern
- SM0032 (Error): Permission class must be sealed
- SM0033 (Error): No duplicate permission values
- SM0034 (Warning): Permission prefix must match module

**View Endpoints:**
- SM0015 (Error): No duplicate view page names
- SM0041 (Warning): View page name prefix
- SM0042 (Error): ViewPrefix required with view endpoints

**Interceptors:**
- SM0039 (Warning): Interceptor has transitive DbContext dependency

### 12. Framework Contributor Guidelines

**Source generator:**
- Target netstandard2.0 with `IIncrementalGenerator`
- Use incremental pipeline for caching
- New diagnostics use the next available SM number after the highest existing one
- All diagnostics must have tests

**Adding a diagnostic:**
- Define descriptor in `DiagnosticEmitter.cs`
- Add detection logic in discovery or emission
- Add positive and negative test cases
- Document in Constitution Section 11
- Errors for runtime breakage, warnings for code smells

**HostDbContext generation:**
- Generator merges all module DbSets into one context
- Entity-to-module mapping drives schema isolation
- Test against all modules before merging changes

**Database framework:**
- `ModuleDbContextOptionsBuilder` handles provider detection and routing
- Interceptors resolved lazily to avoid circular DI
- `ApplyModuleSchema` must handle PostgreSQL, SQL Server, and SQLite

**Core framework:**
- All IModule methods must have default implementations
- New lifecycle hooks require a default no-op
- Keep Contracts dependencies minimal

**Build and CI:**
- `TreatWarningsAsErrors` enabled globally
- `AnalysisLevel=latest-all`, `AnalysisMode=All`
- Suppressed rules live in `.editorconfig`
- Tests run against SQLite and PostgreSQL in CI

## CLAUDE.md Changes

After writing the Constitution, CLAUDE.md will be updated to:

1. Remove these sections (moved to Constitution):
   - Module Communication
   - Event Handler Patterns & Exception Isolation
   - EF Core Interceptor DI Patterns
   - Minimal API Parameter Binding
   - Database (detailed rules)
   - Unified HostDbContext (reframed from "Known Limitation" to "deliberate choice" in Constitution)

2. Replace with a brief reference:
   - Link to `docs/CONSTITUTION.md` for architectural rules
   - Keep: build commands, test commands, frontend commands, CLI usage
   - Keep: Adding a New Module checklist (procedural, not architectural)
   - Keep: C# conventions (enforced by .editorconfig)
   - Keep: Key Constraints (generator netstandard2.0, TreatWarningsAsErrors)
   - Keep: Test Infrastructure (factory usage)
   - Keep: Frontend Packages
   - Keep: Linting & Formatting
   - Keep: Task Management and Core Principles (AI workflow)

## Out of Scope

- Code examples in the Constitution (reference codebase instead)
- Tutorial/walkthrough content (this is a rules document)
- Future microservice patterns (small team, single deploy)
