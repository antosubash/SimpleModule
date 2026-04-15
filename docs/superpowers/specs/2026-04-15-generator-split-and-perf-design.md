# Source Generator Split & Safe Perf Wins

**Status:** Draft
**Date:** 2026-04-15
**Scope:** `framework/SimpleModule.Generator/`

## Problem

Three files in `SimpleModule.Generator` have grown well past the project's 300-line cap:

| File | Lines |
|---|---|
| `Discovery/SymbolDiscovery.cs` | 2068 |
| `Emitters/DiagnosticEmitter.cs` | 1294 |
| `Discovery/DiscoveryData.cs` | 640 |
| `Emitters/HostDbContextEmitter.cs` | 290 (borderline, out of scope) |

`SymbolDiscovery.Extract` alone is ~790 lines with 30+ static helpers, mixing module/endpoint/DTO/DbContext/contract/permission/feature/interceptor/Vogen/agent discovery in one method. `DiagnosticEmitter` holds 38 `DiagnosticDescriptor` definitions and the matching check logic in a single class. `DiscoveryData` declares the top-level record plus every nested record type.

Alongside the size problem, discovery itself does repeated work per invocation (it runs on every compilation change in the IDE):
- ~15 `compilation.GetTypeByMetadataName` calls scattered through `Extract`
- `compilation.References` iterated 3+ times
- `FindClosestModuleName` does a linear scan over modules, called per endpoint/view/DbContext/entity-config
- `moduleNsByName` rebuilt inside a per-module loop

## Goal

1. **No file over 300 lines** in `framework/SimpleModule.Generator/`.
2. **Safe performance wins** — clarity-preserving improvements that reduce repeated work inside a single `Extract` call. No restructuring of the incremental pipeline topology.
3. **Zero behavior change** — same diagnostics, same generated output, same test results.

Explicitly **out of scope**: switching to `SyntaxProvider.ForAttributeWithMetadataName` (fundamentally different pipeline, can't see referenced assemblies' types); base-type-walk memoization; any change to the `CompilationProvider.Select` topology.

## Design

### File split

#### `SymbolDiscovery.cs` (2068 → 12 files)

| New file | Contents | Approx LOC |
|---|---|---|
| `Discovery/SymbolDiscovery.cs` | `Extract` thin orchestrator — resolves `CoreSymbols`, classifies references, calls finders in order, assembles `DiscoveryData` | ~180 |
| `Discovery/CoreSymbols.cs` | **New.** Record holding all `GetTypeByMetadataName` lookups resolved once per `Extract` | ~80 |
| `Discovery/Finders/ModuleFinder.cs` | `FindModuleTypes` + capability probing | ~200 |
| `Discovery/Finders/EndpointFinder.cs` | `FindEndpointTypes`, `ReadRouteConstFields`, view-page inference | ~220 |
| `Discovery/Finders/DtoFinder.cs` | `FindDtoTypes`, `FindConventionDtoTypes`, `ExtractDtoProperties`, `HasJsonIgnoreAttribute` | ~250 |
| `Discovery/Finders/DbContextFinder.cs` | `FindDbContextTypes`, `FindEntityConfigTypes`, `HasDbContextConstructorParam` | ~250 |
| `Discovery/Finders/ContractFinder.cs` | `ScanContractInterfaces`, `FindContractImplementations`, `GetContractLifetime`, contracts-assembly classification helpers | ~220 |
| `Discovery/Finders/PermissionFeatureFinder.cs` | `FindPermissionClasses`, `FindFeatureClasses`, `FindModuleOptionsClasses` | ~220 |
| `Discovery/Finders/InterceptorFinder.cs` | `FindInterceptorTypes` | ~60 |
| `Discovery/Finders/VogenFinder.cs` | `FindVogenValueObjectsWithEfConverters`, `IsVogenValueObject`, `ResolveUnderlyingType` | ~150 |
| `Discovery/Finders/AgentFinder.cs` | generic `FindImplementors`, agent/tool/knowledge wiring | ~80 |
| `Discovery/SymbolHelpers.cs` | `ImplementsInterface`, `InheritsFrom`, `DeclaresMethod`, `FindClosestModuleName`, `ScanModuleAssemblies`, `GetSourceLocation`, `FindConcreteClassesImplementing` | ~200 |

