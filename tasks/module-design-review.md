# Module Design Review

## Critical Issues

### 1. Three Modules Violate the Contracts Pattern (Direct Implementation References)

The framework enforces that modules should only depend on other modules through their `.Contracts` projects. The source generator even has diagnostic `SM0011` to catch this. However, three modules directly reference `SimpleModule.Users` (the implementation) instead of `SimpleModule.Users.Contracts`:

| Module | References | Should Reference |
|--------|-----------|-----------------|
| **Admin** | `SimpleModule.Users.csproj` | `SimpleModule.Users.Contracts.csproj` |
| **OpenIddict** | `SimpleModule.Users.csproj` | `SimpleModule.Users.Contracts.csproj` |
| **Permissions** | `SimpleModule.Users.csproj` | `SimpleModule.Users.Contracts.csproj` |

**Why this happens:** All three modules need `ApplicationUser` and/or `ApplicationRole` (Identity entities defined in the Users implementation project) to use `UserManager<ApplicationUser>`, `RoleManager<ApplicationRole>`, and `SignInManager<ApplicationUser>`.

**Why it matters:**
- Defeats module isolation. Changing Users internals can break Admin, OpenIddict, and Permissions at compile time.
- Makes it impossible to swap the Users module implementation without touching 3 other modules.
- The source generator's SM0011 diagnostic should flag this, but these projects may be suppressing it or the generator isn't catching ProjectReference-based violations.

**Root cause:** `ApplicationUser` and `ApplicationRole` are implementation details of the Users module but are treated as shared types. They should be in the contracts layer or in a shared identity abstractions project.

**Fix options:**
- Move `ApplicationUser`, `ApplicationRole`, and Identity type registrations to `SimpleModule.Users.Contracts` (or a new `SimpleModule.Identity.Contracts` project)
- Create an `IIdentityContracts` interface that wraps the UserManager/RoleManager operations these modules need, eliminating the need for direct Identity type access

---

### 2. `IEndpoint` and `IViewEndpoint` Are Identical Interfaces With No Semantic Enforcement

Both interfaces have the exact same signature:

```csharp
public interface IEndpoint { void Map(IEndpointRouteBuilder app); }
public interface IViewEndpoint { void Map(IEndpointRouteBuilder app); }
```

The distinction exists only for the source generator to route-group them differently. But nothing prevents:
- An `IViewEndpoint` that returns JSON instead of calling `Inertia.Render()`
- An `IEndpoint` that calls `Inertia.Render()` accidentally

Since the `Pages/index.ts` registry must match `IViewEndpoint` routes (and missing entries silently 404 on the client), this is error-prone. Consider:
- Adding a compile-time analyzer that verifies `IViewEndpoint` implementations call `Inertia.Render()`
- Or adding a `string Component { get; }` property to `IViewEndpoint` so the generator can validate page registry entries

---

### 3. Event Bus Has No Async/Background Dispatch Option

The `EventBus.PublishAsync` executes all handlers **sequentially in the request pipeline**. Every handler adds latency to the HTTP response. The CLAUDE.md documents this and recommends "use background jobs for expensive operations," but:

- There's no built-in mechanism for fire-and-forget events
- The AuditLogs module works around this by decorating `IEventBus` with `AuditingEventBus` that writes to a Channel, then a background `AuditWriterService` drains it
- Other modules that need async dispatch must reinvent this pattern

Consider adding `IEventBus.PublishInBackgroundAsync<T>()` or a `[BackgroundHandler]` attribute that the framework queues automatically.

---

## Significant Design Concerns

### 4. Admin Module is a "God Module" With Mixed Responsibilities

The Admin module:
- Manages users (CRUD via `UserManager<ApplicationUser>`)
- Manages roles (CRUD via `RoleManager<ApplicationRole>`)
- Has its own `AdminDbContext` for activity tracking
- References the Users implementation directly
- References Permissions contracts

