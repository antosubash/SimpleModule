# CLI Improvements Design

**Date:** 2026-03-26
**Branch:** claude/strange-heyrovsky
**Scope:** Three improvement areas — comprehensive doctor checks, smarter `sm new feature` scaffolding, and UX polish.

---

## 1. Comprehensive Doctor Checks

### Goal

`sm doctor` becomes the authoritative validator for the entire project — catching every convention violation described in CLAUDE.md before it causes a silent failure.

### New Checks

All new checks implement the existing `IDoctorCheck` interface, returning `IEnumerable<CheckResult>`. They are added to the `checks` array in `DoctorCommand.cs`.

| Check Class | What It Validates | Severity | Auto-fixable |
|---|---|---|---|
| `PagesRegistryCheck` | Every `Inertia.Render("X/Y", ...)` call in C# has a matching key in the module's `Pages/index.ts` | FAIL | Yes — adds stub entry |
| `ViteConfigCheck` | Each module has a `vite.config.ts`, uses library mode, externalizes React, ReactDOM, and `@inertiajs/react` | WARN | No |
| `PackageJsonCheck` | Module `package.json` lists React and `@inertiajs/react` as `peerDependencies`, not `dependencies` | WARN | No |
| `NpmWorkspaceCheck` | Each module directory is covered by the root `package.json` `workspaces` glob | FAIL | Yes — adds workspace glob |
| `ModuleAttributeCheck` | The module class (implements `IModule`) has `[Module(...)]` attribute with a non-empty `RoutePrefix` | FAIL | No |
| `ViewEndpointNamingCheck` | Endpoint classes end in `Endpoint`, are located under an `Endpoints/` directory | WARN | No |
| `ContractsIsolationCheck` | Contracts `.csproj` references only `SimpleModule.Core` — no references to other modules' impl projects | FAIL | No |

### PagesRegistryCheck Implementation

1. For each module, scan all `.cs` files under `src/{ModuleName}/` with regex: `Inertia\.Render\s*\(\s*"([^"]+)"`
2. Parse `Pages/index.ts` with regex matching known key patterns: `['"\`]([^'"\`]+)['"\`]\s*:\s*(?:\(\s*\)|(?:async\s*)?\(\s*\)\s*=>|import)`
3. Diff the two sets; report each missing key as a FAIL result named `"Pages -> {ModuleName}/{ComponentName}"`

**Auto-fix:** When `--fix` is set, append a stub entry to the `pages` object in `Pages/index.ts`:
```typescript
"Products/Create": () => import("../Views/Create"),
```

### Auto-fix Registration

`DoctorCommand` already matches `CheckResult.Name` patterns to dispatch fixes. Extend the pattern matching:
- `"Pages -> {module}/{component}"` → call new `PagesRegistryFixer`
- `"NpmWorkspace -> {module}"` → call new `NpmWorkspaceFixer`

---

## 2. Smarter `sm new feature` Scaffolding

### Goal

