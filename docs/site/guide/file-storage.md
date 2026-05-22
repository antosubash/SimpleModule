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

## Signed URLs

Use `ISignedUrlGenerator` when you need to hand a recipient a time-bound, anonymous link to a protected endpoint — typical for download links pasted into emails, share-by-link flows, or browser redirects to a file the user is allowed to fetch right now but not in general.

```csharp
public sealed class ShareReportEndpoint(ISignedUrlGenerator urls) : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/reports/{id}/share", (ReportId id) =>
        {
            var url = urls.Sign(
                path: $"/api/reports/{id}/export",
                expiresAt: DateTimeOffset.UtcNow.AddMinutes(15),
                purpose: "report-export");

            return Results.Ok(new { url });
        });
}
```

On the endpoint that consumes the link, gate it with `RequireSignedUrl(purpose)`:

```csharp
public sealed class ExportByLinkEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/reports/{id}/export", Handle)
           .RequireSignedUrl(purpose: "report-export");
}
```

`RequireSignedUrl` calls `AllowAnonymous()` and replaces the auth check with a signature check — a missing, tampered, expired, or wrong-purpose link returns `403`. The validated `SignedUrlClaims` (path, purpose, expiry) are stashed on `HttpContext.Items` and retrievable via `httpContext.GetSignedUrlClaims()` if the handler needs them.

The `purpose` string is a free-form scope. Use distinct purposes per flow (`report-export`, `email-confirm`, `account-unlock`) so a leaked link from one flow cannot be replayed against another endpoint.

## Next Steps

- [Configuration](/reference/configuration) -- all storage configuration options
- [Deployment](/advanced/deployment) -- production storage configuration
