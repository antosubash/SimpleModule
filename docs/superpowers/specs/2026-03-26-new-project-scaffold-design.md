# Design: Improved `sm new project` Scaffold

## Summary

Rewrite `sm new project` to produce a working project by copying the template Host and adding one starter module with tests. The scaffolded project mirrors the real template structure so users get a runnable app immediately.

## What Changes

The current `sm new project` creates bare Core/Database/Generator/Api projects with no frontend. The new version instead:

1. Copies `template/SimpleModule.Host/` as `src/{ProjectName}.Host/` (with name transformations)
2. Runs `sm new module Home` logic to create a starter module
3. Adds root config files (slnx, Directory.Build.props, etc.)
4. Creates `tests/{ProjectName}.Tests.Shared/` for test infrastructure

No separate Core, Database, or Generator projects — those are part of the framework, referenced via the Host's project references.

## Output Structure

```
{ProjectName}/
├── src/
│   ├── {ProjectName}.Host/              # Copied from template/SimpleModule.Host
│   │   ├── ClientApp/
│   │   │   ├── app.tsx
│   │   │   ├── vite.config.ts
│   │   │   ├── validate-pages.mjs
│   │   │   └── package.json
│   │   ├── Components/
│   │   │   ├── App.razor
│   │   │   ├── InertiaShell.razor
│   │   │   ├── Routes.razor
│   │   │   └── _Imports.razor
│   │   ├── Styles/
│   │   │   └── app.css
│   │   ├── Properties/
│   │   │   └── launchSettings.json
│   │   ├── wwwroot/                     # Empty (built by npm run build)
│   │   ├── Program.cs
│   │   ├── {ProjectName}.Host.csproj
│   │   ├── appsettings.json
│   │   └── appsettings.Development.json
│   └── modules/
│       └── Home/                        # Starter module (created by module scaffolding logic)
│           ├── src/
│           │   ├── Home.Contracts/
│           │   │   ├── Home.Contracts.csproj
│           │   │   ├── IItemContracts.cs
│           │   │   ├── Item.cs
│           │   │   └── Events/ItemCreatedEvent.cs
│           │   └── Home/
│           │       ├── Home.csproj
│           │       ├── HomeModule.cs
│           │       ├── HomeConstants.cs
│           │       ├── HomeDbContext.cs
│           │       ├── ItemService.cs
│           │       ├── Endpoints/Home/GetAllEndpoint.cs
│           │       ├── Pages/index.ts
│           │       ├── Views/Browse.tsx       # React page (from sm new feature)
│           │       ├── vite.config.ts
│           │       └── package.json
│           └── tests/
│               └── Home.Tests/
│                   ├── Home.Tests.csproj
│                   ├── GlobalUsings.cs
│                   ├── Unit/ItemServiceTests.cs
│                   └── Integration/HomeEndpointTests.cs
├── tests/
│   └── {ProjectName}.Tests.Shared/
│       └── {ProjectName}.Tests.Shared.csproj
├── {ProjectName}.slnx
├── Directory.Build.props
├── Directory.Packages.props
├── global.json
├── package.json                         # npm workspace root
├── biome.json
├── tsconfig.json
└── .editorconfig
```

## Host .csproj Transformation

Source: `template/SimpleModule.Host/SimpleModule.Host.csproj`

Transformations:
- Rename `SimpleModule` → `{ProjectName}` everywhere
- **Strip all module ProjectReferences** (Dashboard, Users, OpenIddict, etc.)
- **Strip ServiceDefaults reference** (Aspire-specific)
- **Strip the Import** for `SimpleModule.Hosting.targets` (build infrastructure for the monorepo)
- **Strip InternalsVisibleTo** (project-specific)
- **Strip NoWarn suppressions** (project-specific TODOs)
- **Strip EF Core Design package** (not needed for starter)
- **Keep**: Framework reference to `SimpleModule.Hosting` and Generator analyzer reference (paths adjusted to point to NuGet packages or framework — TBD based on how the new project references the framework)
- **Add**: ProjectReference to `Home` module