When a view endpoint is created, the CLI fully wires it up — both backend (C# endpoint) and frontend (React component + Pages registry entry) — so the developer never hits the silent 404 described in CLAUDE.md.

### Artifacts Created

Given `sm new feature Create --module Products --method POST --route /`:

| Artifact | Action | Location |
|---|---|---|
| `CreateEndpoint.cs` | Create (existing) | `src/modules/Products/src/Products/Endpoints/Products/` |
| `CreateValidator.cs` | Create (existing, if `--validator`) | `src/modules/Products/src/Products/Endpoints/Products/` |
| `Create.tsx` | Create (new) | `src/modules/Products/src/Products/Views/` |
| `Pages/index.ts` | Modify (new) | `src/modules/Products/src/Products/Pages/` |

### Views/Create.tsx Template

```tsx
import type { InferPageProps } from '@inertiajs/react'

type Props = {
    // TODO: add props from your endpoint's response
}

export default function Create({ }: Props) {
    return (
        <div>
            <h1>Create</h1>
        </div>
    )
}
```

Component name and heading are derived from the feature name.

### Pages/index.ts Update Strategy

1. Read existing `Pages/index.ts`
2. Find the last `}` closing the `pages` object
3. Insert the new entry on the line before it, matching existing indentation
4. If `Pages/index.ts` does not exist, create it from scratch:

```typescript
export const pages: Record<string, any> = {
    "Products/Create": () => import("../Views/Create"),
}
```

Entry key format: `"{ModuleName}/{FeatureName}"` — matches the `Inertia.Render` call pattern.

### `--no-view` Flag

Skips creation of `Views/{FeatureName}.tsx` and the `Pages/index.ts` update. For pure API endpoints that render no React page.

---

## 3. UX Polish

### Dry-Run Mode

All `new` commands (`new project`, `new module`, `new feature`) gain a `--dry-run` flag. When set:

1. Settings resolve interactively as normal (prompts still run)
2. Instead of writing files, print a tree showing what would happen
3. Exit without writing anything

Example output:
```
Dry run — no files written

  modules/Products/
  ├── src/Products/
  │   ├── Endpoints/Products/
  │   │   └── CreateEndpoint.cs         [create]
  │   └── Views/
  │       └── Create.tsx                [create]
  └── Pages/
      └── index.ts                      [modify]
```

Implementation: commands accumulate a `List<(string path, FileAction action)>` during execution. In dry-run mode, render this list as a Spectre.Console `Tree` instead of calling `File.WriteAllText`.

`FileAction` enum: `Create`, `Modify`.

### Tree Output on Real Execution

After successful `sm new feature` or `sm new module`, replace the current flat `+ file.cs` list with a Spectre.Console `Tree` of created/modified files, grouped by directory. Same information, easier to scan.

### Better Error Messages

When validation fails, include a concrete suggestion:

| Error | Current | Improved |
|---|---|---|
| Non-PascalCase name | `Name must be PascalCase` | `'products' is not PascalCase. Did you mean 'Products'?` |
| Module not found | `Module 'xyz' not found` | `Module 'xyz' not found. Available modules: Products, Orders, Users` |
| Solution not found | `No .slnx file found` | `No .slnx file found. Run this command from inside a SimpleModule project.` |

### Progress Spinner

`sm new module` wraps its multi-step execution in a Spectre.Console `Status` spinner:

```
⠋ Creating module structure...
✓ Done
```

Steps: create directories → write C# files → update .slnx → update Host .csproj → write frontend files.

---

## Out of Scope

- Version checks / update notifications (no NuGet publish pipeline yet)
- Full AST-based csproj editing (regex + XDocument is sufficient)
- New top-level commands beyond `new` and `doctor`
- DtoAttributeCheck (removed per user request)

---

## File Changes Summary

### New files
- `cli/SimpleModule.Cli/Commands/Doctor/Checks/PagesRegistryCheck.cs`
- `cli/SimpleModule.Cli/Commands/Doctor/Checks/ViteConfigCheck.cs`
- `cli/SimpleModule.Cli/Commands/Doctor/Checks/PackageJsonCheck.cs`
- `cli/SimpleModule.Cli/Commands/Doctor/Checks/NpmWorkspaceCheck.cs`
- `cli/SimpleModule.Cli/Commands/Doctor/Checks/ModuleAttributeCheck.cs`
- `cli/SimpleModule.Cli/Commands/Doctor/Checks/ViewEndpointNamingCheck.cs`
- `cli/SimpleModule.Cli/Commands/Doctor/Checks/ContractsIsolationCheck.cs`
- `cli/SimpleModule.Cli/Infrastructure/PagesRegistryFixer.cs`
- `cli/SimpleModule.Cli/Infrastructure/NpmWorkspaceFixer.cs`

### Modified files
- `cli/SimpleModule.Cli/Commands/Doctor/DoctorCommand.cs` — register new checks + new fix dispatchers
- `cli/SimpleModule.Cli/Commands/New/NewFeatureCommand.cs` — add Views + Pages/index.ts generation, tree output, dry-run
- `cli/SimpleModule.Cli/Commands/New/NewFeatureSettings.cs` — add `--no-view`, `--dry-run` flags
- `cli/SimpleModule.Cli/Commands/New/NewModuleCommand.cs` — add spinner, tree output, dry-run
- `cli/SimpleModule.Cli/Commands/New/NewModuleSettings.cs` — add `--dry-run` flag
- `cli/SimpleModule.Cli/Commands/New/NewProjectCommand.cs` — add dry-run
- `cli/SimpleModule.Cli/Commands/New/NewProjectSettings.cs` — add `--dry-run` flag
- `cli/SimpleModule.Cli/Templates/FeatureTemplates.cs` — add `ViewComponent()` template method
- `cli/SimpleModule.Cli/Infrastructure/SolutionContext.cs` — add `GetModuleViewsPath()`, `GetModulePagesIndexPath()` helpers
- `cli/SimpleModule.Cli/Infrastructure/FileAction.cs` — new `FileAction` enum (`Create`, `Modify`) used by dry-run tree rendering

### New test files
- `tests/SimpleModule.Cli.Tests/PagesRegistryCheckTests.cs`
- `tests/SimpleModule.Cli.Tests/ViteConfigCheckTests.cs`
- `tests/SimpleModule.Cli.Tests/PackageJsonCheckTests.cs`
- `tests/SimpleModule.Cli.Tests/NpmWorkspaceCheckTests.cs`
- `tests/SimpleModule.Cli.Tests/ContractsIsolationCheckTests.cs`
- `tests/SimpleModule.Cli.Tests/ModuleAttributeCheckTests.cs`
- `tests/SimpleModule.Cli.Tests/NewFeatureViewScaffoldingTests.cs`
- `tests/SimpleModule.Cli.Tests/DryRunTests.cs`
