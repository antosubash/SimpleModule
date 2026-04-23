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

## What Gets Created

Running `sm new project MyApp` generates the following structure:

```
MyApp/
  MyApp.slnx                      # Solution file
  Directory.Build.props            # Shared MSBuild properties
  Directory.Packages.props         # Central package management
  global.json                      # SDK version pinning
  src/
    MyApp.Api/
      MyApp.Api.csproj             # Host/API project
      Program.cs                   # Entry point with generated extensions
    MyApp.Core/
      MyApp.Core.csproj            # Core framework (IModule, IEndpoint, etc.)
    MyApp.Database/
      MyApp.Database.csproj        # Database infrastructure
    MyApp.Generator/
      MyApp.Generator.csproj       # Roslyn source generator (netstandard2.0)
    modules/                       # Empty directory for modules
  tests/
    MyApp.Tests.Shared/
      MyApp.Tests.Shared.csproj    # Shared test infrastructure
```

## Project Details

- **Api** -- the host application that calls generated `AddModules()` and `MapModuleEndpoints()` extension methods
- **Core** -- defines the `IModule` interface, `[Module]` attribute, `IEndpoint`, `[Dto]`, events (`IEvent` + Wolverine), and menu system
- **Database** -- multi-provider database support with schema isolation per module
- **Generator** -- Roslyn incremental source generator targeting `netstandard2.0` for compile-time module discovery
- **Tests.Shared** -- `WebApplicationFactory` base class, fake data generators, and test authentication

## After Scaffolding

```bash
cd MyApp
sm new module Products        # create your first module
dotnet build                  # build the solution
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
