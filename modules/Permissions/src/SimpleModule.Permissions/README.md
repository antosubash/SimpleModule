# SimpleModule.Permissions

Permission management module for [SimpleModule](https://github.com/antosubash/SimpleModule) — a modular monolith framework for .NET.

## Features

- Role-based access control (RBAC) with database persistence
- Permission seeding for initial setup
- Cross-module authorization via contracts
- Permission assignment to roles
- Fine-grained endpoint authorization

## Installation

```bash
sm install SimpleModule.Permissions
```

Or via .NET CLI:

```bash
dotnet add package SimpleModule.Permissions
```

## Usage

The module is auto-discovered by the SimpleModule framework. Use `IPermissionsContracts` to check permissions from other modules.

## License

MIT
