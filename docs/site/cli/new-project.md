---
outline: deep
---

# sm new project

Scaffolds a complete SimpleModule solution with all the foundational projects and configuration files.

## Usage

```bash
sm new project [name]
```

If you omit the name, the CLI prompts you interactively.

## Options

| Option | Description |
|--------|-------------|
| `[name]` | Project name in PascalCase (e.g., `MyApp`). Prompted if omitted. |
| `-o, --output <dir>` | Output directory. Defaults to the current directory. |
| `--dry-run` | Preview the files that would be created without writing anything to disk. |
| `--framework-version <version>` | Override the auto-resolved SimpleModule NuGet package version. |

## What Gets Created

Running `sm new project MyApp` generates the following structure:

```
MyApp/
  MyApp.slnx                         # Solution file
  Directory.Build.props              # Shared MSBuild properties
  Directory.Packages.props           # Central package management
  global.json                        # SDK version pinning
  nuget.config                       # NuGet feed configuration
  package.json                       # npm workspace root
  biome.json                         # Biome lint + format config
  tsconfig.json                      # Shared TypeScript config
  .editorconfig                      # Editor/style rules
  src/
    MyApp.Host/
      MyApp.Host.csproj              # Host project (references framework NuGet packages)
      Program.cs                     # Entry point with generated extensions
      ClientApp/                     # React + Inertia bootstrap
      Styles/                        # Tailwind entry
      Properties/launchSettings.json
      wwwroot/index.html             # Inertia static shell
    modules/
      Items/                         # Starter module scaffolded by default
        src/
          Items.Contracts/
          Items/                     # Module, DbContext, service, endpoints, Views, Pages, vite.config, package.json
        tests/
          Items.Tests/
  tests/
    MyApp.Tests.Shared/
      MyApp.Tests.Shared.csproj      # Shared test infrastructure
```

## Project Details

- **Host** -- the host application that calls generated `AddModules()` and `MapModuleEndpoints()` extension methods; serves the Inertia + React frontend
- **Items module** -- a starter module under `src/modules/Items/` demonstrating the three-project pattern (contracts, implementation, tests) plus the frontend conventions (`Views/`, `Pages/index.ts`, `vite.config.ts`, `package.json`)
- **Tests.Shared** -- `WebApplicationFactory` base class, fake data generators, and test authentication

Framework code (`Core`, `Database`, `Generator`) is consumed from NuGet packages rather than scaffolded into your repo.

## After Scaffolding

```bash
cd MyApp
sm new module Products        # add another module under src/modules/
dotnet build                  # build the solution
dotnet run --project src/MyApp.Host
```

::: warning
The CLI will refuse to create a project in a non-empty directory. Choose a clean location or use the `--output` flag to specify a different path.
:::

## Example

```bash
# Create in current directory
sm new project MyApp

# Create in a specific directory
sm new project MyApp --output ~/projects
```

## Next Steps

- [sm new module](/cli/new-module) -- add a module to your project
- [Quick Start](/getting-started/quick-start) -- build and run your new project
- [Project Structure](/getting-started/project-structure) -- understand the generated layout
