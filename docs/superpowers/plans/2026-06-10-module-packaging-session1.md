# Module Packaging — Session 1 (Package Contract) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Every SimpleModule module assembly carries a machine-readable JSON manifest emitted at compile time; the host discovers module frontend bundles via that manifest instead of filesystem probing; module DbContexts that ship EF migrations get them applied; an in-repo module (FeatureFlags) loads end-to-end as a packed nupkg from a local folder feed.

**Architecture:** A new `ModuleManifestEmitter` in the Roslyn generator emits `[assembly: ModuleManifest("{json}")]` into each module assembly (source generators cannot emit literal embedded resources — the assembly-level attribute is the closest equivalent readable both via reflection and via `System.Reflection.Metadata` without loading; `sm pack` in Session 2 will additionally extract it to a `module-manifest.json` in the nupkg). The generator gains a `SimpleModuleProjectKind` analyzer-config switch: `Module` projects get ONLY the manifest emitter; hosts keep current behavior. Hosting builds a `ModuleManifestRegistry` from the DI-registered `IModule` instances and injects a `<script id="sm-module-assets" type="application/json">` map into the Inertia HTML shell; `resolve-page.ts` consults it before falling back to the existing `/_content/` probing.

**Tech Stack:** Roslyn `IIncrementalGenerator` (netstandard2.0), System.Text.Json, ASP.NET Core, React 19/Inertia, xUnit.v3 + FluentAssertions.

**Pre-established facts (verified in repo):**
- Generator attaches only to Host/Worker today (`template/SimpleModule.Host/SimpleModule.Host.csproj:20`). Module projects do NOT run it.
- `modules/Directory.Build.props` already sets `IsPackable=true` + `PackageTags simplemodule-module`; `modules/Directory.Build.targets` already runs Vite before `GenerateNuspec` → nupkgs already contain built static web assets.
- `ModuleAttribute` (`framework/SimpleModule.Core/ModuleAttribute.cs`) has `Name`, `Version`, `RoutePrefix`, `ViewPrefix`. No `DisplayName`.
- `DiscoveryData` (`framework/SimpleModule.Generator/Discovery/DiscoveryData.cs:33`) has no event records; manifest needs an `EventFinder`.
- `UseSimpleModuleInfrastructure` (`framework/SimpleModule.Hosting/SimpleModuleHostExtensions.cs:176-210`) applies migrations ONLY for the host context; module contexts are skipped (HostDbContext + EnsureCreated covers in-repo modules, but NOT packaged modules with own migrations).
- Generated `ModuleExtensions` exposes `public static readonly Assembly[] ModuleAssemblies` and registers every module: `services.AddSingleton<IModule>(instance)`.
- `HtmlFileInertiaPageRenderer.RenderPageAsync` already concatenates a JSON `<script data-page="app">` at render time — the asset-map script goes in the same concat.
- `resolve-page.ts` probes `SimpleModule.{Module}` then `{Module}` under `/_content/`.
- FeatureFlags = checkpoint module (1 page `FeatureFlags/Manage`, 1 DbContext, permissions).

---

## Manifest schema (v1) — locked contract

```json
{
  "schemaVersion": 1,
  "id": "SimpleModule.FeatureFlags",
  "name": "FeatureFlags",
  "displayName": "Feature Flags",
  "version": "1.0.0",
  "frameworkCompat": ">=0.0.38 <1.0.0",
  "routePrefix": "/api/feature-flags",
  "viewPrefix": "/feature-flags",
  "schema": "FeatureFlags",
  "permissions": ["FeatureFlags.View", "FeatureFlags.Manage"],
  "frontendEntry": "_content/SimpleModule.FeatureFlags/SimpleModule.FeatureFlags.pages.js",
  "pages": ["FeatureFlags/Manage"],
  "eventsPublished": ["SimpleModule.FeatureFlags.Contracts.FlagToggled"],
  "eventsConsumed": ["SimpleModule.Users.Contracts.UserCreated"],
  "hasDbContext": true
}
```

Field sources: `id` = assembly name; `name`/`version`/`routePrefix`/`viewPrefix` = `[Module]`; `displayName` = new optional `ModuleAttribute.DisplayName` (defaults to `name`); `schema` = module name (the `ModuleConnections` key in `AddModuleDbContext`); `permissions` = `PermissionClassRecord` field values for the module; `frontendEntry` = `_content/{assembly}/{assembly}.pages.js` when `Views.Length > 0`, else `null`; `pages` = view page names; `eventsPublished` = `DomainEvent`-derived types declared in the module's contracts/impl assemblies; `eventsConsumed` = first parameters of Wolverine-convention handlers (`*Handler`/`*Consumer` with `Handle/HandleAsync/Consume/ConsumeAsync`) declared in the module; `frameworkCompat` = `>={referenced SimpleModule.Core assembly version} <{nextMajor}.0.0`, overridable via MSBuild property `SimpleModuleFrameworkCompat` (CompilerVisibleProperty).

