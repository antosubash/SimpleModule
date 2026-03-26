# SimpleModule.Storage.S3

[![NuGet](https://img.shields.io/nuget/v/SimpleModule.Storage.S3.svg)](https://www.nuget.org/packages/SimpleModule.Storage.S3)

AWS S3 storage provider for SimpleModule.Storage.

## Installation

```bash
dotnet add package SimpleModule.Storage.S3
```

## Quick Start

```csharp
builder.Services.AddS3Storage(options =>
{
    options.BucketName = "my-app-uploads";
    options.Region = "us-east-1";
});
```

## Key Features

- **Upload, download, list, and delete** files in S3 buckets
- **Implements IStorageProvider** from SimpleModule.Storage
- **AWS SDK** integration via AWSSDK.S3

## Links

- [GitHub Repository](https://github.com/antosubash/SimpleModule)
