---
outline: deep
---

# sm doctor

Validates your project structure and conventions against SimpleModule requirements. Reports issues as PASS, WARN, or FAIL, and can auto-fix common problems.

## Usage

```bash
sm doctor [--fix]
```

## Options

| Option | Description |
|--------|-------------|
| `--fix` | Auto-fix missing `.slnx` entries and project references |

## Checks Performed

The doctor command runs five categories of checks:

### 1. Solution Structure

Verifies the foundational directory layout exists:

| Check | Severity |
|-------|----------|
| `.slnx` solution file exists | FAIL |
| `src/` directory exists | FAIL |
| `src/modules/` directory exists | FAIL |
| `tests/` directory exists | FAIL |

### 2. Project References

For each discovered module, verifies the host/API `.csproj` has a `<ProjectReference>` to the module's implementation project.

| Check | Severity | Auto-fixable |
|-------|----------|-------------|
| API project references each module | FAIL | Yes |

### 3. Solution File Entries

For each discovered module, verifies the `.slnx` file contains folder entries for the module's projects (contracts, implementation, tests).

| Check | Severity | Auto-fixable |
|-------|----------|-------------|
| Module entries exist in `.slnx` | FAIL | Yes |

### 4. .csproj Conventions

Validates project file conventions for each module:

| Check | Severity |
|-------|----------|
| Module `.csproj` has `<FrameworkReference Include="Microsoft.AspNetCore.App" />` | FAIL |
| Contracts `.csproj` only references Core (not other modules) | WARN |
| Generator project targets `netstandard2.0` | FAIL |

### 5. Module Pattern

Validates that each module follows the expected file structure:

| Check | Severity |
|-------|----------|
| `{Module}Module.cs` exists | WARN |
| `{Module}Constants.cs` exists | WARN |
| `{Module}DbContext.cs` exists | WARN |
| `Endpoints/` directory exists | WARN |

## Auto-Fix Behavior

When you pass `--fix`, the doctor attempts to repair the following issues:

- **Missing `.slnx` entries** -- adds folder entries for the module's three projects
- **Missing project references** -- adds a `<ProjectReference>` in the host `.csproj` pointing to the module

After auto-fixing, all checks are re-run and the results table reflects the current state.

```bash
sm doctor --fix
# Attempting auto-fix...
#   Fixed: added Invoices to .slnx
#   Fixed: added Invoices reference to API csproj
```

::: info
Auto-fix only handles structural wiring (slnx entries and project references). It does not create missing files like `Module.cs` or `DbContext.cs` -- use `sm new module` for that.
:::

## Output

The doctor displays a table with three columns:

```
| Status | Check                  | Details                              |
|--------|------------------------|--------------------------------------|
| PASS   | Solution file          | .slnx exists                         |
| PASS   | Directory src/         | exists                               |
| FAIL   | API -> Invoices        | missing project reference in API     |
| WARN   | Invoices/Endpoints/    | Endpoints directory missing          |
```

The command exits with code `0` when all checks pass (warnings are allowed) and code `1` when any check fails.

## CI Integration

Add `sm doctor` to your CI pipeline to catch structural issues early:

```yaml
- name: Validate project structure
  run: sm doctor
```

The non-zero exit code on failure makes it suitable for CI gates. Pair it with `--fix` in a pre-commit hook for local development if desired.
