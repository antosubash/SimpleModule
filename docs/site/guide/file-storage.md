---
outline: deep
---

# File Storage

SimpleModule provides a file storage abstraction with pluggable providers for local filesystem, AWS S3, and Azure Blob Storage. The FileStorage module adds HTTP endpoints, database tracking, and an admin UI on top of this abstraction.

## Storage Providers

### IStorageProvider Interface

All providers implement a common interface:

```csharp
public interface IStorageProvider
{
    Task<StorageResult> SaveAsync(
        string path,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);
    Task<Stream?> GetAsync(string path, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string path, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StorageEntry>> ListAsync(
        string prefix,
        CancellationToken cancellationToken = default);
}
```

### Local Storage

Stores files on the local filesystem. Best for development and single-server deployments.

```csharp
builder.Services.AddLocalStorage(builder.Configuration);
```

```json
{
  "Storage": {
    "Provider": "Local",
    "Local": {
      "BasePath": "./storage"
    }
  }
}
```

### AWS S3

```csharp
builder.Services.AddS3Storage(builder.Configuration);
```

```json
{
  "Storage": {
    "Provider": "S3",
    "S3": {
      "BucketName": "my-bucket",
      "AccessKey": "your-access-key",
      "SecretKey": "your-secret-key",
      "Region": "us-east-1",
      "ServiceUrl": null,
      "ForcePathStyle": false
    }
  }
}
```

`ServiceUrl` is typed as `Uri?` — supply a valid URI (e.g., `"https://nyc3.digitaloceanspaces.com"`) or leave it as `null` to use the default AWS endpoint for the region. An empty string will not bind. Set `ForcePathStyle` to `true` for path-style URL access on S3-compatible services (MinIO, DigitalOcean Spaces).

### Azure Blob Storage

```csharp
builder.Services.AddAzureBlobStorage(builder.Configuration);
```

```json
{
  "Storage": {
    "Provider": "Azure",
    "Azure": {
      "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=...",
      "ContainerName": "files"
    }
  }
}
```

## FileStorage Module

The FileStorage module provides HTTP endpoints and a database-backed file registry on top of the storage abstraction.

### API Endpoints

| Method | Route | Permission | Description |
|--------|-------|------------|-------------|
| `POST` | `/api/files/` | `FileStorage.Upload` | Upload a file (multipart form) |
| `GET` | `/api/files/` | `FileStorage.View` | List files (optional `?folder=` filter) |
| `GET` | `/api/files/{id}` | `FileStorage.View` | Get file metadata by ID |
| `GET` | `/api/files/{id}/download` | `FileStorage.View` | Download file content |
| `DELETE` | `/api/files/{id}` | `FileStorage.Delete` | Delete a file |
| `GET` | `/api/files/folders` | `FileStorage.View` | List folders (optional `?parent=` filter) |

### Browse UI

A file browser view at `/files/` lets users navigate folders, upload files, and download or delete existing files. (The module uses `ViewPrefix = "/files"` with the browse endpoint mounted at `/`.)

### Module Settings

| Setting | Default | Description |
|---------|---------|-------------|
| `FileStorage.MaxFileSizeMb` | `50` | Maximum upload size in megabytes |
| `FileStorage.AllowedExtensions` | `.jpg,.jpeg,.png,.gif,.pdf,.doc,.docx,.xls,.xlsx,.zip` | Comma-separated allowed file extensions |

### Using from Other Modules

Inject `IFileStorageContracts` to interact with file storage from any module:

```csharp
public interface IFileStorageContracts
{
    Task<IEnumerable<StoredFile>> GetFilesAsync(string? folder = null, string? userId = null);
    Task<StoredFile?> GetFileByIdAsync(FileStorageId id);
    Task<StoredFile> UploadFileAsync(
        Stream content,
        string fileName,
        string contentType,
        string? folder = null,
        string? userId = null);
    Task DeleteFileAsync(FileStorageId id);
    Task DeleteFileAsync(StoredFile file);
    Task<Stream?> DownloadFileAsync(FileStorageId id);
    Task<Stream?> DownloadFileAsync(StoredFile file);
    Task<IEnumerable<string>> GetFoldersAsync(string? parentFolder = null, string? userId = null);
}
```

## Provider Comparison

| Feature | Local | S3 | Azure |
|---------|-------|----|-------|
| External dependencies | None | AWSSDK.S3 | Azure.Storage.Blobs |
| Folder support | Physical directories | Prefix-based | Prefix-based |
| Pagination | N/A | ListObjectsV2 | GetBlobsByHierarchyAsync |
| Best for | Development, single server | Production, multi-region | Production, Azure ecosystem |

## Path Handling

All paths are normalized to forward slashes internally. The `StoragePathHelper` utility provides safe path operations:

- Path traversal attacks are blocked (paths cannot escape the base directory)
- Leading/trailing slashes are normalized
- File names and folder names are extracted consistently across providers

## Next Steps

- [AI Agents](/guide/ai-agents) -- using file storage with RAG knowledge indexing
- [Configuration](/reference/configuration) -- all storage configuration options
- [Deployment](/advanced/deployment) -- production storage configuration