All finders remain `internal static` classes. No public surface change.

#### `DiscoveryData.cs` (640 → 3 files)

| New file | Contents | Approx LOC |
|---|---|---|
| `Discovery/DiscoveryData.cs` | `DiscoveryData` record + `Equals`/`GetHashCode` + `HashHelper` + `SourceLocationRecord` | ~200 |
| `Discovery/Records/ModuleRecords.cs` | `ModuleInfoRecord`, `EndpointInfoRecord`, `ViewInfoRecord`, `ModuleDependencyRecord`, `IllegalModuleReferenceRecord` | ~150 |
| `Discovery/Records/DataRecords.cs` | DTO, DbContext, Entity, Contract, Permission, Feature, Interceptor, Vogen, ModuleOptions, Agent records | ~290 |

#### `DiagnosticEmitter.cs` (1294 → 8 files)

| New file | Contents | Approx LOC |
|---|---|---|
| `Emitters/Diagnostics/DiagnosticEmitter.cs` | `IEmitter.Emit` — routes to checker classes in order | ~60 |
| `Emitters/Diagnostics/DiagnosticDescriptors.cs` | All 38 `DiagnosticDescriptor` definitions, `internal static readonly` | ~280 |
| `Emitters/Diagnostics/ModuleChecks.cs` | SM0002, 0040, 0043, 0049 + `Strip`/shared helpers | ~180 |
| `Emitters/Diagnostics/DbContextChecks.cs` | SM0001, 0003, 0005, 0006, 0007, 0054 | ~220 |
| `Emitters/Diagnostics/ContractAndDtoChecks.cs` | SM0008, 0009, 0011, 0012, 0013, 0022, 0023, 0053, missing-contracts-assembly | ~250 |
| `Emitters/Diagnostics/PermissionFeatureChecks.cs` | SM0014–0020 (permissions), SM0041/0042/0044 (features), SM0051 (multiple options) | ~250 |
| `Emitters/Diagnostics/EndpointChecks.cs` | SM0045, 0046, 0047, 0048, 0050 | ~130 |
| `Emitters/Diagnostics/DependencyChecks.cs` | SM0010 (circular via `TopologicalSort`), illegal-reference checks | ~80 |

`AssemblyConventions` (currently in `DiagnosticEmitter.cs`) moves to `Discovery/AssemblyConventions.cs` because both discovery and emission use it.

### Safe perf wins

Each is a small, orthogonal change with no effect on generated output.

1. **`CoreSymbols` record** — one pass of `GetTypeByMetadataName` at the top of `Extract`, threaded through finders. Replaces ~15 scattered lookups.

2. **Module-by-name dictionary** — build `Dictionary<string, ModuleInfo>` once and replace `modules.Find(m => m.ModuleName == ownerName)` (currently called per endpoint and view).

3. **Single-pass reference classification** — iterate `compilation.References` once, build `List<IAssemblySymbol> refAssemblies` plus `List<IAssemblySymbol> contractsAssemblies` upfront. Subsequent scans iterate pre-classified lists, cutting `GetAssemblyOrModuleSymbol` calls by ~3x.

4. **Lift `moduleNsByName` above the loop** — it doesn't depend on the module being scanned.

5. **`FindClosestModuleName` reverse-index** — build a `(string namespace, string moduleName)[]` sorted by namespace-length descending once; each call does a single forward scan until first match. Removes repeated substring work.

6. **DTO convention-pass short-circuit** — skip recursion into namespaces whose full FQN is already in `existingDtoFqns` (common case: attributed DTOs already counted).

