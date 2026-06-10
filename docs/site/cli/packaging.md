# Module packaging commands

`sm` distributes modules as standard NuGet packages (see
[Module Packaging](/advanced/module-packaging) for the package contract).
Four commands cover the local lifecycle: `pack`, `add`, `remove`, and the
packaged-modules view in `list`.

The package registry defaults to nuget.org. Point a solution at a different
NuGet V3 feed by adding `sm.json` to the solution root:

```json
{ "registry": "https://my-feed.example.com/v3/index.json" }
```

## sm pack

```bash
sm pack [module-path] [--version <x.y.z>] [--output <dir>] [--skip-tests] [-c <Configuration>]
```

Builds, validates and packs a module (and its `.Contracts` project) into
nupkgs. The pipeline fails closed at the first violated step:

1. **Frontend build** — a fresh production Vite build (when the module has a
   `package.json`).
2. **Externals validation** — the built bundle must not inline react,
   react-dom, react/jsx-runtime or @inertiajs/react (host-provided). A module
   that bundles React breaks hooks at runtime.
3. **`dotnet build`** (Release by default).
4. **`dotnet test`** of the module's test project (skip with `--skip-tests`).
5. **Manifest validation** — the built assembly must carry a parseable
   schema-v1 manifest whose id matches the assembly and whose declared
   frontend entry exists on disk.
6. **`dotnet pack`** — also writes `module-manifest.json` into the nupkg root
   and guarantees the `simplemodule-module` package tag, without editing your
   project files.

::: tip Prerelease frameworks
Packing a *stable* module version against a *prerelease* framework fails with
NU5104 — pass a prerelease `--version` (e.g. `1.2.0-rc.1`) in that case.
:::

## sm add

```bash
sm add <package-id> [--version <x.y.z>] [--source <feed>] [--skip-migrations] [--skip-doctor]
```

Installs a packaged module into the host application:

1. Resolves the nupkg from `--source` (local folder feed or NuGet V3 service
   index URL), or the `sm.json` registry.
2. Reads the module manifest — packages without one are refused (use
   `sm install` for plain NuGet packages).
3. **Compatibility gate**: checks the manifest's `frameworkCompat` range
   against the host's `SimpleModule.Core` version *before touching any file*.
4. Registers local folder feeds in `nuget.config`.
5. Adds the package reference — **CPM-aware**: with Central Package
   Management the version goes into `Directory.Packages.props` and the csproj
   gets a version-less `PackageReference`.
6. `dotnet build`, then applies the module's migrations by running the host
   once with `SIMPLEMODULE_MIGRATE_ONLY=1` (database initialization runs and
   the process exits without serving traffic).
7. Runs `sm doctor`.

## sm remove

```bash
sm remove <package-id>
```

Removes the package reference (csproj + CPM entry). The module's database
schema and data are **never dropped** — the command prints exactly what was
left behind (schema name, migration history rows, permission grants) so the
cleanup is a deliberate, manual decision.

## sm list

`sm list` shows source modules (with route prefixes and endpoint counts) and a
second table of installed packaged modules with their versions and framework
compatibility status against the current host.

## sm publish

```bash
sm publish [module-path] [--version <x.y.z>] [--source <feed-or-url>] [--api-key <key>] [--dry-run] [--register]
```

Runs the full `sm pack` pipeline, then pushes the nupkgs with
`dotnet nuget push`. The API key comes from `--api-key` or `NUGET_API_KEY`
(not needed for local folder feeds). `--dry-run` validates everything and
shows what would be pushed. `--register` is the extension point for the
future SimpleModule marketplace — today it explains that registration is not
available yet (packages are already discoverable via the
`simplemodule-module` tag).

## sm search

```bash
sm search [query] [--source <feed-or-url>] [--take <n>] [--prerelease]
```

Lists SimpleModule modules on a registry. Local folder feeds are scanned by
reading each nupkg's manifest (framework compatibility shown inline); remote
registries use the NuGet search API filtered by the `simplemodule-module`
tag (compatibility is verified definitively at `sm add` time).

## sm upgrade

```bash
sm upgrade [package-id] [--version <x.y.z>] [--source <feed>] [--force] [--skip-migrations]
```

Upgrades one installed module (or all of them when no id is given): resolves
the highest stable version, validates the new manifest's `frameworkCompat`
range — **refusing violations unless `--force`** — bumps the CPM-aware
reference, rebuilds, and applies bundled migrations via the migrate-only run.

## Doctor packaging checks

`sm doctor` validates the packaging contract too:

- **bundle externals** — source modules whose built bundles inline a React
  copy (the duplicate-React failure mode) fail the check;
- **packaged module manifests** — unreadable/newer `schemaVersion` manifests
  and framework-incompatible installed modules fail;
- **pending module migrations** — bundled migration ids are compared against
  the SQLite `__EFMigrationsHistory` table (other providers get a
  cannot-verify warning) and pending ones are listed with the command to run.

## Full lifecycle walkthrough

The complete loop, runnable against a local folder feed (`./feed`):

```bash
# 1. New host + module
sm new project Shop && cd Shop && npm install
sm new module Catalog

# 2. Pack & publish v1
sm publish src/modules/Catalog --version 1.0.0 --source ../feed

# 3. Discover and install (in any other SimpleModule host)
sm search catalog --source ../feed
sm add Catalog --version 1.0.0 --source ../feed
#   → compat gate, CPM reference, build, migrations, doctor

# 4. Ship v2 with a schema change: add an EF migration to the module
#    (Migrations/<timestamp>_AddXyz.cs with [DbContext]+[Migration]), then
sm publish src/modules/Catalog --version 1.1.0 --source ../feed

# 5. Upgrade — compat-checked, migrations applied
sm upgrade Catalog --source ../feed
sm doctor          # "Migrations Catalog: N migration(s) applied"

# 6. Uninstall (data stays, loudly)
sm remove Catalog
```

An incompatible upgrade (module built for a newer framework) is refused with
the reason and a `--force` override hint.
