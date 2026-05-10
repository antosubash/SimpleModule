---
outline: deep
---

# Brownfield Integration

This guide is for teams who already have a large, production .NET application and want to adopt SimpleModule **without rewriting the existing codebase**. It walks through bringing the framework in alongside whatever you already have, then migrating features into modules at a pace that suits your team.

If you are starting a new application, follow the [Quick Start](/getting-started/quick-start) instead. The CLI scaffolds a clean solution in seconds.

## Why this is realistic

SimpleModule is composed of a few independent capabilities:

- A **Roslyn source generator** that discovers `[Module]` classes from referenced assemblies at compile time.
- A small set of **infrastructure NuGet packages** (`SimpleModule.Hosting`, `SimpleModule.Database`, `SimpleModule.Storage.*`).
- A growing set of **optional cross-cutting modules** (Users, Permissions, Settings, AuditLogs, FileStorage, FeatureFlags, Tenants, Email, BackgroundJobs, Localization, RateLimiting, OpenIddict, Admin, Dashboard).

There is **no required base class for your existing controllers, no global filter, no startup interceptor**. Adding `builder.AddSimpleModule()` is purely additive: it registers services, maps routes under module-defined prefixes, and otherwise leaves your application untouched. Your existing MVC controllers, minimal API endpoints, middleware, EF Core contexts, and authentication schemes keep working exactly as before.

This means you can integrate the framework in a single afternoon and migrate features over months.

## What "brownfield integration" looks like

A typical migration goes through four phases. You can stop at any phase — each one is a stable end-state.

| Phase | Goal | Time investment |
|-------|------|-----------------|
| **1. Coexist** | SimpleModule installed and starting up next to your existing code. No features migrated. | An afternoon |
| **2. First module** | One small, isolated bounded context (e.g. "Notifications" or "Audit") moved into a module. | A few days |
| **3. Adopt cross-cutting modules** | Replace home-grown pieces with `Permissions`, `Settings`, `AuditLogs`, `FileStorage`, etc. | Per-module, weeks |
| **4. Module-first by default** | New features are written as modules; old code is migrated opportunistically. | Ongoing |

Most teams stop at Phase 3. A full rewrite is rarely the right goal.

## Phase 1: Coexist

The objective of Phase 1 is to get SimpleModule loaded into your existing host process with **zero behavioural change**. After this phase your application starts up, every existing route still works, and `builder.AddSimpleModule()` resolves successfully.

### Prerequisites

- **.NET 10 SDK** for your host project. If your host is on an older TFM, you have two options:
  - Multi-target the host (`<TargetFrameworks>net8.0;net10.0</TargetFrameworks>`) and run on net10.0.
  - Stand up a new net10.0 host project that proxies/calls into your existing app. Many teams find this easier than retargeting a large solution at once.
- **A modern `WebApplication`-style `Program.cs`**. The framework hooks into `WebApplicationBuilder` and `WebApplication`. If you are still on `Startup.cs`, port `Program.cs` to the minimal hosting model first — it is a mechanical change.

### Step 1: Add the framework projects to your solution

You have two choices:

**Option A — Reference the framework as projects (recommended while it pre-1.0).** Clone or submodule the SimpleModule repository into your tree and add the framework `.csproj` files to your `.slnx`/`.sln`:

- `framework/SimpleModule.Core`
- `framework/SimpleModule.Database`
- `framework/SimpleModule.Generator`
- `framework/SimpleModule.Hosting`
- `framework/SimpleModule.Storage.Local` (or `.S3` / `.Azure`)

**Option B — NuGet packages** once you publish your own builds to a private feed. The `<PackageReference>` shape is identical to the `<ProjectReference>` shape below; substitute as needed.

### Step 2: Wire the host `.csproj`

Add the following to your existing host project file. The two important lines are the `Generator` reference (which **must** be marked as an Analyzer) and `SimpleModule.Hosting`:

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <!-- Required so the source generator's output is visible to design-time tools -->
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <!-- Suppress diagnostics for cross-module extension points your host doesn't implement -->
  <NoWarn>$(NoWarn);SM0025;SM0028</NoWarn>
</PropertyGroup>

<ItemGroup>
  <!-- Hosting infrastructure: middleware, exception handling, Inertia, auth defaults -->
  <ProjectReference Include="..\SimpleModule\framework\SimpleModule.Hosting\SimpleModule.Hosting.csproj" />

  <!-- The generator MUST be referenced as an analyzer -->
  <ProjectReference
    Include="..\SimpleModule\framework\SimpleModule.Generator\SimpleModule.Generator.csproj"
    OutputItemType="Analyzer"
    ReferenceOutputAssembly="false" />
