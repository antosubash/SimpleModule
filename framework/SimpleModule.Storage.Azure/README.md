# SimpleModule.Storage.Azure

[![NuGet](https://img.shields.io/nuget/v/SimpleModule.Storage.Azure.svg)](https://www.nuget.org/packages/SimpleModule.Storage.Azure)

Azure Blob Storage provider for SimpleModule.Storage.

## Installation

```bash
dotnet add package SimpleModule.Storage.Azure
```

## Quick Start

```csharp
builder.Services.AddAzureBlobStorage(options =>
{
    options.ConnectionString = "your-connection-string";
    options.ContainerName = "uploads";
});
```

## Key Features

- **Upload, download, list, and delete** files in Azure Blob containers
- **Implements IStorageProvider** from SimpleModule.Storage
- **Azure SDK** integration via Azure.Storage.Blobs

## Links

- [GitHub Repository](https://github.com/antosubash/SimpleModule)
