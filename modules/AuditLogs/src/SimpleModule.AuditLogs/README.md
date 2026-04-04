# SimpleModule.AuditLogs

Audit logging module for [SimpleModule](https://github.com/antosubash/SimpleModule) — a modular monolith framework for .NET.

## Features

- Captures HTTP request audit trails automatically via middleware
- Tracks domain events and entity changes
- Configurable retention policies
- Browse and search audit log entries via built-in UI
- Query parameters and request body tracking

## Installation

```bash
sm install SimpleModule.AuditLogs
```

Or via .NET CLI:

```bash
dotnet add package SimpleModule.AuditLogs
```

## Usage

The module is auto-discovered by the SimpleModule framework. Once installed, it begins capturing audit trails for all authenticated requests.

## License

MIT
