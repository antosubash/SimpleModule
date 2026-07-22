# SimpleModule

> **Experimental** — This project is vibe coded and under active development. APIs, conventions, and structure may change without notice. Use at your own risk.

A modular monolith framework for .NET 10 that uses Roslyn source generators to discover and wire up modules at compile time — no reflection, no manual registration. The frontend is React 19 + Inertia.js, rendered server-side through a static HTML shell and hydrated on the client.

## What it does

- **Compile-time module discovery** — A Roslyn `IIncrementalGenerator` scans referenced assemblies for `[Module]` classes, `IEndpoint`/`IViewEndpoint` implementors, and `[Dto]` types. It emits `AddModules()`, `MapModuleEndpoints()`, JSON serializer contexts, and TypeScript interface definitions. No startup reflection.
- **Module isolation** — Each module has its own database schema (or table prefix on SQLite), its own contracts project, and its own React page bundle. Modules communicate through contract interfaces and an async event bus — never by referencing each other's internals.
- **Full-stack type safety** — `[Dto]`-decorated C# types generate TypeScript interfaces that the React frontend imports. The source generator keeps both sides in sync automatically.
- **Inertia.js bridge** — Endpoints call `Inertia.Render("Module/Page", props)`. The static HTML shell delivers initial HTML with JSON props, then React hydrates client-side. No separate API layer needed for page rendering.
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

#### Seed credentials

Two accounts are seeded on first start, and the login page's quick-login buttons use these exact
credentials:

| Account | Default password | Override with |
|---------|------------------|---------------|
| `admin@simplemodule.dev` | `Admin123!` | `Seed__AdminPassword` |
| `user@simplemodule.dev` | `User123!` | `Seed__UserPassword` |

The defaults apply in every environment so the app boots and is usable out of the box. Outside
`Development` and `Testing` the app logs a warning when it falls back to them.

> **These passwords are published in this repository.** Before exposing a deployment to anyone else,
> set `Seed__AdminPassword` (or change the password after first login) and turn off the
> **Show Test Accounts** setting, which renders the quick-login buttons and defaults to on.

```bash
docker build -t simplemodule .
docker run -e Seed__AdminPassword='<strong-password>' -p 8080:8080 simplemodule
```

`Seed__AdminPassword` is the environment-variable spelling of the `Seed:AdminPassword` configuration
key — use the latter in `appsettings.*.json`. Accounts are seeded only when they don't already
exist, so changing these values later will **not** rotate an existing password; change it from the
account page instead.

> **Upgrading an existing deployment.** Previously the demo `user@simplemodule.dev` account was
> skipped outside `Development` and `Testing` when `Seed__UserPassword` was unset, so deployments that set only
> `Seed__AdminPassword` have no such account today. They will gain one, seeded with `User123!`, on
> the next restart. Set `Seed__UserPassword`, or delete the account after it appears, if you don't
> want it. The admin account is unaffected — it already exists, and existing accounts are never
> re-seeded or rotated.

