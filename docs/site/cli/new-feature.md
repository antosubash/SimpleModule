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

## What Gets Created

Running `sm new feature UpdateInvoice --module Invoices --method PUT --route /{id} --validator` generates:

```
modules/Invoices/src/Invoices/Endpoints/Invoices/
  UpdateInvoiceEndpoint.cs             # IEndpoint implementation
  UpdateInvoiceRequestValidator.cs     # Request validator (when --validator is used)
```

### Endpoint Auto-Discovery

The generated endpoint implements `IEndpoint`, which the Roslyn source generator scans at compile time. There is no need to manually register routes -- just build the project and the endpoint is live.

### Validator (Optional)

When you pass `--validator`, the CLI generates a companion validator class that validates the request before the endpoint logic runs.

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
If your feature is a page (view endpoint using `Inertia.Render`), remember to add a corresponding entry in your module's `Pages/index.ts`. See the [Pages Registry Pattern](/guide/modules#pages-registry) for details.
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
