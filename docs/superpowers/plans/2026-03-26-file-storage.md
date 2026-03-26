# FileStorage Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a general-purpose file storage system with pluggable providers (Local, Azure Blob, S3-compatible) and a file browser UI module.

**Architecture:** Framework-level `IStorageProvider` abstraction in `SimpleModule.Storage` with three provider packages (`Storage.Local`, `Storage.Azure`, `Storage.S3`). A `FileStorage` module consumes the abstraction, adds DB metadata tracking, permissions, settings, and a React file browser view.

**Tech Stack:** .NET 10, EF Core, Vogen, Azure.Storage.Blobs, AWSSDK.S3, React 19, Inertia.js, @simplemodule/ui

**Spec:** `docs/superpowers/specs/2026-03-26-file-storage-design.md`

---

## File Map

### Framework Projects

```
framework/SimpleModule.Storage/
├── SimpleModule.Storage.csproj
├── IStorageProvider.cs
├── StorageResult.cs
├── StorageEntry.cs
├── StorageOptions.cs
└── StoragePathHelper.cs

framework/SimpleModule.Storage.Local/
├── SimpleModule.Storage.Local.csproj
├── LocalStorageProvider.cs
├── LocalStorageOptions.cs
└── LocalStorageExtensions.cs

framework/SimpleModule.Storage.Azure/
├── SimpleModule.Storage.Azure.csproj
├── AzureBlobStorageProvider.cs
├── AzureBlobStorageOptions.cs
└── AzureBlobStorageExtensions.cs

framework/SimpleModule.Storage.S3/
├── SimpleModule.Storage.S3.csproj
├── S3StorageProvider.cs
├── S3StorageOptions.cs
└── S3StorageExtensions.cs
```

### Module Projects

```
modules/FileStorage/
├── src/
│   ├── FileStorage.Contracts/
│   │   ├── FileStorage.Contracts.csproj
│   │   ├── IFileStorageContracts.cs
│   │   ├── StoredFile.cs
│   │   └── FileStorageId.cs
│   └── FileStorage/
│       ├── FileStorage.csproj
│       ├── FileStorageModule.cs
│       ├── FileStorageConstants.cs
│       ├── FileStoragePermissions.cs
│       ├── FileStorageService.cs
│       ├── FileStorageDbContext.cs
│       ├── EntityConfigurations/
│       │   └── StoredFileConfiguration.cs
│       ├── Endpoints/
│       │   └── Files/
│       │       ├── GetAllEndpoint.cs
│       │       ├── GetByIdEndpoint.cs
│       │       ├── DownloadEndpoint.cs
│       │       ├── UploadEndpoint.cs
│       │       ├── DeleteEndpoint.cs
│       │       └── ListFoldersEndpoint.cs
│       ├── Views/
│       │   ├── Browse.tsx
│       │   └── BrowseEndpoint.cs
│       ├── Pages/
│       │   └── index.ts
│       ├── types.ts
│       ├── vite.config.ts
│       └── package.json
└── tests/
    └── FileStorage.Tests/
        ├── FileStorage.Tests.csproj
        ├── InMemoryStorageProvider.cs
        └── FileStorageServiceTests.cs
```

### Modified Files

```
SimpleModule.slnx                                  — add all new projects
template/SimpleModule.Host/SimpleModule.Host.csproj — add FileStorage + Storage.Local references
Directory.Packages.props                           — add Azure.Storage.Blobs, AWSSDK.S3 versions
```

---

## Task 1: SimpleModule.Storage — Abstraction Package

**Files:**
- Create: `framework/SimpleModule.Storage/SimpleModule.Storage.csproj`
- Create: `framework/SimpleModule.Storage/IStorageProvider.cs`
- Create: `framework/SimpleModule.Storage/StorageResult.cs`
- Create: `framework/SimpleModule.Storage/StorageEntry.cs`
- Create: `framework/SimpleModule.Storage/StoragePathHelper.cs`

- [ ] **Step 1: Create project file**

Create `framework/SimpleModule.Storage/SimpleModule.Storage.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Library</OutputType>
  </PropertyGroup>
</Project>
```

- [ ] **Step 2: Create IStorageProvider interface**

Create `framework/SimpleModule.Storage/IStorageProvider.cs`:

```csharp
namespace SimpleModule.Storage;

public interface IStorageProvider
{
    Task<StorageResult> SaveAsync(string path, Stream content, string contentType, CancellationToken cancellationToken = default);
    Task<Stream?> GetAsync(string path, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string path, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StorageEntry>> ListAsync(string prefix, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 3: Create model records**

Create `framework/SimpleModule.Storage/StorageResult.cs`:

```csharp
namespace SimpleModule.Storage;

public sealed record StorageResult(string Path, long Size, string ContentType);
```

Create `framework/SimpleModule.Storage/StorageEntry.cs`:

```csharp
namespace SimpleModule.Storage;

public sealed record StorageEntry(
    string Path,
    string Name,
    long Size,
    string ContentType,
    DateTimeOffset LastModified,
    bool IsFolder
);
```

- [ ] **Step 4: Create StoragePathHelper**

Create `framework/SimpleModule.Storage/StoragePathHelper.cs`:

```csharp
namespace SimpleModule.Storage;

public static class StoragePathHelper
{
    public static string Normalize(string path)
    {
        var normalized = path.Replace('\\', '/').Trim('/').Trim();
        return normalized;
    }

    public static string Combine(string? folder, string fileName)
    {
        if (string.IsNullOrWhiteSpace(folder))
        {
            return fileName;
        }

        return $"{Normalize(folder)}/{fileName}";
    }

    public static string GetFileName(string path)
    {
        var normalized = Normalize(path);
        var lastSlash = normalized.LastIndexOf('/');
        return lastSlash < 0 ? normalized : normalized[(lastSlash + 1)..];
    }

    public static string? GetFolder(string path)
    {
        var normalized = Normalize(path);
        var lastSlash = normalized.LastIndexOf('/');
        return lastSlash < 0 ? null : normalized[..lastSlash];
    }
}
```

- [ ] **Step 5: Verify build**

Run: `dotnet build framework/SimpleModule.Storage/SimpleModule.Storage.csproj`
Expected: Build succeeded. 0 Warning(s). 0 Error(s).

- [ ] **Step 6: Commit**

```bash
git add framework/SimpleModule.Storage/
git commit -m "feat: add SimpleModule.Storage abstraction package"
```

---

## Task 2: SimpleModule.Storage.Local — Local Filesystem Provider

**Files:**
- Create: `framework/SimpleModule.Storage.Local/SimpleModule.Storage.Local.csproj`
- Create: `framework/SimpleModule.Storage.Local/LocalStorageOptions.cs`
- Create: `framework/SimpleModule.Storage.Local/LocalStorageProvider.cs`
- Create: `framework/SimpleModule.Storage.Local/LocalStorageExtensions.cs`

- [ ] **Step 1: Create project file**

Create `framework/SimpleModule.Storage.Local/SimpleModule.Storage.Local.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Library</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <ProjectReference Include="..\SimpleModule.Storage\SimpleModule.Storage.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Create options class**

Create `framework/SimpleModule.Storage.Local/LocalStorageOptions.cs`:

```csharp
namespace SimpleModule.Storage.Local;

public sealed class LocalStorageOptions
{
    public string BasePath { get; set; } = "./storage";
}
```

- [ ] **Step 3: Create LocalStorageProvider**

Create `framework/SimpleModule.Storage.Local/LocalStorageProvider.cs`:

