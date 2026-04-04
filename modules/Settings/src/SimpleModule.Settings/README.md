# SimpleModule.Settings

Settings management module for [SimpleModule](https://github.com/antosubash/SimpleModule) — a modular monolith framework for .NET.

## Features

- Application-scoped and user-scoped configuration settings
- Theme, language, and timezone preferences
- System settings (maintenance mode, registration toggles)
- Settings UI with built-in views
- Cross-module settings access via contracts

## Installation

```bash
sm install SimpleModule.Settings
```

Or via .NET CLI:

```bash
dotnet add package SimpleModule.Settings
```

## Usage

The module is auto-discovered by the SimpleModule framework. Use `ISettingsContracts` to read and write settings from other modules.

## License

MIT
