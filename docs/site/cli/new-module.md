---
outline: deep
---

# sm new module

Creates a new module following the three-project pattern: implementation, contracts, and tests. The CLI also wires the module into the solution file and adds a project reference to the host.

## Usage

```bash
sm new module [name]
```

If you omit the name, the CLI prompts you interactively.

## Options

| Option | Description |
|--------|-------------|
| `[name]` | Module name in PascalCase (e.g., `Invoices`). Must start with an uppercase letter. Prompted if omitted. |

## What Gets Created

Running `sm new module Invoices` generates the following structure. The CLI automatically derives the singular name (e.g., `Invoices` becomes `Invoice`) for class names.

### Contracts Project

```
modules/Invoices/src/Invoices.Contracts/
  Invoices.Contracts.csproj        # References Core only
  IInvoiceContracts.cs             # Public contract interface
  Invoice.cs                       # [Dto] type for cross-module use
  Events/
    InvoiceCreatedEvent.cs         # Domain event
```

### Implementation Project

```
modules/Invoices/src/Invoices/
  Invoices.csproj                  # References Core + Contracts
  InvoicesModule.cs                # IModule with [Module("Invoices")]
  InvoicesConstants.cs             # Module constants (permissions, etc.)
  InvoicesDbContext.cs             # EF Core DbContext
  InvoiceService.cs                # Service implementing IInvoiceContracts
  Endpoints/Invoices/
    GetAllEndpoint.cs              # IEndpoint (auto-discovered)
```

### Test Project

```
modules/Invoices/tests/Invoices.Tests/
  Invoices.Tests.csproj            # xUnit test project
  GlobalUsings.cs                  # Common test usings
  Unit/
    InvoiceServiceTests.cs         # Service unit test skeleton
  Integration/
    InvoicesEndpointTests.cs       # Endpoint integration test skeleton
```

## Three-Project Pattern

Each module follows a strict separation:

| Project | Purpose | Dependencies |
|---------|---------|-------------|
| `Invoices.Contracts` | Public API surface -- interfaces, DTOs, events | Core only |
| `Invoices` | Implementation -- services, DbContext, endpoints | Core + Contracts |
| `Invoices.Tests` | Unit and integration tests | Implementation + Tests.Shared |

This ensures modules communicate only through contracts, never through implementation details.

## Automatic Wiring

After creating the files, the CLI automatically:

1. **Adds entries to `.slnx`** -- all three projects appear in the solution with proper folder grouping
2. **Adds a `ProjectReference`** in the host/API `.csproj` pointing to the module's implementation project

This means the module is immediately discoverable by the Roslyn source generator on the next build.

## Post-Creation Steps

After the CLI finishes:

```bash
dotnet build                  # source generator discovers the new module
dotnet run --project src/MyApp.Api
```

::: tip
Run `sm doctor` after creating a module to verify all references and solution entries are correct.
:::

## Example

```bash
# Interactive mode
sm new module

# Direct mode
sm new module Invoices
```