Note that `Production` additionally requires OpenIddict signing and encryption certificates
(`OpenIddict__SigningCertificatePath` / `OpenIddict__EncryptionCertificatePath`, PKCS#12), and
refuses to start without them.

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
  SimpleModule.Core            # IModule, IEndpoint, IViewEndpoint, [Dto], [Module], events, menus
  SimpleModule.Generator       # Roslyn IIncrementalGenerator (netstandard2.0)
  SimpleModule.Database        # Multi-provider DB (SQLite, PostgreSQL, SQL Server)
  SimpleModule.Hosting         # ASP.NET host builder extensions, Inertia page rendering
  SimpleModule.Storage(.Local/.Azure/.S3)  # File storage abstraction + provider implementations
  SimpleModule.Testing         # Shared testing utilities for module test projects
modules/
  Admin, AuditLogs, BackgroundJobs, Branding, Dashboard, Email, FeatureFlags,
  FileStorage, Identity, Keycloak, Localization, Notifications,
  RateLimiting, Settings, Tenants, Users
  OpenIddict                   # OpenID Connect / OAuth 2.0 via OpenIddict
  Permissions                  # RBAC and access control
packages/
  SimpleModule.Client          # Vite plugin + page resolution for module frontends
  SimpleModule.UI              # Radix UI component library with Tailwind
  SimpleModule.Theme.Default   # Tailwind CSS base theme
  SimpleModule.Echo            # Real-time client for the broadcasting hub (wraps SignalR)
  SimpleModule.TsConfig        # Shared TypeScript configuration
template/
  SimpleModule.Host            # Host app (net10.0) — wires modules via generated code
cli/
  SimpleModule.Cli             # `sm` CLI tool for scaffolding and validation
tools/
  SimpleModule.DevTools        # Developer tooling and diagnostics
tests/                         # Framework tests, shared test infrastructure, and e2e tests
scripts/                       # Build/dev orchestrators, type extraction, component scaffolding
```

### Request Flow

```
Browser request
  → ASP.NET route handler calls Inertia.Render("Tenants/Browse", { tenants })
  → Inertia middleware renders static HTML shell with JSON props
  → Client loads React, dynamically imports module's Tenants.pages.js bundle
  → React component hydrates with server-provided props
```

Subsequent navigations are XHR — Inertia fetches JSON props and swaps the React component client-side without a full page reload.

### Module Anatomy

Each module follows the same structure. Using the Tenants module as an example:

```
modules/Tenants/
  src/
    SimpleModule.Tenants.Contracts/   # Public interface + DTOs (what other modules depend on)
      ITenantContracts.cs             # Service interface
      Tenant.cs                       # [Dto] — generates TypeScript types
      TenantId.cs                     # Strongly-typed ID
      CreateTenantRequest.cs          # Request DTO
    SimpleModule.Tenants/             # Implementation (never referenced by other modules)
      TenantsModule.cs                # [Module("Tenants")] + IModule — DI, menus, permissions
      TenantsDbContext.cs             # Module-scoped EF Core context
      TenantService.cs                # Implements ITenantContracts
      Endpoints/Tenants/              # IEndpoint classes (auto-discovered, auto-mapped)
      Pages/                          # IViewEndpoint classes + co-located React .tsx pages
      Pages/index.ts                  # Page registry — maps route names to lazy component imports
  tests/
    SimpleModule.Tenants.Tests/       # xUnit tests for this module
```

### Module Communication

- **Contracts** — Modules expose a `.Contracts` project with a public interface (e.g., `ITenantContracts`) and `[Dto]` types. Other modules depend on contract interfaces, never on implementation projects.
- **Event bus** — Modules publish domain events via Wolverine's `IMessageBus.PublishAsync<T>()`. Handlers are discovered by naming convention (a class ending in `Handler`/`Consumer` with a `Handle`/`Consume` method) and dispatched in-process through a durable inbox/outbox — no manual registration needed.

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

## Acknowledgments

SimpleModule is built with many excellent open-source projects. Key dependencies include:

**Backend** — [ASP.NET Core](https://dotnet.microsoft.com/), [Entity Framework Core](https://github.com/dotnet/efcore), [Roslyn](https://github.com/dotnet/roslyn) (source generators), [OpenIddict](https://github.com/openiddict/openiddict-core), [OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet), [.NET Aspire](https://github.com/dotnet/aspire), [Npgsql](https://github.com/npgsql/efcore.pg), [Swashbuckle](https://github.com/domaindrivendev/Swashbuckle.AspNetCore), [Vogen](https://github.com/SteveDunn/Vogen), [Spectre.Console](https://github.com/spectreconsole/spectre.console), [Azure SDK for .NET](https://github.com/Azure/azure-sdk-for-net), [AWS SDK for .NET](https://github.com/aws/aws-sdk-net)

**Frontend** — [React](https://github.com/facebook/react), [Inertia.js](https://github.com/inertiajs/inertia), [Vite](https://github.com/vitejs/vite), [Tailwind CSS](https://github.com/tailwindlabs/tailwindcss), [Radix UI](https://github.com/radix-ui/primitives), [TypeScript](https://github.com/microsoft/TypeScript), [Recharts](https://github.com/recharts/recharts), [Puck Editor](https://github.com/measuredco/puck), [Biome](https://github.com/biomejs/biome)

**Testing** — [xUnit.v3](https://github.com/xunit/xunit), [FluentAssertions](https://github.com/fluentassertions/fluentassertions), [NSubstitute](https://github.com/nsubstitute/NSubstitute), [Bogus](https://github.com/bchavez/Bogus), [Playwright](https://github.com/microsoft/playwright), [Faker.js](https://github.com/faker-js/faker)

For a complete list with descriptions and licenses, see [docs/site/reference/acknowledgments.md](docs/site/reference/acknowledgments.md).