```csharp
using Microsoft.Extensions.Options;

namespace SimpleModule.Storage.Local;

public sealed class LocalStorageProvider(IOptions<LocalStorageOptions> options) : IStorageProvider
{
    private readonly string _basePath = Path.GetFullPath(options.Value.BasePath);

    public async Task<StorageResult> SaveAsync(
        string path,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default
    )
    {
        var normalized = StoragePathHelper.Normalize(path);
        var fullPath = GetFullPath(normalized);

        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var fileStream = new FileStream(
            fullPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true
        );
        await content.CopyToAsync(fileStream, cancellationToken);

        return new StorageResult(normalized, fileStream.Length, contentType);
    }

    public Task<Stream?> GetAsync(string path, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(StoragePathHelper.Normalize(path));

        if (!File.Exists(fullPath))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true
        );
        return Task.FromResult<Stream?>(stream);
    }

    public Task<bool> DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(StoragePathHelper.Normalize(path));

        if (!File.Exists(fullPath))
        {
            return Task.FromResult(false);
        }

        File.Delete(fullPath);
        return Task.FromResult(true);
    }

    public Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(StoragePathHelper.Normalize(path));
        return Task.FromResult(File.Exists(fullPath));
    }

    public Task<IReadOnlyList<StorageEntry>> ListAsync(
        string prefix,
        CancellationToken cancellationToken = default
    )
    {
        var normalized = StoragePathHelper.Normalize(prefix);
        var fullPath = string.IsNullOrEmpty(normalized)
            ? _basePath
            : Path.Combine(_basePath, normalized.Replace('/', Path.DirectorySeparatorChar));

        if (!Directory.Exists(fullPath))
        {
            return Task.FromResult<IReadOnlyList<StorageEntry>>(Array.Empty<StorageEntry>());
        }

        var entries = new List<StorageEntry>();

        foreach (var dir in Directory.GetDirectories(fullPath))
        {
            var dirInfo = new DirectoryInfo(dir);
            var relativePath = Path.GetRelativePath(_basePath, dir).Replace('\\', '/');
            entries.Add(
                new StorageEntry(
                    relativePath,
                    dirInfo.Name,
                    Size: 0,
                    ContentType: string.Empty,
                    dirInfo.LastWriteTimeUtc,
                    IsFolder: true
                )
            );
        }

        foreach (var file in Directory.GetFiles(fullPath))
        {
            var fileInfo = new FileInfo(file);
            var relativePath = Path.GetRelativePath(_basePath, file).Replace('\\', '/');
            entries.Add(
                new StorageEntry(
                    relativePath,
                    fileInfo.Name,
                    fileInfo.Length,
                    ContentType: string.Empty,
                    fileInfo.LastWriteTimeUtc,
                    IsFolder: false
                )
            );
        }

        return Task.FromResult<IReadOnlyList<StorageEntry>>(entries);
    }

    private string GetFullPath(string normalizedPath)
    {
        var localPath = normalizedPath.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(_basePath, localPath));

        if (!fullPath.StartsWith(_basePath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Path traversal detected.");
        }

        return fullPath;
    }
}
```

- [ ] **Step 4: Create DI extension method**

Create `framework/SimpleModule.Storage.Local/LocalStorageExtensions.cs`:

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleModule.Storage.Local;

public static class LocalStorageExtensions
{
    public static IServiceCollection AddLocalStorage(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<LocalStorageOptions>(configuration.GetSection("Storage:Local"));
        services.AddSingleton<IStorageProvider, LocalStorageProvider>();
        return services;
    }
}
```

- [ ] **Step 5: Verify build**

Run: `dotnet build framework/SimpleModule.Storage.Local/SimpleModule.Storage.Local.csproj`
Expected: Build succeeded. 0 Warning(s). 0 Error(s).

- [ ] **Step 6: Commit**

```bash
git add framework/SimpleModule.Storage.Local/
git commit -m "feat: add local filesystem storage provider"
```

---

## Task 3: SimpleModule.Storage.Azure — Azure Blob Provider

**Files:**
- Create: `framework/SimpleModule.Storage.Azure/SimpleModule.Storage.Azure.csproj`
- Create: `framework/SimpleModule.Storage.Azure/AzureBlobStorageOptions.cs`
- Create: `framework/SimpleModule.Storage.Azure/AzureBlobStorageProvider.cs`
- Create: `framework/SimpleModule.Storage.Azure/AzureBlobStorageExtensions.cs`
- Modify: `Directory.Packages.props` — add `Azure.Storage.Blobs` version

- [ ] **Step 1: Add NuGet version to Directory.Packages.props**

Add to the `<ItemGroup>` in `Directory.Packages.props`:

```xml
<PackageVersion Include="Azure.Storage.Blobs" Version="12.24.0" />
```

- [ ] **Step 2: Create project file**

Create `framework/SimpleModule.Storage.Azure/SimpleModule.Storage.Azure.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Library</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <PackageReference Include="Azure.Storage.Blobs" />
    <ProjectReference Include="..\SimpleModule.Storage\SimpleModule.Storage.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Create options class**

Create `framework/SimpleModule.Storage.Azure/AzureBlobStorageOptions.cs`:

```csharp
namespace SimpleModule.Storage.Azure;

public sealed class AzureBlobStorageOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "files";
}
```

- [ ] **Step 4: Create AzureBlobStorageProvider**

Create `framework/SimpleModule.Storage.Azure/AzureBlobStorageProvider.cs`:

```csharp
using global::Azure.Storage.Blobs;
using global::Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace SimpleModule.Storage.Azure;

public sealed class AzureBlobStorageProvider : IStorageProvider
{
    private readonly BlobContainerClient _container;

    public AzureBlobStorageProvider(IOptions<AzureBlobStorageOptions> options)
    {
        var client = new BlobServiceClient(options.Value.ConnectionString);
        _container = client.GetBlobContainerClient(options.Value.ContainerName);
    }

    public async Task<StorageResult> SaveAsync(
        string path,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default
    )
    {
        var normalized = StoragePathHelper.Normalize(path);
        var blob = _container.GetBlobClient(normalized);

        await blob.UploadAsync(
            content,
            new BlobHttpHeaders { ContentType = contentType },
            cancellationToken: cancellationToken
        );

        var properties = await blob.GetPropertiesAsync(cancellationToken: cancellationToken);
        return new StorageResult(normalized, properties.Value.ContentLength, contentType);
    }

    public async Task<Stream?> GetAsync(string path, CancellationToken cancellationToken = default)
    {
        var normalized = StoragePathHelper.Normalize(path);
        var blob = _container.GetBlobClient(normalized);

        if (!await blob.ExistsAsync(cancellationToken))
        {
            return null;
        }

        var response = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken);
        return response.Value.Content;
    }

    public async Task<bool> DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        var normalized = StoragePathHelper.Normalize(path);
        var blob = _container.GetBlobClient(normalized);
        var response = await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
        return response.Value;
    }

    public async Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        var normalized = StoragePathHelper.Normalize(path);
        var blob = _container.GetBlobClient(normalized);
        var response = await blob.ExistsAsync(cancellationToken);
        return response.Value;
    }

    public async Task<IReadOnlyList<StorageEntry>> ListAsync(
        string prefix,
        CancellationToken cancellationToken = default
    )
    {
        var normalized = StoragePathHelper.Normalize(prefix);
        var blobPrefix = string.IsNullOrEmpty(normalized) ? null : normalized + "/";

        var entries = new List<StorageEntry>();

        await foreach (
            var item in _container.GetBlobsByHierarchyAsync(
                delimiter: "/",
                prefix: blobPrefix,
                cancellationToken: cancellationToken
            )
        )
        {
            if (item.IsPrefix)
            {
                var folderPath = item.Prefix.TrimEnd('/');
                entries.Add(
                    new StorageEntry(
                        folderPath,
                        StoragePathHelper.GetFileName(folderPath),
                        Size: 0,
                        ContentType: string.Empty,
                        DateTimeOffset.MinValue,
                        IsFolder: true
                    )
                );
            }
            else if (item.IsBlob)
            {
                entries.Add(
                    new StorageEntry(
                        item.Blob.Name,
                        StoragePathHelper.GetFileName(item.Blob.Name),
                        item.Blob.Properties.ContentLength ?? 0,
                        item.Blob.Properties.ContentType ?? string.Empty,
                        item.Blob.Properties.LastModified ?? DateTimeOffset.MinValue,
                        IsFolder: false
                    )
                );
            }
        }

        return entries;
    }
}
```

