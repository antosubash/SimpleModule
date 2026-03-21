# Source Generator Diagnostic Opportunities

Compile-time diagnostics that could be added to the SimpleModule Roslyn source generator to catch bugs early and enforce architectural conventions.

## Existing Diagnostics (SM0001–SM0014)

Already implemented: duplicate DbSets, empty module names, multiple IdentityDbContexts, orphaned entity configs, duplicate entity configs, circular dependencies, illegal cross-module references, oversized contract interfaces, missing contract interfaces in referenced assemblies.

---

## Tier 1 — Catches Real Bugs

### SM0015: Unused Permission Constants

Permission constants defined in `{Module}Permissions.cs` but never referenced in any `.RequirePermission()` call across the module's endpoints.

- **Detection:** Scan `{Module}Permissions` classes for `public const string` fields. Cross-reference against `.RequirePermission()` calls in the module's `IEndpoint`/`IViewEndpoint` implementations.
- **Impact:** Catches security dead code, forgotten endpoints, permissions registered in the UI but gating nothing.
- **False positive risk:** Low.

### SM0016: Settings Key Mismatch

Setting key defined in `ConfigureSettings()` but never consumed, or consumed key that doesn't match any definition.

- **Detection:** Collect all `SettingDefinition.Key` string literals from `ConfigureSettings()`. Collect all `GetSettingAsync("key")` string literals across the solution. Cross-reference for orphaned definitions and undefined consumptions.
- **Impact:** A single mistyped character means silent `null` at runtime. The AuditLogs module alone has 9 string keys that could drift.
- **False positive risk:** Medium (dynamic key strings can't be tracked, but those are anti-patterns).

### SM0017: Missing Contract DI Registration

`I{Module}Contracts` interface exists in a Contracts project but no corresponding `services.AddScoped<I{Module}Contracts, ...>()` found in the module's `ConfigureServices`.

- **Detection:** Scan Contracts assemblies for public interfaces matching `I*Contracts`. Check that the owning module's `ConfigureServices` method contains a registration for each.
- **Impact:** Catches DI resolution failures at application startup.
- **False positive risk:** Low.

### SM0018: Missing [Dto] on Public Contract Types

Public class/record in a `*.Contracts` assembly that appears as an endpoint parameter or return type but is missing the `[Dto]` attribute.

- **Detection:** Scan public types in `*.Contracts` assemblies. Check endpoint method signatures for types originating from Contracts. Flag types missing `[Dto]`.
- **Impact:** No TypeScript interface gets generated, frontend breaks at runtime with missing/mismatched prop types.
- **False positive risk:** Very low.

---

## Tier 2 — Enforces Architecture

### SM0019: Orphaned Endpoint Class

Class has a `Map(IEndpointRouteBuilder)` method signature but doesn't implement `IEndpoint` or `IViewEndpoint`. The source generator never discovers it.

- **Detection:** Scan all classes in module assemblies for methods matching `void Map(IEndpointRouteBuilder)`. Flag those not implementing `IEndpoint` or `IViewEndpoint`.
- **Impact:** Route silently doesn't exist. Developer thinks endpoint is live.
- **False positive risk:** Very low.

### SM0020: Route Prefix Collision

Two modules declare identical or overlapping `RoutePrefix` values in their `[Module]` attribute.

- **Detection:** Collect all `RoutePrefix` values from `[Module]` attributes. Flag duplicates or prefixes where one is a prefix of another.
- **Impact:** Ambiguous route matches at runtime. Currently no validation exists.
- **False positive risk:** Low.

### SM0021: Vogen Conversion Flag Inconsistency

`[ValueObject<T>]` type missing required conversion flags (`SystemTextJson | EfCoreValueConverter`).

- **Detection:** Scan all types with `ValueObjectAttribute`. Extract `conversions` parameter. Flag types missing either `SystemTextJson` or `EfCoreValueConverter`.
- **Impact:** Missing `EfCoreValueConverter` causes `InvalidOperationException` when EF queries the type. Missing `SystemTextJson` breaks API serialization.
- **False positive risk:** Very low.

### SM0022: Module Attribute and Constants Drift

`[Module("Products", RoutePrefix = "/products")]` on the module class vs `ProductsConstants.RoutePrefix = "/api/products"` defined separately. These can diverge silently.

- **Detection:** For each module, compare the `[Module]` attribute's name/RoutePrefix arguments against the corresponding `{Module}Constants` class fields. Flag mismatches.
- **Impact:** Routing confusion — endpoints use Constants values but generator reads attribute values.
- **False positive risk:** Low.

---

## Tier 3 — Developer Experience

### SM0023: Settings Type Mismatch

Setting defined as `SettingType.Number` but consumed via `GetSettingAsync<bool>()`. The generic type parameter doesn't align with the definition's `Type` field.

- **Detection:** Map `SettingType` enum to expected CLR types (Number→int/double, Bool→bool, Text→string, Json→object). Cross-reference `GetSettingAsync<T>` call sites.
- **Impact:** Deserialization returns default value or throws.
- **False positive risk:** Medium.

### SM0024: Endpoint Without Authorization

`IEndpoint` implementation that has no `.RequirePermission()`, `.RequireAuthorization()`, `.RequireRole()`, or `[AllowAnonymous]` in its `Map()` method.

- **Detection:** Scan endpoint `Map()` method bodies for authorization-related method calls or attributes.
- **Impact:** Likely a forgotten security gate. Endpoint is publicly accessible.
- **False positive risk:** Medium (some endpoints are intentionally anonymous).

### SM0025: Async Blocking in Endpoint Handlers

`.GetAwaiter().GetResult()` or `.Result` used inside an endpoint handler lambda.

- **Detection:** Scan endpoint `Map()` method bodies for `GetResult()` or `.Result` property access on `Task`/`Task<T>`.
- **Impact:** Potential deadlock under load in ASP.NET request pipeline.
- **False positive risk:** Medium (acceptable in some contexts like interceptors).

### SM0026: Entity Configuration Without Matching DbSet

`IEntityTypeConfiguration<T>` exists but no `DbSet<T>` in the module's DbContext. Extends existing SM0006 with module-scoped precision.

- **Detection:** Already partially covered by SM0006. Enhancement: scope the check per-module rather than globally.
- **Impact:** Dead configuration code, entity not included in migrations.
- **False positive risk:** Low.

---

## Implementation Priority

| Priority | IDs | Rationale |
|----------|-----|-----------|
| 1st | SM0015, SM0018 | Easiest to implement, highest value, data already available in generator |
| 2nd | SM0017, SM0020 | Catches startup/routing failures |
| 3rd | SM0016, SM0021 | Settings and Vogen consistency |
| 4th | SM0019, SM0022 | Architecture enforcement |
| 5th | SM0023–SM0026 | Developer experience, higher false positive risk |

All diagnostics integrate with the existing generator infrastructure that already scans `[Module]`, `[Dto]`, `IEndpoint`, and `DbContext` types.
