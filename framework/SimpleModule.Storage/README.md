# SimpleModule.Storage

[![NuGet](https://img.shields.io/nuget/v/SimpleModule.Storage.svg)](https://www.nuget.org/packages/SimpleModule.Storage)

Storage abstraction for SimpleModule with a pluggable provider interface for file storage backends.

## Installation

```bash
dotnet add package SimpleModule.Storage
```

## Quick Start

```csharp
public class MyService(IStorageProvider storage)
{
    public async Task UploadAsync(Stream file, string path)
    {
        await storage.UploadAsync(path, file);
    }
}
```

## Available Providers

| Provider | Package |
|----------|---------|
| Local filesystem | `SimpleModule.Storage.Local` |
| Azure Blob Storage | `SimpleModule.Storage.Azure` |
| AWS S3 | `SimpleModule.Storage.S3` |

## Key Features

- **IStorageProvider** interface for upload, download, list, and delete operations
- **Provider-agnostic** -- swap storage backends without changing application code
- **Stream-based API** for efficient large file handling

## Links

- [GitHub Repository](https://github.com/antosubash/SimpleModule)
