# SimpleModule.Agents.Module

AI agent persistence module for [SimpleModule](https://github.com/antosubash/SimpleModule) — a modular monolith framework for .NET.

## Features

- Database-backed session and message persistence for AI agents
- EF Core integration for agent data storage
- Contract-based service interface for cross-module communication

## Installation

```bash
sm install SimpleModule.Agents.Module
```

Or via .NET CLI:

```bash
dotnet add package SimpleModule.Agents.Module
```

## Usage

The module is auto-discovered by the SimpleModule framework. It provides `IAgentsContracts` for other modules to interact with agent sessions and messages.

## License

MIT