---

### Task 1: Core — `ModuleManifestAttribute`, `ModuleManifest` model, reader

**Files:**
- Create: `framework/SimpleModule.Core/Modules/ModuleManifestAttribute.cs`
- Create: `framework/SimpleModule.Core/Modules/ModuleManifest.cs`
- Create: `framework/SimpleModule.Core/Modules/ModuleManifestReader.cs`
- Test: in the existing framework test project (locate via `ls tests/`; if only module tests exist, add to `tests/SimpleModule.Hosting.Tests` or nearest framework-level test project)

- [ ] **Step 1: Write failing tests** — reader parses a manifest from an assembly attribute; returns null for assemblies without one; tolerates unknown JSON fields (forward compat); rejects `schemaVersion` > current with a clear exception type (`ModuleManifestSchemaException` or return-with-flag — pick exception).

```csharp
[Fact]
public void TryRead_returns_null_for_assembly_without_manifest()
{
    ModuleManifestReader.TryRead(typeof(object).Assembly).Should().BeNull();
}

[Fact]
public void Parse_roundtrips_schema_v1_json()
{
    var json = """{"schemaVersion":1,"id":"SimpleModule.X","name":"X","displayName":"X","version":"1.0.0","frameworkCompat":">=0.0.38 <1.0.0","routePrefix":"/api/x","viewPrefix":"/x","schema":"X","permissions":["X.View"],"frontendEntry":"_content/SimpleModule.X/SimpleModule.X.pages.js","pages":["X/Browse"],"eventsPublished":[],"eventsConsumed":[],"hasDbContext":true}""";
    var m = ModuleManifestReader.Parse(json);
    m.Id.Should().Be("SimpleModule.X");
    m.FrontendEntry.Should().NotBeNull();
    m.Pages.Should().ContainSingle("X/Browse");
}
```

- [ ] **Step 2: Run tests, verify they fail** (`dotnet test --filter "FullyQualifiedName~ModuleManifest"`)
- [ ] **Step 3: Implement.**

```csharp
// ModuleManifestAttribute.cs
namespace SimpleModule.Core.Modules;

/// <summary>
/// Carries the compile-time module manifest JSON emitted by SimpleModule.Generator.
/// Assembly-level so tooling can read it via System.Reflection.Metadata without
/// loading the assembly. Source generators cannot add embedded resources, which is
/// why the manifest travels as an attribute rather than a resource stream.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class ModuleManifestAttribute(string json) : Attribute
{
    public string Json { get; } = json;
}
```

```csharp
// ModuleManifest.cs — System.Text.Json POCO, camelCase, init-only props,
// IReadOnlyList<string> collections, int SchemaVersion, string? FrontendEntry.
// ModuleManifestReader.cs — static Parse(string) + TryRead(Assembly) via
// assembly.GetCustomAttribute<ModuleManifestAttribute>(); JsonSerializerOptions
// with PropertyNameCaseInsensitive + camelCase naming.
```

- [ ] **Step 4: Tests pass; commit** `feat(core): module manifest model, attribute and reader`

### Task 2: Generator — event discovery (`EventFinder`)

**Files:**
- Create: `framework/SimpleModule.Generator/Discovery/Finders/EventFinder.cs`
- Modify: `framework/SimpleModule.Generator/Discovery/DiscoveryData.cs` (+2 arrays: `EventTypes`, `EventHandlers` — update ctor, `Empty`, `Equals`, `GetHashCode`)
- Create record types in `framework/SimpleModule.Generator/Discovery/Records/DataRecords.cs`:

```csharp
internal readonly record struct EventTypeRecord(string FullyQualifiedName, string AssemblyName);
internal readonly record struct EventHandlerRecord(string EventFullyQualifiedName, string AssemblyName);
```

