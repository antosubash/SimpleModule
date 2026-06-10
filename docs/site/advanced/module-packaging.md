# Module Packaging

SimpleModule modules are distributed as **standard NuGet packages**. A packaged
module is a normal `.nupkg` containing the module assembly, its prebuilt
frontend bundle as static web assets, and its EF Core migrations. No custom
registry is required: discovery and installation work against any NuGet V3 feed
(nuget.org by default), with packages identified by the `simplemodule-module`
tag.

This page is the authoritative contract for module packages: the manifest
schema, the nupkg layout, the frontend externals contract, and the version
compatibility rules.

## The module manifest

Every module assembly carries a compile-time manifest describing the module to
hosts and tooling. The manifest is JSON emitted by `SimpleModule.Generator`
during the module's own build.

::: info Why an assembly attribute and not an embedded resource?
Roslyn source generators can only add *source* to a compilation — they cannot
attach embedded resources. The manifest therefore travels as an assembly-level
attribute, which is the closest in-assembly equivalent: readable at runtime via
reflection (`ModuleManifestReader.TryRead(assembly)`) and by tooling via
`System.Reflection.Metadata` without loading the assembly or its dependencies.
`sm pack` additionally extracts the manifest to a
`module-manifest.json` inside the nupkg so feeds and registries can read it
without touching the assembly at all.
:::

The generated source looks like this:

```csharp
// ModuleManifest.g.cs (auto-generated into the module assembly)
[assembly: global::SimpleModule.Core.Modules.ModuleManifest(@"{""schemaVersion"":1,...}")]
```

### Schema (version 1)

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
  "permissions": ["FeatureFlags.Manage", "FeatureFlags.View"],
  "frontendEntry": "_content/SimpleModule.FeatureFlags/SimpleModule.FeatureFlags.pages.js",
  "pages": ["FeatureFlags/Manage"],
  "eventsPublished": ["SimpleModule.FeatureFlags.Contracts.Events.FeatureFlagToggledEvent"],
  "eventsConsumed": [],
  "hasDbContext": true
}
```

| Field | Source | Meaning |
|-------|--------|---------|
| `schemaVersion` | generator constant | Manifest schema this assembly was built against. Hosts refuse manifests newer than they understand. |
| `id` | assembly name | Package/assembly identity. |
| `name` | `[Module("Name")]` | Module name — also the database schema/prefix and `ModuleConnections` key. |
| `displayName` | `[Module(DisplayName = "...")]` | Human-readable name; defaults to `name`. |
| `version` | `[Module]` version argument | Module's own version. |
| `frameworkCompat` | derived or `SimpleModuleFrameworkCompat` MSBuild property | SemVer range of `SimpleModule.Core` versions the module supports. |
| `routePrefix` / `viewPrefix` | `[Module]` | API and view route prefixes. |
| `schema` | module name | Schema/prefix name for module data isolation. |
| `permissions` | `IModulePermissions` const fields | All permission values the module declares. |
| `frontendEntry` | view discovery | Static web asset path of the prebuilt pages bundle; `null` for backend-only modules. |
| `pages` | `IViewEndpoint` discovery | Inertia page names the module serves. |
| `eventsPublished` | `IEvent` implementors declared in the module's assemblies | Domain events the module defines. |
| `eventsConsumed` | Wolverine-convention handlers (`*Handler`/`*Consumer` with `Handle`/`Consume` methods) | Domain events the module subscribes to. |
| `hasDbContext` | DbContext discovery | Whether the module owns a DbContext. |

Unknown fields are ignored when parsing (forward compatibility). A
`schemaVersion` higher than the framework understands fails closed with a
descriptive error.

### How emission is wired

Module projects build with the source generator attached in **module kind**:

```xml
<!-- modules/Directory.Build.props (already configured in this repo;
     `sm new module` scaffolds the same for standalone modules) -->
<PropertyGroup>
  <SimpleModuleProjectKind>Module</SimpleModuleProjectKind>
</PropertyGroup>
<ItemGroup>
  <CompilerVisibleProperty Include="SimpleModuleProjectKind" />
  <CompilerVisibleProperty Include="SimpleModuleFrameworkCompat" />
