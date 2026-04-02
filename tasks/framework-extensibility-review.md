# Framework Extensibility & Architecture Review

> Deep review of SimpleModule's modularity, extensibility, and architectural soundness.
> Extends the findings in `tasks/module-design-review.md` with additional depth.

---

## Executive Summary

SimpleModule's source generator is genuinely impressive — 6-line `Program.cs`, compile-time module discovery, automatic contract wiring, TypeScript generation, and unified DbContext creation. The framework delivers on "convention over configuration" for the happy path.

However, the framework has **extensibility bottlenecks** that will compound as the module count grows. The issues fall into three categories:

1. **Closed extension points** — enums, hardcoded lifetimes, and static calls that cannot be overridden
2. **Generator scalability** — non-incremental compilation, namespace heuristics, missing source locations on diagnostics
3. **Coupling at boundaries** — Inertia serialization bypass, unified DbContext, sequential event dispatch

---

## Critical Architectural Issues

### 1. Source Generator Is Not Truly Incremental

**Files:** `framework/SimpleModule.Generator/ModuleDiscovererGenerator.cs`, `Discovery/SymbolDiscovery.cs`

The generator uses `CompilationProvider.Select()` which fires on **every** compilation change. `SymbolDiscovery.Extract()` performs a full recursive walk of all types in all referenced assemblies on every keystroke-triggered recompile.

```csharp
var dataProvider = context.CompilationProvider.Select(
    static (compilation, _) => SymbolDiscovery.Extract(compilation)
);
```

The correct incremental pattern would use `SyntaxProvider.ForAttributeWithMetadataName()` to react only when `[Module]`, `[Dto]`, or `[ViewPage]` attributes change. The `DiscoveryData.Equals()` implementation provides downstream caching (emitters skip if data is unchanged), but the expensive discovery traversal itself always runs.

**Impact:** With 10-15 modules and hundreds of types, this is O(total types across all assemblies) per compile. Developers will notice degraded IDE responsiveness as the solution grows.

**Recommendation:** Refactor the pipeline to use attribute-based `SyntaxProvider` filtering. Scan only assemblies that contain `[Module]`-attributed types (detectable via assembly-level attributes or a marker).

---

### 2. IModule Is a Wide Interface (ISP Violation)

**File:** `framework/SimpleModule.Core/IModule.cs`

`IModule` conflates 9 concerns into one interface: services, routing, middleware, menu, permissions, settings, startup, shutdown, and health. Default interface methods mitigate boilerplate, but this design has real costs:

- **Adding a new lifecycle hook** requires adding a method to `IModule`, a detection boolean to `ModuleInfoRecord` in the generator, and a call site in an emitter. This is a cross-cutting change across three layers for what should be a single extension.
- **Modules silently ignore new hooks.** When a 10th method is added to `IModule`, existing compiled modules just get the default no-op. There is no signal that a module should consider the new hook.
- **The generator must track which methods each module overrides** via `DeclaresMethod()` reflection, creating tight coupling between the interface shape and the generator's code.

**Recommendation:** Consider decomposing into focused interfaces (`IModuleServices`, `IModuleMiddleware`, `IModuleMenu`, etc.) that modules opt into. The generator already tracks which methods are overridden — this would formalize it. Alternatively, use a registration-based pattern where modules register capabilities rather than implementing a mega-interface.

---

### 3. All Diagnostics Lack Source Locations

**File:** `framework/SimpleModule.Generator/Emitters/DiagnosticEmitter.cs`

All 30+ SM diagnostics (SM0001-SM0044) use `Location.None`:

```csharp
context.ReportDiagnostic(Diagnostic.Create(descriptor, Location.None, ...));
```

This means IDE error squiggles never appear on the offending line. All errors show as project-level messages in the Error List. The generator **has** the `INamedTypeSymbol` at discovery time and could attach `symbol.Locations.FirstOrDefault()` for source-defined types.

**Impact:** Developers must manually search for the problematic type/endpoint when a diagnostic fires. For SM0015 (duplicate view page name) or SM0033 (duplicate permission), this can be genuinely difficult with 10+ modules.

**Recommendation:** Attach `Location` to every diagnostic where the originating symbol has syntax references. For metadata-only symbols (from compiled assemblies), `Location.None` is unavoidable, but most module code is source-referenced during development.

