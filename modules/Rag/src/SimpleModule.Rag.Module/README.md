# SimpleModule.Rag.Module

Retrieval-Augmented Generation (RAG) persistence module for [SimpleModule](https://github.com/antosubash/SimpleModule) — a modular monolith framework for .NET.

## Features

- Database-backed storage for RAG data
- Structured knowledge cache integration
- Entity Framework persistence for RAG documents
- Contract-based service interface for cross-module use

## Installation

```bash
sm install SimpleModule.Rag.Module
```

Or via .NET CLI:

```bash
dotnet add package SimpleModule.Rag.Module
```

## Usage

The module is auto-discovered by the SimpleModule framework. It provides `IRagContracts` for other modules to store and retrieve RAG data.

## License

MIT
