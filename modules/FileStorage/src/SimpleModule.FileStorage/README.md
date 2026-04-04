# SimpleModule.FileStorage

File storage module for [SimpleModule](https://github.com/antosubash/SimpleModule) — a modular monolith framework for .NET.

## Features

- File upload and download with configurable storage providers
- Configurable file size limits and allowed extensions
- Permission-based access control
- Folder organization and management
- Browse and manage files via built-in UI

## Installation

```bash
sm install SimpleModule.FileStorage
```

Or via .NET CLI:

```bash
dotnet add package SimpleModule.FileStorage
```

## Usage

The module is auto-discovered by the SimpleModule framework. Configure storage providers (local, S3, Azure) via module options.

## License

MIT