---

### 4. Module-to-Endpoint Ownership Uses a Fragile Namespace Heuristic

**File:** `framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs` — `FindClosestModuleName()`

Endpoint classes are assigned to modules by **longest namespace prefix match**. If no prefix matches, the fallback is `modules[0].ModuleName` — the first discovered module, which is non-deterministic.

**Scenarios that break:**
- Two modules with overlapping namespace prefixes (e.g., `SimpleModule.Products` and `SimpleModule.ProductReviews`) — endpoints in `SimpleModule.ProductReviews.Endpoints` could match `Products` if `ProductReviews` isn't discovered yet.
- An endpoint in an unconventional namespace silently falls to the first module with no diagnostic.

**Recommendation:** Require endpoint classes to be in or under the module's declared namespace, and emit a diagnostic when the heuristic falls back to `modules[0]`.

---

### 5. Contract Implementations Are Hardcoded to Scoped Lifetime

**File:** `framework/SimpleModule.Generator/Emitters/ModuleExtensionsEmitter.cs`

The generator always emits:
```csharp
services.AddScoped<IFooContracts, FooService>();
```

There is no attribute or convention to request `Singleton` or `Transient` lifetime. A module author who needs a singleton service (e.g., a cache, a connection pool wrapper) must bypass auto-discovery entirely and register manually in `ConfigureServices`.

**Impact:** This forces a choice: use auto-discovery with scoped lifetime, or opt out completely. There's no middle ground.

**Recommendation:** Support a `[ContractLifetime(ServiceLifetime.Singleton)]` attribute on the implementation class that the generator reads.

---

### 6. Inertia Serialization Bypasses DI-Registered JSON Options

**Files:** `framework/SimpleModule.Core/Inertia/InertiaResult.cs`

`InertiaResult` creates its own private `JsonSerializerOptions` with camelCase naming:

```csharp
private static readonly JsonSerializerOptions _camelCaseOptions = new()
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    // ...
};
```

This **does not** include the `ModulesJsonResolver` registered by `AddModules()`. Consequences:
- Custom JSON converters (e.g., Vogen value object converters) registered in `ConfigureHttpJsonOptions` do not apply to Inertia responses.
- A DTO serialized via an API endpoint and via an Inertia view endpoint can produce different JSON.
- The comment explains this is intentional for Vogen unwrapping consistency, but it creates a split serialization path.

**Recommendation:** Resolve `JsonSerializerOptions` from DI (via `IOptions<JsonOptions>`) and merge the camelCase policy, rather than maintaining a separate static instance.

---

### 7. MenuSection Is a Closed Enum

**File:** `framework/SimpleModule.Core/Menu/MenuSection.cs`

```csharp
public enum MenuSection { Navbar, UserDropdown, AdminSidebar, AppSidebar }
```

Adding a new section (e.g., `Footer`, `SettingsSidebar`, `MobileNav`) requires modifying the Core enum — a breaking change for all compiled modules.

Additionally, `MenuItem.RequiresAuth` is a boolean, not a policy reference. There's no way to tie menu visibility to a permission check — the UI must manually filter based on user claims.

**Recommendation:** Replace the enum with a string-based section key or an open `MenuSection` record type. Add an optional `RequiredPermission` string to `MenuItem` that the framework evaluates at render time.

---

### 8. InertiaSharedData Has No Namespace Isolation

**File:** `framework/SimpleModule.Core/Inertia/InertiaSharedData.cs`

`InertiaSharedData` is a scoped key-value store (`Dictionary<string, object?>`) that any middleware or endpoint can write to. Two modules setting the same key (e.g., `"notifications"`, `"user"`) silently overwrite each other.

**Recommendation:** Namespace shared data keys by module name, or provide a typed API (`SetSharedData<TModule>(key, value)`) that prevents collisions.

---

### 9. Event Bus Has No Middleware/Pipeline Support

**Files:** `framework/SimpleModule.Core/Events/EventBus.cs`, `BackgroundEventDispatcher.cs`

The event bus dispatches directly to handlers with no interception points. There's no way to add:
- Logging/tracing around all event handlers
- Retry policies for failed handlers
- Metrics collection
- Transaction boundaries

The AuditLogs module works around this by **decorating** `IEventBus` itself (`AuditingEventBus`), but this is a one-off pattern — there's no composable pipeline.