### Framework Reference Strategy

The Host.csproj references `SimpleModule.Hosting` and `SimpleModule.Generator` via relative project references. For a new project, these need to point to the framework:

**Option A (project reference to framework):** New project is created inside the SimpleModule repo structure. References are relative paths to `framework/`.

**Decision:** Use project references. The `sm` CLI already requires being run from within the repo. The new project is created as a sibling or child directory. Relative paths are adjusted accordingly.

## Host Files — Copy & Transform

| Source File | Transform |
|-------------|-----------|
| `Program.cs` | Copy as-is (uses `AddSimpleModule()` / `UseSimpleModule()` which are generated) |
| `Components/App.razor` | Replace `SimpleModule` in title and component references |
| `Components/InertiaShell.razor` | Copy as-is (uses framework component) |
| `Components/Routes.razor` | Remove Users module assembly reference |
| `Components/_Imports.razor` | Replace namespace `SimpleModule.Host` → `{ProjectName}.Host` |
| `ClientApp/app.tsx` | Copy as-is (no project-specific references) |
| `ClientApp/vite.config.ts` | Copy as-is |
| `ClientApp/validate-pages.mjs` | Copy as-is |
| `ClientApp/package.json` | Replace `@simplemodule/app` → `@{projectname}/app` |
| `Styles/app.css` | Remove `_scan/` import line. Keep Tailwind, theme, UI, and module source lines. Adjust relative paths. |
| `Properties/launchSettings.json` | Replace `SimpleModule` → `{ProjectName}` |
| `appsettings.json` | Copy as-is |
| `appsettings.Development.json` | Copy as-is |

Files **NOT** copied:
- `wwwroot/` contents (built artifacts — start empty, user runs `npm run build`)
- `Styles/_scan/` (pre-built CSS scans for existing modules)
- `Migrations/` (no migrations for a fresh project)
- `HostDbContextFactory.cs` (not needed until user wants migrations)

## Root Config Files

| File | Source | Transform |
|------|--------|-----------|
| `{ProjectName}.slnx` | Generate new (not from existing) | Contains Host + Home module + Tests.Shared |
| `Directory.Build.props` | Copy from repo root | Strip Roslynator (existing logic) |
| `Directory.Packages.props` | Copy from repo root | Strip project-specific packages (existing logic) |
| `global.json` | Copy from repo root | As-is |
| `package.json` | Generate new | Workspace root pointing to `src/{ProjectName}.Host/ClientApp` and `src/modules/*/src/*` |
| `biome.json` | Copy from repo root | Adjust paths for new structure |
| `tsconfig.json` | Copy from repo root | As-is |
| `.editorconfig` | Copy from repo root | As-is |

## Starter Module ("Home")

Created using the existing `ModuleTemplates` / `NewModuleCommand` logic. The module includes:
- Contracts project with a sample DTO and event
- Module implementation with DbContext, service, endpoint
- `Pages/index.ts` page registry
- Test project with unit and integration test skeletons

After creation, the module is registered in the Host .csproj and .slnx.

## Implementation Approach

1. **New class `HostTemplates`** — handles reading/transforming template Host files
2. **Refactor `NewProjectCommand`** — replaces current bare skeleton with:
   - Create root directory + config files
   - Copy & transform Host project files
   - Call module creation logic for "Home"
   - Create Tests.Shared
3. **Update `ProjectTemplates`** — add methods for new root config files (package.json, biome.json, tsconfig.json, .editorconfig)
4. **Reuse existing infrastructure** — `SlnxManipulator`, `ProjectManipulator`, `ModuleTemplates`

## Post-Scaffold Next Steps (shown to user)

```
cd {ProjectName}
npm install
npm run build
dotnet build
dotnet run --project src/{ProjectName}.Host
```

## Out of Scope

- NuGet package publishing for the framework
- Template Engine (`dotnet new`) template
- Interactive module name prompt (fixed as "Home")
- React view scaffolding within the starter module (just Pages/index.ts + endpoint)