This module duplicates user/role management that arguably belongs in the Users and Permissions modules respectively. The Admin module should be a **UI aggregator** (composing views from other modules' contracts) rather than reimplementing user management with direct Identity access.

---

### 5. Unified HostDbContext Creates Tight Coupling at the Database Level

All module entities are merged into a single `HostDbContext` by the source generator. While this simplifies migrations, it means:

- **All modules share one database connection and one migration history.** A schema change in Products blocks Orders deployments.
- **Only one module can own IdentityDbContext** (SM0003 enforces this). This is a hard architectural constraint that limits identity extensibility.
- **Schema isolation is provider-dependent.** SQLite uses table prefixes, PostgreSQL uses schemas. The abstraction leaks when writing raw SQL or cross-module queries.
- **No path to independent module databases.** The modular monolith can't evolve toward microservices without replacing the entire data layer.

For a framework that emphasizes module independence, the database is the strongest coupling point.

---

### 6. Contract Interface Design Inconsistencies

**Varying abstraction levels across contracts:**

| Contract | Methods | Abstraction Level |
|----------|---------|-------------------|
| `IUserContracts` | 6 | DTO-based, clean |
| `IProductContracts` | 5 | DTO-based, clean |
| `IOrderContracts` | 5 | DTO-based, clean |
| `IPageBuilderContracts` | 5 | DTO-based, clean |
| `IPageBuilderTemplateContracts` | 4 | Separate interface, good |
| `IPageBuilderTagContracts` | 3 | Separate interface, good |
| `IPermissionContracts` | 8+ | Mixes role management with permission queries |
| `IAuditLogContracts` | 5+ | Includes export/stats alongside CRUD |

The PageBuilder module does well by splitting into three focused interfaces. Other modules should follow this pattern rather than growing a single interface.

---

### 7. No Module Lifecycle or Health Reporting

`IModule` has configuration hooks but no runtime lifecycle:
- No `OnStartAsync()` / `OnStopAsync()` for graceful startup/shutdown
- No `HealthCheckAsync()` for module-level health (the framework has app-level health checks via ASP.NET, but modules can't report their own status)
- No way for a module to declare "I'm degraded but still serving requests"

Modules like OpenIddict (certificate loading), AuditLogs (background writer), and FileStorage (external storage provider) would benefit from lifecycle hooks.

---

### 8. Permission System Has No Hierarchical Support

Permissions are flat strings (`"Products.View"`, `"Products.Create"`). There's no:
- Wildcard support (`"Products.*"` grants all Products permissions)
- Hierarchical inheritance (`"Admin"` implies all sub-permissions)
- Permission grouping beyond the module name prefix

The `PermissionAuthorizationHandler` only checks exact string matches against claims (with an Admin role bypass). As the number of modules grows, managing individual permissions per role becomes unwieldy.

---

### 9. Inertia Version Strategy is Fragile

```csharp
// InertiaMiddleware.cs
private static readonly string CacheBuster = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
```

The Inertia version is set once at app startup. In a multi-instance deployment:
- Each instance gets a different version (different startup times)
- Requests load-balanced between instances will trigger constant 409 Conflict responses
- The `DEPLOYMENT_VERSION` env var override exists but isn't enforced or documented prominently

---

## Minor Issues

### 10. Dashboard Module Has No Contracts Project

Dashboard is the only module without a contracts project. If other modules ever need to contribute dashboard widgets or data, there's no extension point. This is fine for now but breaks the pattern.

### 11. Orders.Contracts References Other Contracts

`Orders.Contracts` depends on both `Users.Contracts` and `Products.Contracts`. This means consuming the Orders contract transitively pulls in two other contracts. For a contracts layer meant to be lightweight, this creates a dependency chain. Consider using primitive types (IDs as `Guid` or value objects) in contract DTOs rather than referencing other module contracts.

### 12. No Standardized Error/Result Types in Contracts

Modules use exceptions (`NotFoundException`, `ValidationException`) for error signaling. The `IOrderContracts.CreateOrderAsync()` throws if the user doesn't exist instead of returning a result type. This forces all consumers to catch exceptions for expected business logic failures, mixing control flow with error handling.

### 13. `IModule` Uses Default Interface Methods (C# 8+)

All methods on `IModule` have empty default implementations. While convenient, this means:
- A module can implement `IModule` and do nothing (the generator still discovers it)
- No compile-time signal when a new hook is added to `IModule` - existing modules silently ignore it
- Static analysis tools can't determine which hooks a module uses without reading the implementation

### 14. Source Generator Threshold for Contract Size is Hardcoded

SM0012 warns at 15 methods, SM0013 errors at 20 methods. The diagnostic message claims these are "configurable in .editorconfig" but the thresholds appear hardcoded in the generator. This should either be actually configurable or the message should be corrected.

---

## Summary

The framework has a strong foundation: compile-time discovery, contracts pattern enforcement, and clean separation of concerns. The most impactful issues to address are:

1. **Fix the Users implementation leak** (issues #1 and #4) - this is the single biggest modularity violation
2. **Add IViewEndpoint validation** (issue #2) - prevents silent client-side failures
3. **Consider database decoupling strategy** (issue #5) - the current unified DbContext is the tightest coupling
4. **Add background event dispatch** (issue #3) - prevents modules from reinventing the AuditLogs pattern