</ItemGroup>
```

In module kind the generator emits **only** the manifest attribute. Host-level
artifacts (`AddModules()`, endpoint maps, the host DbContext, TypeScript
definitions, …) remain exclusive to host projects, which run the generator
without `SimpleModuleProjectKind` set.

## Nupkg layout

A packed module is a regular Razor-class-library-style package:

```
SimpleModule.FeatureFlags.1.2.0.nupkg
├── lib/net10.0/
│   └── SimpleModule.FeatureFlags.dll        # module assembly + [ModuleManifest]
│                                            # + bundled EF Core migrations
├── staticwebassets/
│   ├── SimpleModule.FeatureFlags.pages.js   # prebuilt Vite bundle (library mode)
│   └── simplemodule.featureflags.css        # optional module CSS
├── README.md
└── (package metadata: tags include `simplemodule-module`)
```

The contracts assembly ships as its own package
(`SimpleModule.FeatureFlags.Contracts`) so other modules can depend on the
public surface without pulling the implementation.

Key facts:

- **Frontend bundles ship prebuilt.** The module's Vite build runs before
  `pack` (wired into `GenerateNuspec` via `modules/Directory.Build.targets`),
  so consumers never need Node to install a module.
- **Migrations ship inside the module assembly.** Packaged modules MUST bundle
  EF Core migrations for their DbContext. `EnsureCreated` is not acceptable for
  installed modules — it cannot evolve an existing database. At startup the
  host applies migrations for every module context that ships them
  (`MigrateAsync`); module contexts without migrations (in-repo modules) keep
  relying on the unified host DbContext.
- **The `simplemodule-module` package tag** marks the package as a SimpleModule
  module for search and discovery.

::: warning EnsureCreated → migrations transition
A database first created via `EnsureCreated` has no `__EFMigrationsHistory`
table. Installing a packaged module into such a database applies the module's
migrations from zero, which is safe because module tables are schema-isolated —
but the *host's* own tables must not be managed by both mechanisms. Fresh hosts
should use migrations from the start.
:::

## Frontend externals contract

Module bundles are Vite **library-mode** builds and MUST externalize:

- `react` (and `react/jsx-runtime`)
- `react-dom` (and `react-dom/client`)
- `@inertiajs/react`

These are provided by the host at runtime via the import map in the HTML shell.
A module that bundles its own React copy breaks hooks (two React instances) and
bloats every page load. `sm pack` validates the built bundle and
fails closed when one of the externals is found inlined.

::: warning @simplemodule/ui is currently inlined
The shared UI component library is not yet host-provided (no vendor bundle /
import-map entry), so each module bundle statically includes the components it
uses. Externalizing it is planned; when that lands the externals contract and
the pack-time validation will extend to `@simplemodule/ui`.
:::

### How the host finds module bundles

The host builds a module → bundle map from the loaded manifests
(`IModuleManifestRegistry`) and injects it into the HTML shell:

```html
<script id="sm-module-assets" type="application/json">
  {"FeatureFlags":"_content/SimpleModule.FeatureFlags/SimpleModule.FeatureFlags.pages.js"}
</script>
```

The client-side page resolver imports the exact path from this map. Modules
without a manifest (built before manifest emission existed) fall back to the
legacy convention probe: `/_content/SimpleModule.{Module}/…` then
`/_content/{Module}/…`.

## Version compatibility rules

`frameworkCompat` is a SemVer range over the `SimpleModule.Core` version:

- **Default:** derived at compile time from the referenced `SimpleModule.Core`
  assembly: `>={version} <{nextMajor}.0.0`.
- **Override:** set the MSBuild property explicitly when you have verified a
  wider or narrower range:

  ```xml
  <PropertyGroup>
    <SimpleModuleFrameworkCompat>&gt;=0.0.38 &lt;1.0.0</SimpleModuleFrameworkCompat>
  </PropertyGroup>
  ```

- Installation tooling (`sm add`) checks the host's framework version
  against this range **before** installing and refuses incompatible modules
  (override with `--force` at your own risk).

::: tip Pre-1.0 caveat
While the framework is on `0.x`, the default range `>=0.0.N <1.0.0` is
optimistic — SemVer reserves the right to break between 0.x minors. The range
semantics tighten when the framework reaches 1.0. Pin a narrower override if
your module depends on unstable surface.
:::

## Reading manifests programmatically

```csharp
using SimpleModule.Core.Modules;

// At runtime (host side) — all loaded modules:
var registry = serviceProvider.GetRequiredService<IModuleManifestRegistry>();
foreach (var manifest in registry.Manifests)
{
    Console.WriteLine($"{manifest.DisplayName} {manifest.Version} ({manifest.Id})");
}

// From a specific assembly:
ModuleManifest? manifest = ModuleManifestReader.TryRead(typeof(MyModule).Assembly);
```
