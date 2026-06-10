# Module Packaging — Session 2 (Pack & Add) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `sm pack` produces a validated module nupkg (production frontend build, externals check, manifest check, tests); `sm add` installs a packaged module into a host (compat check first, CPM-aware reference, local-feed nuget.config, migration run, auto-doctor); `sm remove` reverses it with a schema warning; `sm list` shows packaged modules with compat status.

**Architecture:** New CLI infrastructure shared with Session 3: `SmConfig` (sm.json registry abstraction), `NuGetClient` (V3 service-index + flat-container), `NupkgManifestReader`/`AssemblyManifestReader` (System.Reflection.Metadata — no assembly loading), `FrameworkCompatChecker` (SemVer ranges `>=X <Y`), `PackageReferenceManipulator` (CPM-aware), `NuGetConfigManipulator`, `BundleExternalsValidator`. Framework gains a migrate-only hook (`SIMPLEMODULE_MIGRATE_ONLY=1` → run DB init regardless of environment gating, then exit) so `sm add` can apply module migrations deterministically (issue #258).

**Tech Stack:** Spectre.Console.Cli, System.Reflection.Metadata, System.IO.Compression, xUnit.v3 (+ PersistedAssemblyBuilder for manifest-reader tests).

**Facts (verified):**
- CLI patterns: `Command<TSettings>`/`AsyncCommand`, `SolutionContext.Discover()`, exit codes 0/1, AnsiConsole markup; tests exercise infra classes directly with temp dirs (no CommandApp).
- `sm install` already runs `dotnet add package` (kept as the dumb low-level command; `sm add` is the module-aware one).
- Scaffolded hosts use CPM (`Directory.Packages.props` with `SimpleModule.Core` pinned) — host framework version is readable from there; fallback `version.json`.
- Doctor: 12 `IDoctorCheck` classes returning `CheckResult(Name, Status, Message)`; auto-fix by name prefix.
- Externals actually host-provided today: react, react-dom, react/jsx-runtime, react-dom/client, @inertiajs/react (`packages/SimpleModule.Client/src/vite-plugin-vendor.ts:13`). `@simplemodule/ui` is NOT vendored → pack validates the react/inertia set; UI externalization filed as a packaging issue (out of scope here).
- In-repo modules: `modules/Directory.Build.targets` runs Vite before `GenerateNuspec`; default JsBuildCommand is a production build, but a stale dev stamp can leave dev bundles (issue #260) — `sm pack` always runs a fresh production Vite build first.

---

### Task 1: CLI infra — SmConfig, FrameworkCompatChecker

**Files:** Create `cli/SimpleModule.Cli/Infrastructure/SmConfig.cs`, `cli/SimpleModule.Cli/Infrastructure/FrameworkCompatChecker.cs`; tests `tests/SimpleModule.Cli.Tests/SmConfigTests.cs`, `FrameworkCompatCheckerTests.cs`.

- `SmConfig.Load(solutionRoot)` → reads `sm.json` (`{"registry": "url"}`); missing file/field → default `https://api.nuget.org/v3/index.json`. `Save` for completeness.
- `FrameworkCompatChecker.IsCompatible(range, version)` parsing `>=X.Y.Z[-pre] <A.B.C`, `>=X.Y.Z`, empty range → compatible-with-warning (`CompatResult { Compatible, Reason }`). SemVer compare incl. numeric segments + prerelease ordering.
- TDD: tests first, then impl, commit.

### Task 2: CLI infra — manifest readers (no assembly load)

**Files:** Create `cli/SimpleModule.Cli/Infrastructure/AssemblyManifestReader.cs` (System.Reflection.Metadata: find assembly-level custom attribute whose type name is `SimpleModule.Core.Modules.ModuleManifestAttribute`, decode the single string fixed arg from the value blob), `cli/SimpleModule.Cli/Infrastructure/NupkgManifestReader.cs` (ZipArchive: prefer `module-manifest.json` at package root, else first `lib/*/<PackageId>.dll` via AssemblyManifestReader), plus a tiny `ModuleManifestData` DTO (CLI-local POCO mirroring schema v1 — the CLI does not reference SimpleModule.Core).
**Tests:** `AssemblyManifestReaderTests` emits a temp assembly via `PersistedAssemblyBuilder` carrying a same-named attribute with JSON; `NupkgManifestReaderTests` zips a fixture.

### Task 3: CLI infra — PackageReferenceManipulator + NuGetConfigManipulator + HostFrameworkVersion

**Files:** Create `cli/SimpleModule.Cli/Infrastructure/PackageReferenceManipulator.cs` (CPM detect: Directory.Packages.props w/ `ManagePackageVersionsCentrally=true` → add/update `<PackageVersion>` + versionless `<PackageReference>` in host csproj; non-CPM → inline `Version`; `Remove*` counterparts; idempotent), `NuGetConfigManipulator.cs` (ensure `nuget.config` at root has a named `<add key="sm-local-<hash>" value="<dir>"/>` source; create file with `<clear/>`-free defaults if missing), `HostFrameworkVersionResolver.cs` (CPM `SimpleModule.Core` PackageVersion → else `version.json` → else null).
**Tests:** temp-dir round-trips for CPM and non-CPM, idempotency, removal.

### Task 4: CLI infra — NuGetClient + BundleExternalsValidator

**Files:** Create `cli/SimpleModule.Cli/Infrastructure/NuGetClient.cs` — given registry service-index URL: resolve `PackageBaseAddress/3.0.0` resource, `GetVersionsAsync(id)`, `DownloadNupkgAsync(id, version, destPath)`; local-directory sources bypass HTTP (`FindLocalNupkg(dir, id, version?)` choosing highest version). Create `BundleExternalsValidator.cs` — scan built JS files (wwwroot `*.pages.js` + sibling chunks): FAIL when a file contains an inlined-React marker (`Symbol.for("react.element")`, `Symbol.for("react.transitional.element")`, `react.production`, `react.development`, `__CLIENT_INTERNALS_DO_NOT_USE`); WARN when no file imports `react` at all. Returns structured violations.
**Tests:** URL/version selection for local feed; validator against fixture strings (externalized vs inlined).

### Task 5: framework — migrate-only hook

**Files:** Modify `framework/SimpleModule.Hosting/SimpleModuleHostExtensions.cs`: in `UseSimpleModuleInfrastructure`, `var migrateOnly = Environment.GetEnvironmentVariable("SIMPLEMODULE_MIGRATE_ONLY") == "1"`; run the DB-init block when `migrateOnly || <existing gating>`; after init, if migrateOnly → log summary and `Environment.Exit(0)` (documented CLI-only entry point; addresses #258 for installs).
**Tests:** existing suites stay green (hook is env-gated); note in docs.

### Task 6: `sm pack`

**Files:** Create `cli/SimpleModule.Cli/Commands/Pack/PackCommand.cs` + `PackSettings` (`[module-path]` arg; `--output`, `--version`, `--skip-tests`, `--configuration` default Release). Register in Program.cs.
Pipeline (each step fails closed with an actionable message):
1. Resolve module dir (arg path, or `modules/<name>`-style lookup via SolutionContext; must contain exactly one impl csproj, optionally a sibling `.Contracts`).
2. Frontend: if `package.json` → run `npx vite build --configLoader runner` (production default) in module dir; then `BundleExternalsValidator` on `wwwroot/` output.
3. `dotnet build -c Release [-p:Version=X]`.
4. Unless `--skip-tests`: `dotnet test <module tests project>` when present.
5. Read manifest from built dll (AssemblyManifestReader): require parseable JSON, `schemaVersion==1`, `id == assembly name`; if `frontendEntry` non-null require the wwwroot bundle exists. Write `module-manifest.json` to the module project dir (packed via the `None Pack` item added in modules/Directory.Build.props + module template).
6. `dotnet pack -c Release --no-build -o <output>` for impl (and Contracts when present); print nupkg paths.
Also: add to `modules/Directory.Build.props` (and the CLI module template csproj) `<None Include="module-manifest.json" Pack="true" PackagePath="\" Condition="Exists('module-manifest.json')" />`, and gitignore `module-manifest.json`.
**Tests:** step orchestration is in small static helpers (`PackPipeline`) tested directly: module-dir resolution, manifest validation rules, output path handling. Subprocess steps mocked behind a `ProcessRunner` seam (`Func` injection or interface) so tests don't shell out.

### Task 7: `sm add`

**Files:** Create `cli/SimpleModule.Cli/Commands/Add/AddCommand.cs` + settings (`<package-id>`, `--version`, `--source`, `--skip-migrations`, `--skip-doctor`). Register in Program.cs.
Pipeline:
1. `SolutionContext.Discover()`; resolve source: `--source` (dir or URL) → else `sm.json` registry.
2. Obtain nupkg (local find / NuGet download to temp) → `NupkgManifestReader` → manifest. No manifest → fail: "not a SimpleModule module package".
3. Compat gate BEFORE any file change: `HostFrameworkVersionResolver` + `FrameworkCompatChecker`; incompatible → fail with both versions printed.
4. Local dir source → `NuGetConfigManipulator.EnsureSource`.
5. `PackageReferenceManipulator.Add` (CPM-aware) into host csproj.
6. `dotnet restore` + `dotnet build` host (ProcessRunner).
7. Unless `--skip-migrations` and when `manifest.hasDbContext`: run host with `SIMPLEMODULE_MIGRATE_ONLY=1` (`dotnet run --project <host> --no-build`); non-zero → report + exit 1.
8. Unless `--skip-doctor`: run doctor checks in-process — refactor `DoctorCommand` to expose `static List<CheckResult> RunChecks(SolutionContext)` reused by both commands.
Print summary: module display name, version, schema, permissions count, frontend entry.
**Tests:** decision logic helpers (source resolution, compat gating, reference wiring) against temp dirs; ProcessRunner seam mocked.

### Task 8: `sm remove`

**Files:** Create `cli/SimpleModule.Cli/Commands/Remove/RemoveCommand.cs` (`<package-id>`). Look up manifest from `~/.nuget/packages/<id>/<resolved-version>/lib/*/dll` (best effort) BEFORE removal to name the schema; `PackageReferenceManipulator.Remove` from host csproj + CPM entry; ALWAYS print a prominent warning: database schema `<schema>` and its tables were left in place (data is not dropped) + what was left. Exit 0.
**Tests:** removal round-trip + warning content via captured console (AnsiConsole.Record or just test the helper that builds the message).

### Task 9: `sm list` packaged-modules section

**Files:** Modify `cli/SimpleModule.Cli/Commands/List/ListCommand.cs`: after the source-modules table, parse host csproj `PackageReference`s (+ CPM versions); for each, try manifest from the global packages cache (`NUGET_PACKAGES` env or `~/.nuget/packages`); render second table: Package, Version, Module, Framework compat (range + ✓/✗ vs host version). Packages without a manifest are skipped (not SimpleModule modules).
**Tests:** the csproj/CPM parsing helper + compat rendering decision.

### Task 10: Session 2 checkpoint (scratch host)

1. `VER=0.0.99-local` — pack framework packages referenced by the scaffolded host template (`Core`, `Database`, `Hosting`, `Generator`, + whatever the template lists) from the worktree with `-p:Version=$VER` into `$CLAUDE_JOB_DIR/tmp/feed2`.
2. `sm pack modules/FeatureFlags --version $VER --output feed2` (from the worktree solution).
3. `sm new project Demo` in a temp dir pinned to `$VER` (use the version option on `new project`; verify its name first), npm install.
4. `cd Demo && sm add SimpleModule.FeatureFlags --version $VER --source feed2` → expect: compat gate passes, nuget.config gains the feed, CPM entry added, build OK, migrate-only run OK, doctor green.
5. `dotnet test` in Demo; run Demo host; Playwright: log in, `/feature-flags/manage` renders.
6. `sm remove SimpleModule.FeatureFlags` → reference gone, schema warning printed.

### Task 11: verification + docs

- Full `dotnet build` (0 warnings), CLI tests (`dotnet run --project tests/SimpleModule.Cli.Tests`), all other suites, `npm run check`, `validate-pages`.
- Update `docs/site/cli/` with pack/add/remove/list reference page; extend `docs/site/advanced/module-packaging.md` (module-manifest.json in nupkg, migrate-only hook).
- Commits per task; checkpoint report; then `/code-review`.

**Out of scope (Session 3):** publish/search/upgrade, doctor packaging checks beyond reuse, marketplace registration.
