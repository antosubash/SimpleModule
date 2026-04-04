# SimpleModule.Users

User management module for [SimpleModule](https://github.com/antosubash/SimpleModule) — a modular monolith framework for .NET.

## Features

- User authentication and identity via ASP.NET Core Identity
- User and role management
- Email configuration for account verification
- Account security settings
- Cross-module user access via contracts

## Installation

```bash
sm install SimpleModule.Users
```

Or via .NET CLI:

```bash
dotnet add package SimpleModule.Users
```

## Usage

The module is auto-discovered by the SimpleModule framework. Use `IUsersContracts` to access user data from other modules.

## License

MIT