- [ ] **Step 5: Create DI extension method**

Create `framework/SimpleModule.Storage.Azure/AzureBlobStorageExtensions.cs`:

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleModule.Storage.Azure;

public static class AzureBlobStorageExtensions
{
    public static IServiceCollection AddAzureBlobStorage(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<AzureBlobStorageOptions>(configuration.GetSection("Storage:Azure"));
        services.AddSingleton<IStorageProvider, AzureBlobStorageProvider>();
        return services;
    }
}
```

- [ ] **Step 6: Verify build**

Run: `dotnet build framework/SimpleModule.Storage.Azure/SimpleModule.Storage.Azure.csproj`
Expected: Build succeeded. 0 Warning(s). 0 Error(s).

- [ ] **Step 7: Commit**

```bash
git add framework/SimpleModule.Storage.Azure/ Directory.Packages.props
git commit -m "feat: add Azure Blob storage provider"
```

---

## Task 4: SimpleModule.Storage.S3 — S3-Compatible Provider

**Files:**
- Create: `framework/SimpleModule.Storage.S3/SimpleModule.Storage.S3.csproj`
- Create: `framework/SimpleModule.Storage.S3/S3StorageOptions.cs`
- Create: `framework/SimpleModule.Storage.S3/S3StorageProvider.cs`
- Create: `framework/SimpleModule.Storage.S3/S3StorageExtensions.cs`
- Modify: `Directory.Packages.props` — add `AWSSDK.S3` version

- [ ] **Step 1: Add NuGet version to Directory.Packages.props**

Add to the `<ItemGroup>` in `Directory.Packages.props`:

```xml
<PackageVersion Include="AWSSDK.S3" Version="3.7.500.2" />
```

- [ ] **Step 2: Create project file**

Create `framework/SimpleModule.Storage.S3/SimpleModule.Storage.S3.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Library</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <PackageReference Include="AWSSDK.S3" />
    <ProjectReference Include="..\SimpleModule.Storage\SimpleModule.Storage.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Create options class**

Create `framework/SimpleModule.Storage.S3/S3StorageOptions.cs`:

```csharp
namespace SimpleModule.Storage.S3;

public sealed class S3StorageOptions
{
    public string ServiceUrl { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Region { get; set; } = "us-east-1";
    public bool ForcePathStyle { get; set; }
}
```

- [ ] **Step 4: Create S3StorageProvider**

Create `framework/SimpleModule.Storage.S3/S3StorageProvider.cs`:

```csharp
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace SimpleModule.Storage.S3;

public sealed class S3StorageProvider : IStorageProvider, IDisposable
{
    private readonly IAmazonS3 _client;
    private readonly string _bucketName;

    public S3StorageProvider(IOptions<S3StorageOptions> options)
    {
        var opts = options.Value;
        var config = new AmazonS3Config
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(opts.Region),
            ForcePathStyle = opts.ForcePathStyle,
        };

        if (!string.IsNullOrEmpty(opts.ServiceUrl))
        {
            config.ServiceURL = opts.ServiceUrl;
        }

        _client = new AmazonS3Client(opts.AccessKey, opts.SecretKey, config);
        _bucketName = opts.BucketName;
    }

    public async Task<StorageResult> SaveAsync(
        string path,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default
    )
    {
        var normalized = StoragePathHelper.Normalize(path);

        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = normalized,
            InputStream = content,
            ContentType = contentType,
        };

        await _client.PutObjectAsync(request, cancellationToken);

        var metadata = await _client.GetObjectMetadataAsync(
            _bucketName,
            normalized,
            cancellationToken
        );

        return new StorageResult(normalized, metadata.ContentLength, contentType);
    }

    public async Task<Stream?> GetAsync(
        string path,
        CancellationToken cancellationToken = default
    )
    {
        var normalized = StoragePathHelper.Normalize(path);

        try
        {
            var response = await _client.GetObjectAsync(
                _bucketName,
                normalized,
                cancellationToken
            );
            return response.ResponseStream;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<bool> DeleteAsync(
        string path,
        CancellationToken cancellationToken = default
    )
    {
        var normalized = StoragePathHelper.Normalize(path);

        var exists = await ExistsAsync(normalized, cancellationToken);
        if (!exists)
        {
            return false;
        }

        await _client.DeleteObjectAsync(_bucketName, normalized, cancellationToken);
        return true;
    }

    public async Task<bool> ExistsAsync(
        string path,
        CancellationToken cancellationToken = default
    )
    {
        var normalized = StoragePathHelper.Normalize(path);

        try
        {
            await _client.GetObjectMetadataAsync(_bucketName, normalized, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<StorageEntry>> ListAsync(
        string prefix,
        CancellationToken cancellationToken = default
    )
    {
        var normalized = StoragePathHelper.Normalize(prefix);
        var s3Prefix = string.IsNullOrEmpty(normalized) ? null : normalized + "/";

        var request = new ListObjectsV2Request
        {
            BucketName = _bucketName,
            Prefix = s3Prefix,
            Delimiter = "/",
        };

        var entries = new List<StorageEntry>();
        ListObjectsV2Response response;

        do
        {
            response = await _client.ListObjectsV2Async(request, cancellationToken);

            foreach (var commonPrefix in response.CommonPrefixes)
            {
                var folderPath = commonPrefix.TrimEnd('/');
                entries.Add(
                    new StorageEntry(
                        folderPath,
                        StoragePathHelper.GetFileName(folderPath),
                        Size: 0,
                        ContentType: string.Empty,
                        DateTimeOffset.MinValue,
                        IsFolder: true
                    )
                );
            }

            foreach (var obj in response.S3Objects)
            {
                if (obj.Key.EndsWith('/'))
                {
                    continue;
                }

                entries.Add(
                    new StorageEntry(
                        obj.Key,
                        StoragePathHelper.GetFileName(obj.Key),
                        obj.Size,
                        ContentType: string.Empty,
                        new DateTimeOffset(obj.LastModified),
                        IsFolder: false
                    )
                );
            }

            request.ContinuationToken = response.NextContinuationToken;
        } while (response.IsTruncated);

        return entries;
    }

    public void Dispose() => _client.Dispose();
}
```

- [ ] **Step 5: Create DI extension method**

Create `framework/SimpleModule.Storage.S3/S3StorageExtensions.cs`:

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleModule.Storage.S3;

public static class S3StorageExtensions
{
    public static IServiceCollection AddS3Storage(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<S3StorageOptions>(configuration.GetSection("Storage:S3"));
        services.AddSingleton<IStorageProvider, S3StorageProvider>();
        return services;
    }
}
```

- [ ] **Step 6: Verify build**

Run: `dotnet build framework/SimpleModule.Storage.S3/SimpleModule.Storage.S3.csproj`
Expected: Build succeeded. 0 Warning(s). 0 Error(s).

- [ ] **Step 7: Commit**

```bash
git add framework/SimpleModule.Storage.S3/ Directory.Packages.props
git commit -m "feat: add S3-compatible storage provider"
```

---

## Task 5: FileStorage.Contracts — Module Contracts

**Files:**
- Create: `modules/FileStorage/src/FileStorage.Contracts/FileStorage.Contracts.csproj`
- Create: `modules/FileStorage/src/FileStorage.Contracts/FileStorageId.cs`
- Create: `modules/FileStorage/src/FileStorage.Contracts/StoredFile.cs`
- Create: `modules/FileStorage/src/FileStorage.Contracts/IFileStorageContracts.cs`

- [ ] **Step 1: Create project file**

Create `modules/FileStorage/src/FileStorage.Contracts/FileStorage.Contracts.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Library</OutputType>
    <DefineConstants>$(DefineConstants);VOGEN_NO_VALIDATION</DefineConstants>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" />
    <PackageReference Include="Vogen" />
    <ProjectReference Include="..\..\..\..\framework\SimpleModule.Core\SimpleModule.Core.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Create strongly-typed ID**

