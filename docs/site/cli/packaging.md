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