7. **Scope attributed DTO discovery** — stop scanning every reference assembly for `[Dto]`-attributed types; only scan module + host assemblies. Contracts assemblies get the convention pass. Reverted if any test diffs.

### Incremental caching (clarification, no change)

The existing pipeline is:
```
CompilationProvider
  → Select(Extract)           // DiscoveryData (equatable)
  → RegisterSourceOutput(Emit)
```

Discovery (`Extract`) runs on every compilation change. Emission is skipped when `DiscoveryData` is equal to the previous value. The perf wins above reduce the cost of `Extract`; they do not change what triggers it.

## Verification

### Automated gate (must pass)

1. `dotnet build framework/SimpleModule.Generator` — zero new warnings under `TreatWarningsAsErrors`.
2. `dotnet test tests/SimpleModule.Generator.Tests` — all 20 test files green.
3. `dotnet test` at solution root — integration tests green.
4. `dotnet build template/SimpleModule.Host` — generator output compiles.

### Byte-identical generated output

Before and after the refactor, dump the generated files from `SimpleModule.Host/obj/Debug/.../generated/SimpleModule.Generator/` and diff. Expected: identical modulo whitespace. Any semantic diff is a regression.

Capture a `before/` snapshot on the pre-refactor commit and diff at each commit boundary.

### Diagnostic catalog sanity

Add a one-shot reflection test that enumerates all `DiagnosticDescriptor` static readonly fields in the Diagnostics namespace and asserts, against a baseline captured from `DiagnosticEmitter.cs` on the pre-refactor commit:
- Same count (38)
- Same set of IDs (don't enumerate expected IDs in source — snapshot at step 0, compare at every later commit)
- Same severity and category per ID

### Incremental caching test

Add one test to `ModuleDiscovererGeneratorTests`:
- Run the generator once, capture `GeneratorDriverRunResult.Results[0].TrackedSteps`.
- Run again with the same compilation.
- Assert the `RegisterSourceOutput` step reports `IncrementalStepRunReason.Cached`.

Locks in that `DiscoveryData` equality still works. Fails loudly if a future change introduces a non-equatable field.

## Commit cadence

One commit per logical step, so regressions are bisect-friendly:

1. Extract `CoreSymbols` record (perf + clarity lever)
2. Split `DiscoveryData.cs` → records files
3. Split `SymbolDiscovery.cs` → `Finders/*` + `SymbolHelpers` + thin orchestrator
4. Split `DiagnosticEmitter.cs` → `DiagnosticDescriptors` + per-concern checkers
5. Single-pass reference classification
6. Module-by-name dictionary + lifted `moduleNsByName`
7. `FindClosestModuleName` reverse-index
8. DTO convention short-circuit + scoped attributed-DTO discovery (only if tests stay green)
9. Add diagnostic-catalog reflection test
10. Add incremental-caching test

Each commit must leave `dotnet build && dotnet test` green.

## Risks

- **Hidden coupling between discovery passes.** If `FindDtoTypes` and `FindConventionDtoTypes` share state through the `dtoTypes` list in a way I haven't mapped, splitting may reorder output. Mitigation: byte-identical-output diff at each step.
- **Perf win #7 (scoped attributed-DTO discovery) may change which DTOs are picked up.** Mitigation: if any test diff appears, revert that step; the rest of the design stands.
- **`DiagnosticDescriptor` accessibility changes.** Today some descriptors are `internal` (referenced from tests) and some `private`. When moved to `DiagnosticDescriptors.cs`, all must be at least `internal`. Verify test references still resolve.
- **`AssemblyConventions` relocation.** If any emitter or finder references it, the new `using` must be added. Caught by build.

## Non-goals

- Restructuring the incremental pipeline topology.
- Migrating to `SyntaxProvider.ForAttributeWithMetadataName`.
- Touching the 17 emitter classes that are already under 300 lines.
- Splitting `HostDbContextEmitter.cs` (290 lines, under the cap).
- Performance work beyond the seven items listed above.
