# SimpleModule.FeatureFlags

Feature flag management module for [SimpleModule](https://github.com/antosubash/SimpleModule) — a modular monolith framework for .NET.

## Features

- Create and manage feature flags with in-memory caching
- Toggle features without redeployment
- Sync capabilities across instances
- Middleware integration for flag evaluation
- API endpoints for flag management

## Installation

```bash
sm install SimpleModule.FeatureFlags
```

Or via .NET CLI:

```bash
dotnet add package SimpleModule.FeatureFlags
```

## Usage

The module is auto-discovered by the SimpleModule framework. Use `IFeatureFlagContracts` to check flag status from other modules.

## License

MIT
