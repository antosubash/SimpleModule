# SimpleModule.Orders

Order management module for [SimpleModule](https://github.com/antosubash/SimpleModule) — a modular monolith framework for .NET.

## Features

- Order creation and lifecycle management
- Order listing and detail views
- Data seeding for development
- Menu item registration
- Cross-module communication via contracts

## Installation

```bash
sm install SimpleModule.Orders
```

Or via .NET CLI:

```bash
dotnet add package SimpleModule.Orders
```

## Usage

The module is auto-discovered by the SimpleModule framework. Use `IOrdersContracts` to interact with orders from other modules.

## License

MIT