Create `modules/FileStorage/src/FileStorage.Contracts/FileStorageId.cs`:

```csharp
using Vogen;

namespace SimpleModule.FileStorage.Contracts;

[ValueObject<int>(conversions: Conversions.SystemTextJson | Conversions.EfCoreValueConverter)]
public readonly partial struct FileStorageId;
```

- [ ] **Step 3: Create StoredFile DTO**

Create `modules/FileStorage/src/FileStorage.Contracts/StoredFile.cs`:

```csharp
using SimpleModule.Core;

namespace SimpleModule.FileStorage.Contracts;

[Dto]
public class StoredFile
{
    public FileStorageId Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string? Folder { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
```

- [ ] **Step 4: Create contracts interface**

Create `modules/FileStorage/src/FileStorage.Contracts/IFileStorageContracts.cs`:

```csharp
namespace SimpleModule.FileStorage.Contracts;

public interface IFileStorageContracts
{
    Task<IEnumerable<StoredFile>> GetFilesAsync(string? folder = null);
    Task<StoredFile?> GetFileByIdAsync(FileStorageId id);
    Task<StoredFile> UploadFileAsync(Stream content, string fileName, string contentType, string? folder = null);
    Task DeleteFileAsync(FileStorageId id);
    Task<Stream?> DownloadFileAsync(FileStorageId id);
    Task<IEnumerable<string>> GetFoldersAsync(string? parentFolder = null);
}
```

- [ ] **Step 5: Verify build**

Run: `dotnet build modules/FileStorage/src/FileStorage.Contracts/FileStorage.Contracts.csproj`
Expected: Build succeeded. 0 Warning(s). 0 Error(s).

- [ ] **Step 6: Commit**

```bash
git add modules/FileStorage/src/FileStorage.Contracts/
git commit -m "feat: add FileStorage contracts"
```

---

## Task 6: FileStorage Module — Core Infrastructure

**Files:**
- Create: `modules/FileStorage/src/FileStorage/FileStorage.csproj`
- Create: `modules/FileStorage/src/FileStorage/FileStorageConstants.cs`
- Create: `modules/FileStorage/src/FileStorage/FileStoragePermissions.cs`
- Create: `modules/FileStorage/src/FileStorage/FileStorageDbContext.cs`
- Create: `modules/FileStorage/src/FileStorage/EntityConfigurations/StoredFileConfiguration.cs`

- [ ] **Step 1: Create project file**

Create `modules/FileStorage/src/FileStorage/FileStorage.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <ProjectReference Include="..\..\..\..\framework\SimpleModule.Core\SimpleModule.Core.csproj" />
    <ProjectReference Include="..\..\..\..\framework\SimpleModule.Database\SimpleModule.Database.csproj" />
    <ProjectReference Include="..\..\..\..\framework\SimpleModule.Storage\SimpleModule.Storage.csproj" />
    <ProjectReference Include="..\FileStorage.Contracts\FileStorage.Contracts.csproj" />
  </ItemGroup>
  <ItemGroup>
    <None Update="Views\*.tsx">
      <DependentUpon>%(Filename)Endpoint.cs</DependentUpon>
    </None>
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Create constants**

Create `modules/FileStorage/src/FileStorage/FileStorageConstants.cs`:

```csharp
namespace SimpleModule.FileStorage;

public static class FileStorageConstants
{
    public const string ModuleName = "FileStorage";
    public const string RoutePrefix = "/api/files";
}
```

- [ ] **Step 3: Create permissions**

Create `modules/FileStorage/src/FileStorage/FileStoragePermissions.cs`:

```csharp
using SimpleModule.Core.Authorization;

namespace SimpleModule.FileStorage;

public sealed class FileStoragePermissions : IModulePermissions
{
    public const string View = "FileStorage.View";
    public const string Upload = "FileStorage.Upload";
    public const string Delete = "FileStorage.Delete";
}
```

- [ ] **Step 4: Create DbContext**

Create `modules/FileStorage/src/FileStorage/FileStorageDbContext.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimpleModule.Database;
using SimpleModule.FileStorage.Contracts;
using SimpleModule.FileStorage.EntityConfigurations;

namespace SimpleModule.FileStorage;

public class FileStorageDbContext(
    DbContextOptions<FileStorageDbContext> options,
    IOptions<DatabaseOptions> dbOptions
) : DbContext(options)
{
    public DbSet<StoredFile> StoredFiles => Set<StoredFile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new StoredFileConfiguration());
        modelBuilder.ApplyModuleSchema("FileStorage", dbOptions.Value);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<FileStorageId>()
            .HaveConversion<FileStorageId.EfCoreValueConverter, FileStorageId.EfCoreValueComparer>();
    }
}
```

- [ ] **Step 5: Create entity configuration**

Create `modules/FileStorage/src/FileStorage/EntityConfigurations/StoredFileConfiguration.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleModule.FileStorage.Contracts;

namespace SimpleModule.FileStorage.EntityConfigurations;

public class StoredFileConfiguration : IEntityTypeConfiguration<StoredFile>
{
    public void Configure(EntityTypeBuilder<StoredFile> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedOnAdd();
        builder.Property(f => f.FileName).IsRequired().HasMaxLength(512);
        builder.Property(f => f.StoragePath).IsRequired().HasMaxLength(1024);
        builder.Property(f => f.ContentType).IsRequired().HasMaxLength(256);
        builder.Property(f => f.Folder).HasMaxLength(1024);
        builder.HasIndex(f => f.Folder);
    }
}
```

- [ ] **Step 6: Verify build**

Run: `dotnet build modules/FileStorage/src/FileStorage/FileStorage.csproj`
Expected: Build succeeded. 0 Warning(s). 0 Error(s).

- [ ] **Step 7: Commit**

```bash
git add modules/FileStorage/src/FileStorage/FileStorage.csproj modules/FileStorage/src/FileStorage/FileStorageConstants.cs modules/FileStorage/src/FileStorage/FileStoragePermissions.cs modules/FileStorage/src/FileStorage/FileStorageDbContext.cs modules/FileStorage/src/FileStorage/EntityConfigurations/
git commit -m "feat: add FileStorage module infrastructure"
```

---

## Task 7: FileStorageService — Business Logic

**Files:**
- Create: `modules/FileStorage/src/FileStorage/FileStorageService.cs`

- [ ] **Step 1: Create the service**

Create `modules/FileStorage/src/FileStorage/FileStorageService.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SimpleModule.FileStorage.Contracts;
using SimpleModule.Storage;

namespace SimpleModule.FileStorage;