</ItemGroup>

<!-- Pulls in the post-build target that copies generator outputs to wwwroot -->
<Import Project="..\SimpleModule\framework\SimpleModule.Hosting\build\SimpleModule.Hosting.targets" />
```

::: warning
`OutputItemType="Analyzer"` and `ReferenceOutputAssembly="false"` are both required. Without them the generator either doesn't run or its assembly leaks into the host's runtime references and causes load failures.
:::

### Step 3: Update `Program.cs`

Add three lines to your existing host bootstrap. Place them **after** your own service registrations but **before** `builder.Build()`:

```csharp
var builder = WebApplication.CreateBuilder(args);

// --- Your existing registrations (controllers, EF, MediatR, auth, etc.) ---
builder.Services.AddControllers();
builder.Services.AddDbContext<MyLegacyDbContext>(opts => opts.UseSqlServer(/*...*/));
builder.Services.AddAuthentication(/*...*/);

// --- SimpleModule additions ---
builder.Services.AddLocalStorage(builder.Configuration); // or AddS3Storage / AddAzureStorage
builder.AddSimpleModule();                                // generated extension method

var app = builder.Build();

// --- Your existing pipeline ---
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// --- SimpleModule middleware + endpoints ---
await app.UseSimpleModule();                              // generated extension method

await app.RunAsync();
```

Both `AddSimpleModule` and `UseSimpleModule` are emitted by the source generator. They are the only two calls you need; the generator wires every discovered module's services, endpoints, and middleware behind them.

### Step 4: Provide minimal configuration

`SimpleModule.Database` reads from a `Database` section in your `IConfiguration`:

```json
{
  "Database": {
    "DefaultConnection": "Server=localhost;Database=MyApp;Trusted_Connection=true;TrustServerCertificate=true",
    "Provider": "SqlServer"
  }
}
```

`Provider` accepts `SqlServer`, `PostgreSQL`, or `SQLite`. Per-module overrides are supported via `Database:ModuleConnections:<ModuleName>`.

::: tip Reuse your existing connection string
You can point `DefaultConnection` at the same database your legacy `DbContext` already uses. Modules will create their tables in dedicated **schemas** (SQL Server / PostgreSQL) or with **table prefixes** (SQLite), so they will not collide with your existing tables.
:::

### Step 5: Build and verify

```bash
dotnet build
dotnet run --project YourCompany.Host
```

If the build succeeds and the application starts, Phase 1 is complete. The generator has produced `AddSimpleModule()` and `UseSimpleModule()` based on **zero modules** — they are no-ops at this point. Your existing routes still work; nothing else has changed.

A useful sanity check: open `obj/Debug/net10.0/generated/SimpleModule.Generator/` in your host project. You should see `SimpleModuleExtensions.g.cs`, `ModuleExtensions.g.cs`, and `EndpointExtensions.g.cs`. These are the generated wiring files.

## Phase 2: Your first module

The point of Phase 2 is to prove the migration pattern on a small, low-risk piece of your domain. Pick something with **clear boundaries and few callers** — internal admin tools, a notifications subsystem, or a reporting dashboard are good first candidates. Avoid your core transactional flow on the first try.

### Pick a candidate

Good candidates have most of these properties:

- One bounded responsibility (sending emails, exporting reports, managing a list of products).
- A small number of database tables, ideally with no foreign keys to your core tables.
- A finite set of endpoints, ideally already grouped under a route prefix like `/api/notifications`.
- Few external callers — primarily your own UI or a single integration partner.

### Scaffold the module

You can scaffold by hand, but the CLI saves an hour of boilerplate:

```bash
dotnet tool install -g SimpleModule.Cli
sm new module Notifications --solution-path . --host YourCompany.Host
```

This creates:

```
modules/Notifications/
├── src/
│   ├── YourCompany.Notifications.Contracts/    # interfaces + DTOs other modules can reference
│   └── YourCompany.Notifications/              # IModule, endpoints, EF entities, services
└── tests/
    └── YourCompany.Notifications.Tests/
