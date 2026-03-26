# SimpleModule.Storage.Local

[![NuGet](https://img.shields.io/nuget/v/SimpleModule.Storage.Local.svg)](https://www.nuget.org/packages/SimpleModule.Storage.Local)

Local filesystem storage provider for SimpleModule.Storage.

## Installation

```bash
dotnet add package SimpleModule.Storage.Local
```

## Quick Start

```csharp
builder.Services.AddLocalStorage(options =>
{
    options.RootPath = Path.Combine(builder.Environment.ContentRootPath, "uploads");
});
```

## Key Features

- **Stores files on the local disk** for development and single-server deployments
- **Implements IStorageProvider** from SimpleModule.Storage
- **Zero external dependencies** -- no cloud SDK required

## Links

- [GitHub Repository](https://github.com/antosubash/SimpleModule)
