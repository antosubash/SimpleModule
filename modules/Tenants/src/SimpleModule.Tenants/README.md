# SimpleModule.Tenants

Multi-tenancy module for [SimpleModule](https://github.com/antosubash/SimpleModule) — a modular monolith framework for .NET.

## Features

- Tenant resolution by hostname
- Tenant-scoped services and data isolation
- Tenant management dashboard
- Tenant creation and configuration
- Cross-module tenant awareness via contracts

## Installation

```bash
sm install SimpleModule.Tenants
```

Or via .NET CLI:

```bash
dotnet add package SimpleModule.Tenants
```

## Usage

The module is auto-discovered by the SimpleModule framework. Configure tenant resolution strategy via module options.

## License

MIT
