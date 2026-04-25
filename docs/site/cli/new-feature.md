---
outline: deep
---

# sm new feature

Adds a new endpoint to an existing module. The generated endpoint implements `IEndpoint` and is automatically discovered by the Roslyn source generator -- no manual registration needed.

## Usage

```bash
sm new feature [name]
```

If you omit required options, the CLI prompts you interactively with selection menus.

## Options

| Option | Description |
|--------|-------------|
| `[name]` | Feature name in PascalCase (e.g., `UpdateInvoice`). Prompted if omitted. |
| `-m, --module <name>` | Target module name. If omitted, presents a selection list of existing modules. |
| `--method <method>` | HTTP method: `GET`, `POST`, `PUT`, or `DELETE`. Prompted if omitted. |
| `-r, --route <pattern>` | Route pattern (e.g., `/{id}`). Defaults to `/{id}` when prompted. |
| `--validator` | Include a request validator class alongside the endpoint. |
| `--no-view` | Skip creating the React view component and its `Pages/index.ts` entry. |
| `--dry-run` | Preview the files that would be created or modified without writing anything to disk. |

## What Gets Created

Running `sm new feature UpdateInvoice --module Invoices --method PUT --route /{id} --validator` generates:

```
src/modules/Invoices/src/Invoices/
  Endpoints/Invoices/
    UpdateInvoiceEndpoint.cs           # IEndpoint implementation
    UpdateInvoiceRequestValidator.cs   # Request validator (when --validator is used)
  Views/
    UpdateInvoice.tsx                  # React view component (unless --no-view)
  Pages/index.ts                       # Updated with a new entry mapping "Invoices/UpdateInvoice" to the view
```

### Endpoint Auto-Discovery

The generated endpoint implements `IEndpoint`, which the Roslyn source generator scans at compile time. There is no need to manually register routes -- just build the project and the endpoint is live.

### Validator (Optional)

When you pass `--validator`, the CLI generates a companion validator class that validates the request before the endpoint logic runs.

### View + Pages Registry (Default)

By default the CLI also creates `Views/{Feature}.tsx` and appends an entry to the module's `Pages/index.ts`, e.g.:

```ts
'Invoices/UpdateInvoice': () => import('@/Views/UpdateInvoice'),
```

Pass `--no-view` to skip both steps (useful for pure API endpoints that never render a page).

## Interactive Mode

When run without flags, the CLI walks you through each option:

```bash
sm new feature
# ? Feature name (PascalCase): UpdateInvoice
# ? Select a module: Invoices
# ? HTTP method: PUT
# ? Route pattern: /{id}
# ? Include a validator? Yes
```

## Requirements

- Must be run from within a SimpleModule project (the CLI looks for a `.slnx` file)
- At least one module must exist. If no modules are found, the CLI directs you to run `sm new module` first

::: tip View Endpoints
`sm new feature` automatically adds the `Pages/index.ts` entry for the view it scaffolds. If you later add an `IViewEndpoint` by hand (or pass `--no-view`), you must register it yourself. See the [Pages Registry Pattern](/guide/modules#pages-registry) for details.
:::

## Example

```bash
# Fully specified
sm new feature CreateInvoice --module Invoices --method POST --route / --validator

# Interactive
sm new feature
```

## Next Steps

- [Endpoints](/guide/endpoints) -- API and view endpoint patterns in detail
- [Pages Registry](/frontend/pages) -- register your new page component
- [sm doctor](/cli/doctor) -- validate everything is wired correctly
