# SimpleModule.Localization

Localization module for [SimpleModule](https://github.com/antosubash/SimpleModule) — a modular monolith framework for .NET.

## Features

- Multi-language support with JSON-based translations
- Locale resolution middleware
- String localizer factory integration
- Per-module translation file loading
- Frontend translation support via Inertia.js props

## Installation

```bash
sm install SimpleModule.Localization
```

Or via .NET CLI:

```bash
dotnet add package SimpleModule.Localization
```

## Usage

The module is auto-discovered by the SimpleModule framework. Add JSON translation files to your modules' `Locales/` directories.

## License

MIT