public sealed partial class FileStorageService(
    FileStorageDbContext db,
    IStorageProvider storageProvider,
    ILogger<FileStorageService> logger
) : IFileStorageContracts
{
    public async Task<IEnumerable<StoredFile>> GetFilesAsync(string? folder = null)
    {
        var query = db.StoredFiles.AsNoTracking();

        if (folder is not null)
        {
            var normalizedFolder = StoragePathHelper.Normalize(folder);
            query = query.Where(f => f.Folder == normalizedFolder);
        }
        else
        {
            query = query.Where(f => f.Folder == null);
        }

        return await query.OrderBy(f => f.FileName).ToListAsync();
    }

    public async Task<StoredFile?> GetFileByIdAsync(FileStorageId id)
    {
        var file = await db.StoredFiles.FindAsync(id);
        if (file is null)
        {
            LogFileNotFound(logger, id);
        }

        return file;
    }

    public async Task<StoredFile> UploadFileAsync(
        Stream content,
        string fileName,
        string contentType,
        string? folder = null
    )
    {
        var normalizedFolder = folder is not null ? StoragePathHelper.Normalize(folder) : null;
        var storagePath = StoragePathHelper.Combine(normalizedFolder, fileName);

        var result = await storageProvider.SaveAsync(storagePath, content, contentType);

        var storedFile = new StoredFile
        {
            FileName = fileName,
            StoragePath = result.Path,
            ContentType = contentType,
            Size = result.Size,
            Folder = normalizedFolder,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.StoredFiles.Add(storedFile);
        await db.SaveChangesAsync();

        LogFileUploaded(logger, storedFile.Id, storedFile.FileName);
        return storedFile;
    }

    public async Task DeleteFileAsync(FileStorageId id)
    {
        var file = await db.StoredFiles.FindAsync(id)
            ?? throw new InvalidOperationException($"File with ID {id} not found.");

        await storageProvider.DeleteAsync(file.StoragePath);
        db.StoredFiles.Remove(file);
        await db.SaveChangesAsync();

        LogFileDeleted(logger, id, file.FileName);
    }

    public async Task<Stream?> DownloadFileAsync(FileStorageId id)
    {
        var file = await db.StoredFiles.FindAsync(id);
        if (file is null)
        {
            LogFileNotFound(logger, id);
            return null;
        }

        return await storageProvider.GetAsync(file.StoragePath);
    }

    public async Task<IEnumerable<string>> GetFoldersAsync(string? parentFolder = null)
    {
        var query = db.StoredFiles.AsNoTracking();

        if (parentFolder is not null)
        {
            var normalizedParent = StoragePathHelper.Normalize(parentFolder);
            query = query.Where(f => f.Folder != null && f.Folder.StartsWith(normalizedParent + "/"));

            var folders = await query.Select(f => f.Folder!).Distinct().ToListAsync();
            return folders
                .Select(f => f[(normalizedParent.Length + 1)..])
                .Select(f => f.Contains('/') ? f[..f.IndexOf('/')] : f)
                .Distinct()
                .Select(f => $"{normalizedParent}/{f}")
                .Order();
        }

        var topLevelFolders = await query
            .Where(f => f.Folder != null)
            .Select(f => f.Folder!)
            .Distinct()
            .ToListAsync();

        return topLevelFolders
            .Select(f => f.Contains('/') ? f[..f.IndexOf('/')] : f)
            .Distinct()
            .Order();
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "File with ID {Id} not found")]
    private static partial void LogFileNotFound(ILogger logger, FileStorageId id);

    [LoggerMessage(Level = LogLevel.Information, Message = "File uploaded: {Id} ({FileName})")]
    private static partial void LogFileUploaded(ILogger logger, FileStorageId id, string fileName);

    [LoggerMessage(Level = LogLevel.Information, Message = "File deleted: {Id} ({FileName})")]
    private static partial void LogFileDeleted(ILogger logger, FileStorageId id, string fileName);
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build modules/FileStorage/src/FileStorage/FileStorage.csproj`
Expected: Build succeeded. 0 Warning(s). 0 Error(s).

- [ ] **Step 3: Commit**

```bash
git add modules/FileStorage/src/FileStorage/FileStorageService.cs
git commit -m "feat: add FileStorageService with upload, download, delete, listing"
```

---

## Task 8: FileStorageModule — Module Class with Menu, Settings, Permissions

**Files:**
- Create: `modules/FileStorage/src/FileStorage/FileStorageModule.cs`

- [ ] **Step 1: Create module class**

Create `modules/FileStorage/src/FileStorage/FileStorageModule.cs`:

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Menu;
using SimpleModule.Core.Settings;
using SimpleModule.Database;
using SimpleModule.FileStorage.Contracts;

namespace SimpleModule.FileStorage;

[Module(
    FileStorageConstants.ModuleName,
    RoutePrefix = FileStorageConstants.RoutePrefix,
    ViewPrefix = "/files"
)]
public class FileStorageModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<FileStorageDbContext>(
            configuration,
            FileStorageConstants.ModuleName
        );
        services.AddScoped<IFileStorageContracts, FileStorageService>();
    }

    public void ConfigureMenu(IMenuBuilder menus)
    {
        menus.Add(
            new MenuItem
            {
                Label = "Files",
                Url = "/files/browse",
                Icon =
                    """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M3 7v10a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-6l-2-2H5a2 2 0 00-2 2z"/></svg>""",
                Order = 50,
                Section = MenuSection.AppSidebar,
            }
        );
    }

    public void ConfigurePermissions(PermissionRegistryBuilder builder)
    {
        builder.AddPermissions<FileStoragePermissions>();
    }

    public void ConfigureSettings(ISettingsBuilder settings)
    {
        settings.Add(
            new SettingDefinition
            {
                Key = "FileStorage.MaxFileSizeMb",
                DisplayName = "Max File Size (MB)",
                Description = "Maximum allowed file size for uploads in megabytes.",
                Group = "FileStorage",
                Scope = SettingScope.Application,
                DefaultValue = "50",
                Type = SettingType.Number,
            }
        );
        settings.Add(
            new SettingDefinition
            {
                Key = "FileStorage.AllowedExtensions",
                DisplayName = "Allowed File Extensions",
                Description = "Comma-separated list of allowed file extensions (e.g., .jpg,.pdf,.zip).",
                Group = "FileStorage",
                Scope = SettingScope.Application,
                DefaultValue = ".jpg,.jpeg,.png,.gif,.pdf,.doc,.docx,.xls,.xlsx,.zip",
                Type = SettingType.Text,
            }
        );
    }
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build modules/FileStorage/src/FileStorage/FileStorage.csproj`
Expected: Build succeeded. 0 Warning(s). 0 Error(s).

- [ ] **Step 3: Commit**

```bash
git add modules/FileStorage/src/FileStorage/FileStorageModule.cs
git commit -m "feat: add FileStorageModule with menu, permissions, settings"
```

---

## Task 9: API Endpoints

**Files:**
- Create: `modules/FileStorage/src/FileStorage/Endpoints/Files/GetAllEndpoint.cs`
- Create: `modules/FileStorage/src/FileStorage/Endpoints/Files/GetByIdEndpoint.cs`
- Create: `modules/FileStorage/src/FileStorage/Endpoints/Files/DownloadEndpoint.cs`
- Create: `modules/FileStorage/src/FileStorage/Endpoints/Files/UploadEndpoint.cs`
- Create: `modules/FileStorage/src/FileStorage/Endpoints/Files/DeleteEndpoint.cs`
- Create: `modules/FileStorage/src/FileStorage/Endpoints/Files/ListFoldersEndpoint.cs`

- [ ] **Step 1: Create GetAllEndpoint**

Create `modules/FileStorage/src/FileStorage/Endpoints/Files/GetAllEndpoint.cs`:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.FileStorage.Contracts;

namespace SimpleModule.FileStorage.Endpoints.Files;

public class GetAllEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/",
                (string? folder, IFileStorageContracts files) =>
                    CrudEndpoints.GetAll(() => files.GetFilesAsync(folder))
            )
            .RequirePermission(FileStoragePermissions.View);
}
```

- [ ] **Step 2: Create GetByIdEndpoint**

Create `modules/FileStorage/src/FileStorage/Endpoints/Files/GetByIdEndpoint.cs`:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.FileStorage.Contracts;

namespace SimpleModule.FileStorage.Endpoints.Files;

public class GetByIdEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/{id}",
                (FileStorageId id, IFileStorageContracts files) =>
                    CrudEndpoints.GetById(() => files.GetFileByIdAsync(id))
            )
            .RequirePermission(FileStoragePermissions.View);
}
```

- [ ] **Step 3: Create DownloadEndpoint**

Create `modules/FileStorage/src/FileStorage/Endpoints/Files/DownloadEndpoint.cs`:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.FileStorage.Contracts;

