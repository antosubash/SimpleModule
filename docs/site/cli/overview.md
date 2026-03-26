---
outline: deep
---

# CLI Overview

The `sm` CLI tool streamlines common development tasks in SimpleModule projects. It scaffolds new solutions, modules, and features, and validates your project structure against framework conventions.

## Installation

Install the CLI as a global .NET tool:

```bash
dotnet tool install -g SimpleModule.Cli
```

Once installed, the `sm` command is available globally from any terminal.

## Commands

| Command | Description |
|---------|-------------|
| `sm new project [name]` | Scaffold a new SimpleModule solution |
| `sm new module [name]` | Create a module with contracts, endpoints, and tests |
| `sm new feature [name]` | Add a feature endpoint to an existing module |
| `sm doctor [--fix]` | Validate project structure and conventions |

All `sm new` commands support interactive prompts. If you omit required arguments, the CLI will ask for them using an interactive selection UI powered by [Spectre.Console](https://spectreconsole.net/).

## How It Works

The CLI is built with `Spectre.Console.Cli` and packaged as a .NET tool (`PackAsTool`). It discovers your solution by searching for a `.slnx` file in the current directory or parent directories. Most commands require being run from within an existing SimpleModule project.

::: tip
Run `sm doctor` after creating modules or making structural changes to verify everything is wired up correctly.
:::

## Next Steps

- [Scaffold a new project](./new-project) with `sm new project`
- [Create a module](./new-module) with `sm new module`
- [Add a feature](./new-feature) with `sm new feature`
- [Validate your project](./doctor) with `sm doctor`