Additionally, `BackgroundEventChannel` uses an **unbounded channel** with no backpressure. Under load, memory grows without limit. Background dispatch swallows all exceptions (logged only).

**Recommendation:** Add an `IEventMiddleware` or `IEventPipelineBehavior<T>` pattern (similar to MediatR's pipeline). Consider bounded channels with configurable capacity.

---

### 10. No Conditional Module Registration

**File:** `framework/SimpleModule.Generator/Emitters/ModuleExtensionsEmitter.cs`

All discovered modules are always registered. There is no feature flag, environment check, or configuration mechanism to exclude a module at runtime. The only way to remove a module is to remove its `ProjectReference`.

**Impact:** Cannot disable a module for specific environments (e.g., disable `Admin` in production, disable `AuditLogs` in development). Cannot do A/B testing of module implementations.

**Recommendation:** Support a module enable/disable configuration (e.g., `SimpleModule:Modules:AuditLogs:Enabled = false`) that the generated `AddModules()` checks before calling `ConfigureServices`.

---

## Significant Design Concerns

### 11. Test Infrastructure Requires Manual Updates Per Module

**File:** `tests/SimpleModule.Tests.Shared/Fixtures/SimpleModuleWebApplicationFactory.cs`

The test factory explicitly lists every module's `DbContext` for SQLite replacement. Adding a new module requires manually adding `ReplaceDbContext<NewModuleDbContext>()` and `EnsureCreated()` calls. This is the only part of the framework without auto-discovery.

**Recommendation:** The generator could emit a helper listing all module DbContext types that the test factory reads automatically.

---

### 12. GlobalExceptionHandler Is Sealed and Not Extensible

**File:** `framework/SimpleModule.Core/` (exception handling)

`GlobalExceptionHandler` maps domain exceptions to HTTP status codes via a pattern-match switch. Modules cannot contribute additional exception-to-response mappings without modifying Core. A module with a custom exception type (e.g., `RateLimitedException`) must either use one of the existing base exceptions or accept a generic 500 response.

**Recommendation:** Allow modules to register `IExceptionMapper` implementations that the handler consults before its default switch.

---

### 13. Endpoint Classes Cannot Use Constructor Injection

**Files:** `framework/SimpleModule.Core/IEndpoint.cs`, Generator `EndpointExtensionsEmitter.cs`

Endpoints are instantiated with `new EndpointClass()` — no DI involvement. Services must be injected via Minimal API parameter binding inside the `Map` method's lambda. This is consistent with ASP.NET Minimal API patterns but means:
- No shared cross-cutting logic via an injected base service
- No endpoint-level middleware or filter extension point expressible via the interface
- `[AllowAnonymous]` and `[RequirePermission]` attributes are discovered by the generator for diagnostics but **not enforced in generated code** — the endpoint must still call these methods inside `Map()`

**Recommendation:** Consider resolving endpoints from DI (`ActivatorUtilities.CreateInstance`) to enable constructor injection. Alternatively, have the generator automatically apply `[AllowAnonymous]` and `.RequirePermission()` based on discovered attributes, removing the manual step.

---

### 14. TypeScript Generation Has Gaps

**Files:** `framework/SimpleModule.Generator/Emitters/TypeScriptDefinitionsEmitter.cs`, `Helpers/TypeMappingHelpers.cs`

- **Enums map to `any`**, losing type safety in the frontend
- **Generic DTO types** produce incorrect TypeScript
- **Inheritance** is not handled — base class properties are not included
- **Module name derivation** assumes `SimpleModule.{ModuleName}.Contracts.{Type}` namespace convention — deviations produce wrong grouping
- The TypeScript is embedded inside `#if SIMPLEMODULE_TS` / `#endif` in a C# comment block, requiring an external tool (`extract-ts-types.mjs`) to parse it out

**Recommendation:** Support enum-to-union-type mapping. Handle base class property inheritance. Validate module name derivation and emit a diagnostic on mismatch.

---

### 15. Result<T> and Exceptions Coexist Without Clear Boundaries

**File:** `framework/SimpleModule.Core/` (Result type and exception types)

Core provides both `Result<T>` (functional) and domain exceptions (`NotFoundException`, `ValidationException`). There's no enforced convention about which to use when:
- `GlobalExceptionHandler` handles exceptions
- `Result<T>` must be manually unwrapped by endpoint code
- Modules mix both approaches inconsistently

**Recommendation:** Establish a clear convention: exceptions for truly exceptional failures, `Result<T>` for expected business outcomes. Consider a `Results.ToResponse()` helper that maps `Result<T>` to `IResult` automatically.

---

### 16. SettingDefinition Is Mutable

**File:** `framework/SimpleModule.Core/` (Settings)

`SettingDefinition` is a class with public setters on all properties. Any code holding a reference can mutate it after registration. Since settings definitions are collected once at startup and stored in a singleton registry, mutation after registration corrupts the global state.

**Recommendation:** Make `SettingDefinition` a record or seal it with init-only properties.

---

### 17. `ConfigureEndpoints` Escape Hatch Is All-or-Nothing

**File:** `framework/SimpleModule.Generator/Emitters/EndpointExtensionsEmitter.cs`

If a module declares `ConfigureEndpoints`, the generator skips **all** auto-registration for that module's endpoints. You cannot mix auto-registered and manually registered endpoints in the same module.

**Recommendation:** Change the escape hatch to be additive — auto-register discovered endpoints first, then call `ConfigureEndpoints` for any additional custom routes.

---

### 18. SM0038 Detection Is Too Coarse

**File:** `framework/SimpleModule.Generator/Emitters/DiagnosticEmitter.cs`

The diagnostic for "infrastructure type in Contracts assembly" checks if the FQN **contains the substring `"DbContext"`**. This false-positives on legitimate types like `DbContextSummaryDto` or `UserDbContextOptions`.

**Recommendation:** Check for inheritance from `Microsoft.EntityFrameworkCore.DbContext` rather than string matching.

---

## Summary: Priority Matrix

| Priority | Issue | Impact | Effort |
|----------|-------|--------|--------|
| **P0** | #1 Generator not incremental | Compile-time degrades with scale | High |
| **P0** | #3 Diagnostics lack source locations | Developer experience | Medium |
| **P1** | #2 IModule ISP violation | Extensibility ceiling | High |
| **P1** | #6 Inertia serialization bypass | Silent data inconsistencies | Low |
| **P1** | #5 Hardcoded Scoped lifetime | Forces manual workarounds | Low |
| **P1** | #9 Event bus no pipeline | Cross-cutting concern gap | Medium |
| **P1** | #13 Endpoints not DI-resolved | Attributes not enforced in codegen | Medium |
| **P2** | #4 Namespace heuristic fallback | Silent misassignment | Low |
| **P2** | #7 Closed MenuSection enum | Breaking change to extend | Low |
| **P2** | #10 No conditional modules | Cannot disable per environment | Low |
| **P2** | #11 Test factory manual updates | New module onboarding friction | Low |
| **P2** | #12 Exception handler sealed | Module-specific errors map to 500 | Low |
| **P2** | #14 TypeScript gaps (enums, inheritance) | Frontend type safety holes | Medium |
| **P2** | #17 ConfigureEndpoints all-or-nothing | Limits mixing strategies | Low |
| **P3** | #8 InertiaSharedData no namespacing | Key collision risk | Low |
| **P3** | #15 Result<T> vs exceptions ambiguity | Inconsistent patterns | Low |
| **P3** | #16 SettingDefinition mutable | Post-registration corruption risk | Low |
| **P3** | #18 SM0038 substring match | False positive diagnostics | Low |

---

## What the Framework Gets Right

These are genuine architectural strengths worth preserving:

1. **6-line Program.cs** — The generated `AddSimpleModule()` / `UseSimpleModule()` facade is excellent developer experience
2. **Topological module ordering** — Kahn's algorithm with cycle detection (SM0010) is robust
3. **Contract auto-registration** with validation (SM0025/SM0026/SM0028/SM0029) provides compile-time safety
4. **SM0011 cross-module reference detection** — enforces the contracts boundary pattern
5. **AuditLogs as a decorator** — demonstrates the cross-cutting concern pattern working correctly without other modules changing
6. **Schema isolation per module** — PostgreSQL schemas / SQLite prefixes per module entity
7. **The emitter pattern** (`IEmitter` array) — adding new code generation concerns is clean and extensible
8. **Vite library mode per module** — each module builds independently, externals are shared