- Modify: `framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs` + `DiscoveryDataBuilder.cs` (wire finder into the extraction pipeline, mirroring `PermissionFeatureFinder`)
- Test: `tests/SimpleModule.Generator.Tests/EventDiscoveryTests.cs` (mirror an existing finder test's compilation harness)

- [ ] **Step 1: Failing test** — a compilation with `public sealed record FlagToggled(...) : DomainEvent;` yields an `EventTypeRecord`; a `FlagToggledHandler` class with `Handle(FlagToggled e)` yields an `EventHandlerRecord`. Non-DomainEvent first params and non-conventional class names are ignored.
- [ ] **Step 2: Verify fail. Step 3: Implement** — walk type symbols per assembly (same walker the other finders use): published = named types whose base chain hits `SimpleModule.Core.Events.DomainEvent`; consumed = classes named `*Handler`/`*Consumer` having a public method `Handle|HandleAsync|Consume|ConsumeAsync` whose first parameter's type derives from `DomainEvent`.
- [ ] **Step 4: Tests pass; full `dotnet build` green (incremental-cache equality members updated!); commit** `feat(generator): discover domain events and Wolverine-convention handlers`

### Task 3: Generator — `ModuleManifestEmitter` + `DisplayName` + project-kind switch

**Files:**
- Modify: `framework/SimpleModule.Core/ModuleAttribute.cs` — add `public string DisplayName { get; set; } = "";`
- Modify: `framework/SimpleModule.Generator/Discovery/Finders/ModuleFinder.cs` — read `DisplayName` named arg → `ModuleInfoRecord` (new field; update Equals/GetHashCode)
- Create: `framework/SimpleModule.Generator/Emitters/ModuleManifestEmitter.cs`
- Modify: `framework/SimpleModule.Generator/ModuleDiscovererGenerator.cs` — read analyzer config + combine with compilation provider:

```csharp
var kindProvider = context.AnalyzerConfigOptionsProvider.Select(static (p, _) =>
    p.GlobalOptions.TryGetValue("build_property.SimpleModuleProjectKind", out var k) ? k : "");
var compatProvider = context.AnalyzerConfigOptionsProvider.Select(static (p, _) =>
    p.GlobalOptions.TryGetValue("build_property.SimpleModuleFrameworkCompat", out var c) ? c : "");
// Combine(dataProvider, kindProvider, compatProvider):
//   kind == "Module" → run ONLY ModuleManifestEmitter (manifest for the module whose
//                      AssemblyName == data.HostAssemblyName, i.e. declared in THIS compilation)
//   else             → current emitter set, unchanged (downstream hosts unaffected)
```

- Manifest JSON built with the C# string escaped via `SymbolDisplay`-safe escaping (generator targets netstandard2.0 — hand-roll JSON with a small builder; NO Newtonsoft/STJ dependency in the generator).
- `frameworkCompat`: find the referenced assembly identity named `SimpleModule.Core` in `compilation.ReferencedAssemblyNames` → default `>={ver} <{major+1}.0.0`; the `SimpleModuleFrameworkCompat` build property overrides verbatim. (Capture the Core version into `DiscoveryData` as a new `string CoreAssemblyVersion` field during `SymbolDiscovery.Extract`.)
- Emitted source shape:

```csharp
// ModuleManifest.g.cs (in the module's own compilation only)
[assembly: global::SimpleModule.Core.Modules.ModuleManifest("{escaped json}")]
```

- Test: `tests/SimpleModule.Generator.Tests/ModuleManifestEmitterTests.cs` — compile a fake module (`[Module("X", RoutePrefix=..., ViewPrefix=...)]` + view endpoint + permission class + event) with `SimpleModuleProjectKind=Module`; assert generated attribute source contains expected JSON fields; assert host-kind compilation does NOT get a manifest and DOES get the classic artifacts; assert module-kind compilation does NOT get `AddModules` etc.

- [ ] Step 1 failing tests → Step 2 verify → Step 3 implement → Step 4 all generator tests pass
- [ ] **Step 5: Commit** `feat(generator): emit module manifest attribute for module-kind projects`

### Task 4: Attach generator to module projects

**Files:**
- Modify: `modules/Directory.Build.props`:

```xml
<PropertyGroup>
  <SimpleModuleProjectKind Condition="'$(SimpleModuleProjectKind)' == ''">Module</SimpleModuleProjectKind>
</PropertyGroup>
<ItemGroup>
  <CompilerVisibleProperty Include="SimpleModuleProjectKind" />
  <CompilerVisibleProperty Include="SimpleModuleFrameworkCompat" />
</ItemGroup>
<ItemGroup Condition="'$(MSBuildProjectName)' != '' And $(MSBuildProjectDirectory.Contains('src'))">
  <ProjectReference Include="$(RepoRoot)framework/SimpleModule.Generator/SimpleModule.Generator.csproj"
                    OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
</ItemGroup>
```

(Confirm `$(RepoRoot)` is defined in root Directory.Build.props — it is referenced by `modules/Directory.Build.targets` already. Tests projects also live under `modules/*/tests` — the analyzer is harmless there (no `[Module]` in-compilation → no manifest) but exclude if build time suffers.)

- [ ] **Step 1:** Edit props. **Step 2:** `dotnet build` whole solution — expect green; verify with a one-liner that `SimpleModule.FeatureFlags.dll` now carries the attribute:

```bash
dotnet build modules/FeatureFlags/src/SimpleModule.FeatureFlags -v:q && \
strings modules/FeatureFlags/src/SimpleModule.FeatureFlags/bin/Debug/net10.0/SimpleModule.FeatureFlags.dll | grep -o 'schemaVersion' | head -1
```

- [ ] **Step 3:** Host still builds + `dotnet test tests/SimpleModule.Generator.Tests` green. **Step 4: Commit** `build(modules): run source generator in module-kind on module projects`

### Task 5: Hosting — manifest registry + HTML asset-map injection

**Files:**
- Create: `framework/SimpleModule.Hosting/Modules/ModuleManifestRegistry.cs` (+ `IModuleManifestRegistry` in `SimpleModule.Core/Modules/`)

```csharp
public interface IModuleManifestRegistry
{
    IReadOnlyList<ModuleManifest> Manifests { get; }
    ModuleManifest? Get(string moduleName);
}
// Impl: ctor(IEnumerable<IModule> modules) → for each module instance read
// ModuleManifestReader.TryRead(module.GetType().Assembly); skip nulls (modules
// compiled without the generator attached keep working).
```

- Modify: `framework/SimpleModule.Hosting/SimpleModuleHostExtensions.cs` — register `IModuleManifestRegistry` singleton in `AddSimpleModuleInfrastructure` (resolve `IEnumerable<IModule>` from the provider at construction: `services.AddSingleton<IModuleManifestRegistry>(sp => new ModuleManifestRegistry(sp.GetServices<IModule>()))`).
- Modify: `framework/SimpleModule.Hosting/Inertia/HtmlFileInertiaPageRenderer.cs` — ctor gains `IModuleManifestRegistry registry`; build once: `_moduleAssetsJson = {"FeatureFlags":"_content/SimpleModule.FeatureFlags/SimpleModule.FeatureFlags.pages.js", ...}` (modules with non-null `FrontendEntry` only; serialize with STJ). In `RenderPageAsync` concat: `<script id="sm-module-assets" type="application/json" nonce="{nonce}">{_moduleAssetsJson}</script>` right before the `data-page` script. Empty map → emit nothing.
- Test: integration test using `SimpleModuleWebApplicationFactory` — GET a view page, assert response HTML contains `id="sm-module-assets"` and the FeatureFlags entry. Place beside existing hosting/host integration tests (find with `grep -rl SimpleModuleWebApplicationFactory tests/ | head`).

- [ ] Steps: failing test → implement → pass → **Commit** `feat(hosting): module manifest registry and frontend asset map injection`

### Task 6: Client — manifest-first page resolution

**Files:**
- Modify: `packages/SimpleModule.Client/src/resolve-page.ts`

```typescript
let moduleAssets: Record<string, string> | null | undefined;
function getModuleAssets(): Record<string, string> | null {
  if (moduleAssets !== undefined) return moduleAssets;
  const el = document.getElementById('sm-module-assets');
  moduleAssets = el?.textContent ? JSON.parse(el.textContent) : null;
  return moduleAssets;
}
// In resolvePage: before probing, const entry = getModuleAssets()?.[moduleName];
// if entry → import(`/${entry}${suffix}`) and on success skip candidate loop.
// On failure (or no manifest) fall through to existing candidates probing.
```

- [ ] **Step 1:** implement (keep fallback intact, JSON.parse wrapped in try/catch → null). **Step 2:** `npm run check` green; `npm run dev:build` green. **Step 3: Commit** `feat(client): resolve module bundles via sm-module-assets manifest map`

### Task 7: Database — apply migrations for module contexts that ship them

**Files:**
- Modify: `framework/SimpleModule.Hosting/SimpleModuleHostExtensions.cs:189-209`

```csharp
foreach (var info in infos)
{
    if (scope.ServiceProvider.GetService(info.DbContextType) is not DbContext db)
        continue;

    var hasMigrations = db.Database.GetMigrations().Any();
    if (info.ModuleName == DatabaseConstants.HostModuleName)
    {
        if (hasMigrations) await db.Database.MigrateAsync();
        else await db.Database.EnsureCreatedAsync();
    }
    else if (hasMigrations)
    {
        // Packaged modules ship their own EF migrations (EnsureCreated is not
        // acceptable for installed modules). In-repo module contexts without
        // migrations still get their schema from the unified HostDbContext.
        await db.Database.MigrateAsync();
    }
}
```

- [ ] Test: existing host startup integration tests stay green (`dotnet test`), behavior unchanged for migration-less contexts. Document the EnsureCreated→Migrate transition caveat in the design doc. **Commit** `feat(hosting): apply EF migrations for module DbContexts that bundle them`

### Task 8: Design doc

**Files:**
- Create: `docs/site/advanced/module-packaging.md` — manifest schema v1 (table per field + JSON example), nupkg layout (lib/net10.0 assembly + contracts package + `staticwebassets/` tree + migrations inside the module assembly), frontend externals contract (react, react-dom, @inertiajs/react, SimpleModule.UI host-provided; validated at pack time in Session 2), version compat rules (`frameworkCompat` semantics, 0.x caveat, override property), the embedded-resource-vs-attribute deviation rationale, migration application contract, and the `simplemodule-module` tag convention.
- Check `docs/site/.vitepress/config.*` (or equivalent sidebar config) and add the page to the Advanced sidebar if pages are listed explicitly.

- [ ] Write doc → `npm run check` (if docs are covered) → **Commit** `docs: module packaging contract (manifest schema v1, nupkg layout, externals)`

### Task 9: Checkpoint — FeatureFlags as a packed nupkg

- [ ] **Step 1:** Pack to a local feed (Version defaults to 1.0.0, matching project-reference identities so NuGet unifies Core deps with in-solution projects):

```bash
FEED=$CLAUDE_JOB_DIR/tmp/local-feed && mkdir -p $FEED
dotnet pack modules/FeatureFlags/src/SimpleModule.FeatureFlags.Contracts -o $FEED
dotnet pack modules/FeatureFlags/src/SimpleModule.FeatureFlags -o $FEED
unzip -l $FEED/SimpleModule.FeatureFlags.1.0.0.nupkg | grep -E "pages.js|dll"   # static assets present
```

- [ ] **Step 2:** In `template/SimpleModule.Host/SimpleModule.Host.csproj` swap the two FeatureFlags `ProjectReference`s for `<PackageReference Include="SimpleModule.FeatureFlags" Version="1.0.0" />` (temporary, working tree only).
- [ ] **Step 3:** `dotnet restore template/SimpleModule.Host -p:RestoreAdditionalProjectSources=$FEED && dotnet build template/SimpleModule.Host` — green.
- [ ] **Step 4:** Run the host, then verify: `curl -sk https://localhost:5001/feature-flags` returns HTML containing `sm-module-assets` with the FeatureFlags entry; `curl -sk https://localhost:5001/_content/SimpleModule.FeatureFlags/SimpleModule.FeatureFlags.pages.js -o /dev/null -w '%{http_code}'` → 200. Drive the page in a browser (playwright) to confirm React renders.
- [ ] **Step 5:** Revert the Host csproj swap (`git checkout template/SimpleModule.Host/SimpleModule.Host.csproj`); record results in the checkpoint report.

### Task 10: Full verification

- [ ] `dotnet build` (TreatWarningsAsErrors — zero warnings)
- [ ] `dotnet test` (all)
- [ ] `npm run check` + `npm run validate-pages` + `npm run build:dev`
- [ ] Write checkpoint report: frozen API surface (ModuleManifestAttribute ctor, manifest schema v1 field set, `sm-module-assets` element id, `SimpleModuleProjectKind`/`SimpleModuleFrameworkCompat` build properties, `IModuleManifestRegistry`), framework friction found (GitHub issues labeled `packaging`), assumptions made.

---

## Self-review notes

- Spec coverage: manifest schema/emission ✔ (Tasks 1-4), frontend loading via manifest ✔ (5-6), migrations hook ✔ (7), design doc ✔ (8), checkpoint ✔ (9). Marketplace audit was completed pre-plan (module already deleted in b2698964; recommendation: leave deleted).
- Deviation from spec: "embedded resource" → assembly-level attribute (Roslyn limitation); documented in Task 8 and the checkpoint report.
- Out of scope kept out: no CLI commands (Session 2), no publish/search (Session 3), no custom registry.