namespace SimpleModule.FileStorage.Endpoints.Files;

public class DownloadEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/{id}/download",
                async (FileStorageId id, IFileStorageContracts files) =>
                {
                    var file = await files.GetFileByIdAsync(id);
                    if (file is null)
                    {
                        return Results.NotFound();
                    }

                    var stream = await files.DownloadFileAsync(id);
                    if (stream is null)
                    {
                        return Results.NotFound();
                    }

                    return TypedResults.File(stream, file.ContentType, file.FileName);
                }
            )
            .RequirePermission(FileStoragePermissions.View);
}
```

- [ ] **Step 4: Create UploadEndpoint**

Create `modules/FileStorage/src/FileStorage/Endpoints/Files/UploadEndpoint.cs`:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.FileStorage.Contracts;

namespace SimpleModule.FileStorage.Endpoints.Files;

public class UploadEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                "/",
                async (IFormFile file, string? folder, IFileStorageContracts files) =>
                {
                    await using var stream = file.OpenReadStream();
                    var storedFile = await files.UploadFileAsync(
                        stream,
                        file.FileName,
                        file.ContentType,
                        folder
                    );
                    return TypedResults.Created($"/api/files/{storedFile.Id}", storedFile);
                }
            )
            .RequirePermission(FileStoragePermissions.Upload)
            .DisableAntiforgery();
}
```

- [ ] **Step 5: Create DeleteEndpoint**

Create `modules/FileStorage/src/FileStorage/Endpoints/Files/DeleteEndpoint.cs`:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.FileStorage.Contracts;

namespace SimpleModule.FileStorage.Endpoints.Files;

public class DeleteEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(
                "/{id}",
                (FileStorageId id, IFileStorageContracts files) =>
                    CrudEndpoints.Delete(() => files.DeleteFileAsync(id))
            )
            .RequirePermission(FileStoragePermissions.Delete);
}
```

- [ ] **Step 6: Create ListFoldersEndpoint**

Create `modules/FileStorage/src/FileStorage/Endpoints/Files/ListFoldersEndpoint.cs`:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.FileStorage.Contracts;

namespace SimpleModule.FileStorage.Endpoints.Files;

public class ListFoldersEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/folders",
                async (string? parent, IFileStorageContracts files) =>
                    TypedResults.Ok(await files.GetFoldersAsync(parent))
            )
            .RequirePermission(FileStoragePermissions.View);
}
```

- [ ] **Step 7: Verify build**

Run: `dotnet build modules/FileStorage/src/FileStorage/FileStorage.csproj`
Expected: Build succeeded. 0 Warning(s). 0 Error(s).

- [ ] **Step 8: Commit**

```bash
git add modules/FileStorage/src/FileStorage/Endpoints/
git commit -m "feat: add FileStorage API endpoints"
```

---

## Task 10: Browse View — Backend + Frontend

**Files:**
- Create: `modules/FileStorage/src/FileStorage/Views/BrowseEndpoint.cs`
- Create: `modules/FileStorage/src/FileStorage/Views/Browse.tsx`
- Create: `modules/FileStorage/src/FileStorage/Pages/index.ts`
- Create: `modules/FileStorage/src/FileStorage/types.ts`
- Create: `modules/FileStorage/src/FileStorage/vite.config.ts`
- Create: `modules/FileStorage/src/FileStorage/package.json`

- [ ] **Step 1: Create BrowseEndpoint**

Create `modules/FileStorage/src/FileStorage/Views/BrowseEndpoint.cs`:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Inertia;
using SimpleModule.FileStorage.Contracts;

namespace SimpleModule.FileStorage.Views;

public class BrowseEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/browse",
                async (string? folder, IFileStorageContracts fileStorage) =>
                {
                    var files = await fileStorage.GetFilesAsync(folder);
                    var folders = await fileStorage.GetFoldersAsync(folder);

                    string? parentFolder = null;
                    if (folder is not null)
                    {
                        var normalized = Storage.StoragePathHelper.Normalize(folder);
                        var lastSlash = normalized.LastIndexOf('/');
                        parentFolder = lastSlash > 0 ? normalized[..lastSlash] : null;
                    }

                    return Inertia.Render(
                        "FileStorage/Browse",
                        new
                        {
                            files,
                            folders,
                            currentFolder = folder,
                            parentFolder,
                        }
                    );
                }
            )
            .RequirePermission(FileStoragePermissions.View);
    }
}
```

- [ ] **Step 2: Create types.ts**

Create `modules/FileStorage/src/FileStorage/types.ts`:

```typescript
export interface StoredFile {
  id: number;
  fileName: string;
  storagePath: string;
  contentType: string;
  size: number;
  folder: string | null;
  createdAt: string;
}
```

- [ ] **Step 3: Create Browse.tsx**

Create `modules/FileStorage/src/FileStorage/Views/Browse.tsx`:

```tsx
import { router } from '@inertiajs/react';
import {
  Button,
  DataGridPage,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@simplemodule/ui';
import { useRef, useState } from 'react';
import type { StoredFile } from '../types';

interface Props {
  files: StoredFile[];
  folders: string[];
  currentFolder: string | null;
  parentFolder: string | null;
}

function formatSize(bytes: number): string {
  if (bytes === 0) return '0 B';
  const units = ['B', 'KB', 'MB', 'GB'];
  const i = Math.floor(Math.log(bytes) / Math.log(1024));
  return `${(bytes / 1024 ** i).toFixed(i === 0 ? 0 : 1)} ${units[i]}`;
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  });
}

function folderName(path: string): string {
  const parts = path.split('/');
  return parts[parts.length - 1];
}

function breadcrumbs(folder: string | null): { label: string; path: string | null }[] {
  const crumbs: { label: string; path: string | null }[] = [{ label: 'Files', path: null }];
  if (!folder) return crumbs;
  const parts = folder.split('/');
  for (let i = 0; i < parts.length; i++) {
    crumbs.push({
      label: parts[i],
      path: parts.slice(0, i + 1).join('/'),
    });
  }
  return crumbs;
}