```

…and adds a `<ProjectReference>` to your host's `.csproj`. The generator picks up the new module on the next build — there is no central registry to update.

### Move logic in, not data (yet)

The cheapest migration path is to **leave the existing tables and connection alone** and have the new module call into your legacy code through an interface. This lets you cut over the API surface without touching the database.

```csharp
// In your module's ConfigureServices
public override void ConfigureServices(IServiceCollection services, IConfiguration config)
{
    // Bridge to legacy code via an interface owned by this module
    services.AddScoped<INotificationStore, LegacyNotificationStoreAdapter>();
}
```

Then implement `LegacyNotificationStoreAdapter` in your host project, where it can reach the legacy `DbContext`. When you are ready, move the data into the module's own `DbContext` and delete the adapter.

### Redirect existing callers

If old controllers exposed `/api/notifications/*`, configure your module to use the same prefix:

```csharp
[Module("Notifications", RoutePrefix = "/api/notifications")]
public sealed class NotificationsModule : IModule
{
    // ...
}
```

…and **delete the old controller**. The module's endpoints take over the route. External callers see no change.

If you cannot delete the old controller yet (perhaps it still serves a partner API you have to support unchanged), leave it in place and give the module a different prefix like `/api/v2/notifications`.

### Verify

Run `dotnet test` against the new module's test project. Run your existing integration tests. Hit a few endpoints with curl. Phase 2 is done when the migrated feature is in production behind the new module.

## Phase 3: Adopt cross-cutting modules

Most large applications carry home-grown implementations of permissions, audit logs, settings, file uploads, feature flags, and so on. The cross-cutting modules in `modules/` are designed as drop-in replacements. **Each one is independent.** You can adopt them one at a time, in any order, and you can ignore the ones you do not need.

### How to choose what to adopt

Adopt a module when **all** of these are true:

1. You currently have a custom implementation of the same concern.
2. The custom implementation has bugs, missing features, or is expensive to maintain.
3. The module's contract covers your real use cases (read its `*.Contracts` project before deciding).

Do **not** adopt a module just because it exists. If your custom feature flag system works fine, leave it.

### The cross-cutting catalogue

| Module | Replaces | When to adopt |
|--------|----------|---------------|
| `Permissions` | Custom claim-checking, role lookups, authorization handlers | You want declarative `[Authorize]`-style permission checks across modules |
| `Settings` | `appsettings.json` for runtime-tunable values, custom admin pages | Operators need to change values without a deploy |
| `AuditLogs` | Manual `_logger.LogInformation("user X did Y")` | You need queryable audit history with entity diffs |
| `FileStorage` | Direct `IWebHostEnvironment.ContentRootPath` writes, custom S3 wrappers | Multiple modules need to upload/download files |
| `FeatureFlags` | `if (config["Features:X"] == "true")` checks | You want per-user / per-tenant flag evaluation |
| `Tenants` | Custom `tenantId` filtering in repositories | You operate a multi-tenant SaaS |
| `Email` | Direct `SmtpClient` use, custom Mailkit wrappers | You want templated, queued email with retries |
| `BackgroundJobs` | Hangfire, Quartz, raw `Task.Run` | You want CRON + on-demand jobs with progress tracking |
| `Localization` | `IStringLocalizer` resource files | You want JSON-based locale files modules can ship independently |
| `RateLimiting` | ASP.NET rate limiter middleware tuned by hand | You want per-module rate-limit policies |
| `Users` | ASP.NET Identity setup glue, custom user pages | You want passkeys + a user management UI out of the box |
| `OpenIddict` | Hand-rolled JWT issuance, IdentityServer | You want a maintained OAuth2 / OIDC server |

### Pattern: parallel run

The safest way to adopt a cross-cutting module is to **run it alongside your existing implementation**, then cut over once you trust it.

Example — adopting `Permissions`:

1. Add a `<ProjectReference>` to `SimpleModule.Permissions` and `SimpleModule.Permissions.Contracts`.
2. Define your permissions in a `IModulePermissions` class. The generator picks them up at build time.
3. **Do not delete your existing authorization code yet.** Add module-level permission checks in parallel.
4. After a release cycle of dual-running, switch your existing endpoints to call `IPermissionService` and remove the legacy code.

The same pattern applies to `Settings`, `AuditLogs`, `FeatureFlags`, etc. The cost of running both for a release is small; the cost of a botched cutover is large.

## Phase 4: Module-first by default

After a few cross-cutting modules land, the team's instincts shift. New features get scaffolded as modules because the boilerplate is shorter than spinning up a new controller folder. Old code gets migrated whenever someone is already touching it for another reason.

There is no specific milestone for "Phase 4 complete." A healthy end-state is:

- 80%+ of new code lives in modules.
- The legacy host project mostly hosts infrastructure (auth, telemetry, top-level middleware) and a shrinking set of legacy controllers.
- Cross-module communication happens through `Contracts` interfaces and the event bus, not direct method calls.

## Authentication in a brownfield host

The framework does **not** require you to use OpenIddict, Identity, or any specific auth scheme. Your existing authentication setup is preserved. The only contract is that `HttpContext.User` produces an `IPrincipal` after authentication runs.

A common pattern: keep your existing JWT bearer or cookie scheme, and tell modules they can authorize via standard ASP.NET policies. Modules that need to issue tokens (e.g. `OpenIddict`) are opt-in via `<ProjectReference>` — if you do not reference them, they do not load.

::: warning Default fallback policy
`SimpleModule.Hosting` registers a fallback authorization policy of `RequireAuthenticatedUser()`. This means **every module endpoint requires authentication unless it explicitly opts out** with `.AllowAnonymous()`. This applies to module endpoints only — your existing controllers are governed by whatever fallback policy you previously configured.
:::

## Database in a brownfield host

The single biggest decision in a brownfield migration is how modules interact with your existing database.

### Recommended: shared connection, separate schemas

Point `Database:DefaultConnection` at the same database your legacy `DbContext` uses. The framework's generated `HostDbContext` places module entities in module-named schemas (SQL Server / PostgreSQL) or with prefixed table names (SQLite). Your legacy tables stay in `dbo` (or whatever schema they live in) and never collide.

This is the recommended starting point because:

- One connection string to manage.
- One transaction scope is possible if you ever need to span legacy and module data.
- Migrations from each side are independent (modules use EF Core migrations against their schema; legacy migrations target `dbo`).

### Alternative: per-module connection strings

For modules that need a separate database (perhaps for compliance, scaling, or eventual extraction into a microservice), set a per-module override:

```json
{
  "Database": {
    "DefaultConnection": "Server=...;Database=MyApp;...",
    "ModuleConnections": {
      "AuditLogs": "Server=audit-db;Database=Audit;..."
    },
    "Provider": "SqlServer"
  }
}
```

Each module can target a different database without any code changes.

### What to do with your existing `DbContext`

Leave it alone. Register it the way you always have. Modules do not require you to fold legacy entities into the framework's `HostDbContext`. The two contexts coexist.

## Frontend integration

If your existing app already has a frontend (Razor pages, MVC views, a SPA built separately), you do not have to adopt Inertia + React. Module **API endpoints** (`IEndpoint`) work without any frontend at all.

You only need the Inertia / React stack if you want to use **view endpoints** (`IViewEndpoint`) and the admin UI provided by `SimpleModule.Dashboard`, `SimpleModule.Admin`, etc. Three options:

1. **Skip the UI entirely.** Reference only modules that expose APIs. Do not reference `Dashboard`, `Admin`, or any module with `IViewEndpoint` implementations. You will not need `npm`, Vite, or `ClientApp/`.
2. **Run the framework UI on a sub-path.** Mount it under `/admin` or `/sm` and let your existing frontend keep serving the rest of the app. The default Inertia setup is a static HTML shell — easy to scope to a route.
3. **Adopt the framework UI fully.** Replace your existing frontend over time. This is a separate, much larger migration; do not bundle it with the backend integration.

For options 1 and 2 you can ignore the entire `ClientApp/`, `package.json`, and `npm run dev` workflow described in [Quick Start](/getting-started/quick-start).

## Common pitfalls

**The generator does not see my module.** Every module assembly must be reachable as a `<ProjectReference>` (or `<PackageReference>`) from the host `.csproj`. The generator scans `compilation.References` and the host assembly itself — it does not scan the filesystem. If a module project compiles fine on its own but is not referenced by the host, it will not be discovered.

**`AddSimpleModule` does not exist.** This usually means the generator is referenced incorrectly. Check that the reference includes both `OutputItemType="Analyzer"` and `ReferenceOutputAssembly="false"`. Then run `dotnet build` once — analyzers do not run on `dotnet restore` alone.

**Schema migrations conflict.** If your legacy `DbContext` and the framework's `HostDbContext` both target the default schema, EF Core will refuse to run migrations. Move legacy tables to an explicit schema (`dbo` is fine) and ensure module entities use their module schema (the framework does this automatically via `ApplyModuleSchema`).

**Auth redirects to a sign-in page that does not exist.** The Users module configures cookie redirects to `/account/login`. If you reference Users but have your own login page elsewhere, override the redirect in your `Program.cs` after `AddSimpleModule`:

```csharp
builder.Services.ConfigureApplicationCookie(opts =>
{
    opts.LoginPath = "/your/login/path";
});
```

**The fallback `RequireAuthenticatedUser` policy breaks anonymous endpoints.** Module endpoints are authenticated by default. Add `.AllowAnonymous()` per endpoint, or override the fallback policy in your host before `AddSimpleModule`.

## Where to go next

- [Modules](/guide/modules) — the `IModule` contract in detail.
- [Endpoints](/guide/endpoints) — implementing `IEndpoint` and `IViewEndpoint`.
- [Database](/guide/database) — schema isolation, migrations, multi-provider support.
- [Source Generator](/advanced/source-generator) — what the generator actually emits.
