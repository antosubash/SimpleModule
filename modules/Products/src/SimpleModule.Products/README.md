# SimpleModule.Products

Product catalog module for [SimpleModule](https://github.com/antosubash/SimpleModule) — a modular monolith framework for .NET.

## Features

- Full product CRUD with browse, create, edit, and delete
- Bulk import support
- Advanced tiered pricing (behind feature flag)
- Data seeding for development
- 9 endpoints for product management

## Installation

```bash
sm install SimpleModule.Products
```

Or via .NET CLI:

```bash
dotnet add package SimpleModule.Products
```

## Usage

The module is auto-discovered by the SimpleModule framework. Use `IProductsContracts` to interact with products from other modules.

## License

MIT