export default function Browse({ files, folders, currentFolder, parentFolder }: Props) {
  const [deleteTarget, setDeleteTarget] = useState<{ id: number; name: string } | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  function handleDelete() {
    if (!deleteTarget) return;
    router.delete(`/api/files/${deleteTarget.id}`, {
      onSuccess: () => setDeleteTarget(null),
    });
  }

  function handleUpload(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (!file) return;

    const formData = new FormData();
    formData.append('file', file);
    if (currentFolder) {
      formData.append('folder', currentFolder);
    }

    router.post('/api/files', formData, {
      forceFormData: true,
    });

    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  }

  const crumbs = breadcrumbs(currentFolder);
  const hasContent = folders.length > 0 || files.length > 0;

  return (
    <>
      <DataGridPage
        title={
          <nav className="flex items-center gap-1 text-sm">
            {crumbs.map((crumb, i) => (
              <span key={crumb.path ?? 'root'} className="flex items-center gap-1">
                {i > 0 && <span className="text-text-muted">/</span>}
                {i < crumbs.length - 1 ? (
                  <button
                    type="button"
                    className="text-primary hover:underline"
                    onClick={() =>
                      router.get('/files/browse', crumb.path ? { folder: crumb.path } : {})
                    }
                  >
                    {crumb.label}
                  </button>
                ) : (
                  <span className="font-medium">{crumb.label}</span>
                )}
              </span>
            ))}
          </nav>
        }
        description={`${files.length} file${files.length !== 1 ? 's' : ''}, ${folders.length} folder${folders.length !== 1 ? 's' : ''}`}
        actions={
          <>
            <input
              ref={fileInputRef}
              type="file"
              className="hidden"
              onChange={handleUpload}
            />
            <Button onClick={() => fileInputRef.current?.click()}>Upload File</Button>
          </>
        }
        data={hasContent ? files : []}
        emptyTitle="No files yet"
        emptyDescription="Upload a file to get started."
      >
        {() => (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Name</TableHead>
                <TableHead>Size</TableHead>
                <TableHead>Type</TableHead>
                <TableHead>Date</TableHead>
                <TableHead />
              </TableRow>
            </TableHeader>
            <TableBody>
              {parentFolder !== undefined && currentFolder && (
                <TableRow
                  className="cursor-pointer hover:bg-muted/50"
                  onClick={() =>
                    router.get(
                      '/files/browse',
                      parentFolder ? { folder: parentFolder } : {},
                    )
                  }
                >
                  <TableCell className="font-medium" colSpan={5}>
                    ..
                  </TableCell>
                </TableRow>
              )}
              {folders.map((f) => (
                <TableRow
                  key={f}
                  className="cursor-pointer hover:bg-muted/50"
                  onClick={() => router.get('/files/browse', { folder: f })}
                >
                  <TableCell className="font-medium">
                    <span className="mr-2">📁</span>
                    {folderName(f)}
                  </TableCell>
                  <TableCell className="text-text-muted">&mdash;</TableCell>
                  <TableCell className="text-text-muted">Folder</TableCell>
                  <TableCell className="text-text-muted">&mdash;</TableCell>
                  <TableCell />
                </TableRow>
              ))}
              {files.map((file) => (
                <TableRow key={file.id}>
                  <TableCell className="font-medium">{file.fileName}</TableCell>
                  <TableCell className="text-text-muted">{formatSize(file.size)}</TableCell>
                  <TableCell className="text-text-muted">{file.contentType}</TableCell>
                  <TableCell className="text-text-muted">{formatDate(file.createdAt)}</TableCell>
                  <TableCell>
                    <div className="flex gap-3">
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() =>
                          window.open(`/api/files/${file.id}/download`, '_blank')
                        }
                      >
                        Download
                      </Button>
                      <Button
                        variant="danger"
                        size="sm"
                        onClick={() =>
                          setDeleteTarget({ id: file.id, name: file.fileName })
                        }
                      >
                        Delete
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </DataGridPage>

      <Dialog open={deleteTarget !== null} onOpenChange={(open) => !open && setDeleteTarget(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Delete File</DialogTitle>
            <DialogDescription>
              Are you sure you want to delete &ldquo;{deleteTarget?.name}&rdquo;? This action
              cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="secondary" onClick={() => setDeleteTarget(null)}>
              Cancel
            </Button>
            <Button variant="danger" onClick={handleDelete}>
              Delete
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
```

- [ ] **Step 4: Create Pages/index.ts**

Create `modules/FileStorage/src/FileStorage/Pages/index.ts`:

```typescript
export const pages: Record<string, unknown> = {
  'FileStorage/Browse': () => import('../Views/Browse'),
};
```

- [ ] **Step 5: Create vite.config.ts**

Create `modules/FileStorage/src/FileStorage/vite.config.ts`:

```typescript
import { defineModuleConfig } from '@simplemodule/client/module';

export default defineModuleConfig(__dirname);
```

- [ ] **Step 6: Create package.json**

Create `modules/FileStorage/src/FileStorage/package.json`:

```json
{
  "private": true,
  "name": "@simplemodule/filestorage",
  "version": "0.0.0",
  "scripts": {
    "build": "cross-env VITE_MODE=prod vite build",
    "build:dev": "cross-env VITE_MODE=dev vite build",
    "watch": "cross-env VITE_MODE=dev vite build --watch"
  },
  "peerDependencies": {
    "react": "^19.0.0",
    "react-dom": "^19.0.0"
  }
}
```

- [ ] **Step 7: Verify build**

Run: `dotnet build modules/FileStorage/src/FileStorage/FileStorage.csproj`
Expected: Build succeeded. 0 Warning(s). 0 Error(s).

- [ ] **Step 8: Build frontend**

Run: `npm install && npm run build:dev --workspace=@simplemodule/filestorage`
Expected: Build completes, `modules/FileStorage/src/FileStorage/wwwroot/FileStorage.pages.js` is created.

- [ ] **Step 9: Commit**

```bash
git add modules/FileStorage/src/FileStorage/Views/ modules/FileStorage/src/FileStorage/Pages/ modules/FileStorage/src/FileStorage/types.ts modules/FileStorage/src/FileStorage/vite.config.ts modules/FileStorage/src/FileStorage/package.json
git commit -m "feat: add FileStorage browse view with file browser UI"
```

---

## Task 11: Wire Up Host + Solution

**Files:**
- Modify: `template/SimpleModule.Host/SimpleModule.Host.csproj` — add FileStorage + Storage.Local references
- Modify: `SimpleModule.slnx` — add all new projects
- Modify: `template/SimpleModule.Host/appsettings.json` — add Storage config section

- [ ] **Step 1: Add project references to Host**

Add to the `<ItemGroup>` with `<ProjectReference>` entries in `template/SimpleModule.Host/SimpleModule.Host.csproj`:

```xml
<ProjectReference Include="..\..\modules\FileStorage\src\FileStorage\FileStorage.csproj" />
<ProjectReference Include="..\..\framework\SimpleModule.Storage.Local\SimpleModule.Storage.Local.csproj" />
```

- [ ] **Step 2: Register local storage in Program.cs or host setup**

Check how other providers are registered. The Local storage provider needs to be registered in the host. Add to the host's service configuration (likely in `SimpleModuleHostExtensions` or `Program.cs`):

Find the appropriate location in the host setup code and add:

```csharp
using SimpleModule.Storage.Local;
// In the service configuration:
builder.Services.AddLocalStorage(builder.Configuration);
```

If this is done in a generated extension method, the module's `ConfigureServices` can call it. Otherwise, add it to `Program.cs` or the hosting setup before `builder.Build()`.

- [ ] **Step 3: Add Storage config to appsettings.json**

Add to `template/SimpleModule.Host/appsettings.json`:

```json
"Storage": {
  "Provider": "Local",
  "Local": {
    "BasePath": "./storage"
  }
}
```

- [ ] **Step 4: Add projects to SimpleModule.slnx**

Add the following entries to `SimpleModule.slnx`:

In the `/framework/` folder:

```xml
<Project Path="framework/SimpleModule.Storage/SimpleModule.Storage.csproj" />
<Project Path="framework/SimpleModule.Storage.Local/SimpleModule.Storage.Local.csproj" />
<Project Path="framework/SimpleModule.Storage.Azure/SimpleModule.Storage.Azure.csproj" />
<Project Path="framework/SimpleModule.Storage.S3/SimpleModule.Storage.S3.csproj" />
```

Add a new `/modules/FileStorage/` folder:

```xml
<Folder Name="/modules/FileStorage/">
    <Project Path="modules/FileStorage/src/FileStorage.Contracts/FileStorage.Contracts.csproj" />
    <Project Path="modules/FileStorage/src/FileStorage/FileStorage.csproj" />
    <Project Path="modules/FileStorage/tests/FileStorage.Tests/FileStorage.Tests.csproj" />
</Folder>
```

- [ ] **Step 5: Verify full solution build**

Run: `dotnet build`
Expected: Build succeeded. 0 Warning(s). 0 Error(s).

- [ ] **Step 6: Commit**

```bash
git add template/SimpleModule.Host/SimpleModule.Host.csproj template/SimpleModule.Host/appsettings.json SimpleModule.slnx
git commit -m "feat: wire FileStorage module and Storage.Local into host"
```

---

## Task 12: Tests

**Files:**
- Create: `modules/FileStorage/tests/FileStorage.Tests/FileStorage.Tests.csproj`
- Create: `modules/FileStorage/tests/FileStorage.Tests/InMemoryStorageProvider.cs`
- Create: `modules/FileStorage/tests/FileStorage.Tests/FileStorageServiceTests.cs`

- [ ] **Step 1: Create test project file**

Create `modules/FileStorage/tests/FileStorage.Tests/FileStorage.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="xunit.v3" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\FileStorage\FileStorage.csproj" />
    <ProjectReference Include="..\..\src\FileStorage.Contracts\FileStorage.Contracts.csproj" />
    <ProjectReference Include="..\..\..\..\tests\SimpleModule.Tests.Shared\SimpleModule.Tests.Shared.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Create InMemoryStorageProvider**

Create `modules/FileStorage/tests/FileStorage.Tests/InMemoryStorageProvider.cs`:

```csharp
using System.Collections.Concurrent;
using SimpleModule.Storage;

namespace SimpleModule.FileStorage.Tests;

public sealed class InMemoryStorageProvider : IStorageProvider
{
    private readonly ConcurrentDictionary<string, (byte[] Data, string ContentType, DateTimeOffset Modified)> _files = new();

    public async Task<StorageResult> SaveAsync(
        string path,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default
    )
    {
        var normalized = StoragePathHelper.Normalize(path);
        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, cancellationToken);
        var data = ms.ToArray();
        _files[normalized] = (data, contentType, DateTimeOffset.UtcNow);
        return new StorageResult(normalized, data.Length, contentType);
    }

    public Task<Stream?> GetAsync(string path, CancellationToken cancellationToken = default)
    {
        var normalized = StoragePathHelper.Normalize(path);
        if (_files.TryGetValue(normalized, out var entry))
        {
            return Task.FromResult<Stream?>(new MemoryStream(entry.Data));
        }

        return Task.FromResult<Stream?>(null);
    }

    public Task<bool> DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        var normalized = StoragePathHelper.Normalize(path);
        return Task.FromResult(_files.TryRemove(normalized, out _));
    }

    public Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        var normalized = StoragePathHelper.Normalize(path);
        return Task.FromResult(_files.ContainsKey(normalized));
    }

    public Task<IReadOnlyList<StorageEntry>> ListAsync(
        string prefix,
        CancellationToken cancellationToken = default
    )
    {
        var normalized = StoragePathHelper.Normalize(prefix);
        var p = string.IsNullOrEmpty(normalized) ? "" : normalized + "/";

        var entries = _files
            .Where(kvp => kvp.Key.StartsWith(p, StringComparison.Ordinal))
            .Select(kvp => new StorageEntry(
                kvp.Key,
                StoragePathHelper.GetFileName(kvp.Key),
                kvp.Value.Data.Length,
                kvp.Value.ContentType,
                kvp.Value.Modified,
                IsFolder: false
            ))
            .ToList();

        return Task.FromResult<IReadOnlyList<StorageEntry>>(entries);
    }
}
```

- [ ] **Step 3: Create FileStorageServiceTests**

Create `modules/FileStorage/tests/FileStorage.Tests/FileStorageServiceTests.cs`:

```csharp
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SimpleModule.Database;
using SimpleModule.FileStorage.Contracts;

namespace SimpleModule.FileStorage.Tests;

public class FileStorageServiceTests : IDisposable
{
    private readonly FileStorageDbContext _db;
    private readonly InMemoryStorageProvider _storageProvider;
    private readonly FileStorageService _service;

    public FileStorageServiceTests()
    {
        var dbOptions = new DbContextOptionsBuilder<FileStorageDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        var databaseOptions = Options.Create(new DatabaseOptions { Provider = "Sqlite" });

        _db = new FileStorageDbContext(dbOptions, databaseOptions);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();

        _storageProvider = new InMemoryStorageProvider();
        _service = new FileStorageService(
            _db,
            _storageProvider,
            NullLogger<FileStorageService>.Instance
        );
    }

    [Fact]
    public async Task UploadFileAsync_Saves_File_And_Creates_Record()
    {
        var content = "hello world"u8.ToArray();
        using var stream = new MemoryStream(content);

        var result = await _service.UploadFileAsync(stream, "test.txt", "text/plain");

        result.FileName.Should().Be("test.txt");
        result.ContentType.Should().Be("text/plain");
        result.Size.Should().Be(content.Length);
        result.Folder.Should().BeNull();
        result.Id.Value.Should().BeGreaterThan(0);

        var exists = await _storageProvider.ExistsAsync(result.StoragePath);
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task UploadFileAsync_With_Folder_Sets_Correct_Path()
    {
        using var stream = new MemoryStream("data"u8.ToArray());

        var result = await _service.UploadFileAsync(stream, "photo.jpg", "image/jpeg", "products/images");

        result.Folder.Should().Be("products/images");
        result.StoragePath.Should().Be("products/images/photo.jpg");
    }

    [Fact]
    public async Task GetFileByIdAsync_Returns_Null_For_Missing_Id()
    {
        var result = await _service.GetFileByIdAsync(FileStorageId.From(999));

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteFileAsync_Removes_Record_And_Storage()
    {
        using var stream = new MemoryStream("data"u8.ToArray());
        var uploaded = await _service.UploadFileAsync(stream, "delete-me.txt", "text/plain");

        await _service.DeleteFileAsync(uploaded.Id);

        var record = await _db.StoredFiles.FindAsync(uploaded.Id);
        record.Should().BeNull();

        var exists = await _storageProvider.ExistsAsync(uploaded.StoragePath);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task DownloadFileAsync_Returns_Stream()
    {
        var content = "file content"u8.ToArray();
        using var uploadStream = new MemoryStream(content);
        var uploaded = await _service.UploadFileAsync(uploadStream, "download.txt", "text/plain");

        var downloadStream = await _service.DownloadFileAsync(uploaded.Id);

        downloadStream.Should().NotBeNull();
        using var ms = new MemoryStream();
        await downloadStream!.CopyToAsync(ms);
        ms.ToArray().Should().BeEquivalentTo(content);
    }

    [Fact]
    public async Task GetFilesAsync_Filters_By_Folder()
    {
        using var s1 = new MemoryStream("a"u8.ToArray());
        using var s2 = new MemoryStream("b"u8.ToArray());
        using var s3 = new MemoryStream("c"u8.ToArray());

        await _service.UploadFileAsync(s1, "root.txt", "text/plain");
        await _service.UploadFileAsync(s2, "in-folder.txt", "text/plain", "docs");
        await _service.UploadFileAsync(s3, "also-in-folder.txt", "text/plain", "docs");

        var rootFiles = await _service.GetFilesAsync();
        rootFiles.Should().HaveCount(1);

        var docsFiles = await _service.GetFilesAsync("docs");
        docsFiles.Should().HaveCount(2);
    }

    [Fact]
    public async Task DeleteFileAsync_Throws_For_Missing_Id()
    {
        var act = () => _service.DeleteFileAsync(FileStorageId.From(999));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    public void Dispose()
    {
        _db.Database.CloseConnection();
        _db.Dispose();
    }
}
```

- [ ] **Step 4: Run tests**

Run: `dotnet test modules/FileStorage/tests/FileStorage.Tests/`
Expected: All 6 tests pass.

- [ ] **Step 5: Commit**

```bash
git add modules/FileStorage/tests/
git commit -m "test: add FileStorage unit tests with in-memory provider"
```

---

## Task 13: Validate Pages + Final Verification

**Files:** None (validation only)

- [ ] **Step 1: Run page validation**

Run: `npm run validate-pages`
Expected: No mismatches. FileStorage/Browse should be found in the pages registry.

- [ ] **Step 2: Run full solution build**

Run: `dotnet build`
Expected: Build succeeded. 0 Warning(s). 0 Error(s).

- [ ] **Step 3: Run all tests**

Run: `dotnet test`
Expected: All tests pass, including new FileStorage tests.

- [ ] **Step 4: Run lint check**

Run: `npm run check`
Expected: No lint or formatting errors.

- [ ] **Step 5: Commit any fixes**

If any issues were found in steps 1-4, fix them and commit:

```bash
git add -A
git commit -m "fix: resolve build/lint/test issues"
```
