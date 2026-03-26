# FileStorage Design Spec

## Overview

A general-purpose file storage solution for SimpleModule. Split into two layers:

1. **Framework layer** — storage provider abstraction and implementations in separate packages
2. **Module layer** — FileStorage module with DB metadata tracking, endpoints, permissions, settings, and a file browser UI

Single provider per deployment, configured via `appsettings.json`. Files organized in virtual folders.

---

## Project Structure

### Framework (4 new projects)

```
framework/
├── SimpleModule.Storage/              # Abstraction: IStorageProvider, models, config
├── SimpleModule.Storage.Local/        # LocalStorageProvider (System.IO, no external deps)
├── SimpleModule.Storage.Azure/        # AzureBlobStorageProvider (Azure.Storage.Blobs)
└── SimpleModule.Storage.S3/           # S3StorageProvider (AWSSDK.S3 — AWS, MinIO, R2, etc.)
```

### Module (follows existing module pattern)

```
modules/FileStorage/
├── src/
│   ├── FileStorage.Contracts/         # IFileStorageContracts, StoredFile DTO, FileStorageId
│   └── FileStorage/                   # Module: DB metadata, endpoints, views, service
└── tests/
    └── FileStorage.Tests/
```

### Dependency graph

```
SimpleModule.Storage              ← no external deps, just the abstraction
SimpleModule.Storage.Local        → SimpleModule.Storage
SimpleModule.Storage.Azure        → SimpleModule.Storage, Azure.Storage.Blobs
SimpleModule.Storage.S3           → SimpleModule.Storage, AWSSDK.S3

FileStorage.Contracts             → SimpleModule.Core
FileStorage                       → SimpleModule.Core, SimpleModule.Storage, FileStorage.Contracts
FileStorage.Tests                 → FileStorage, SimpleModule.Tests.Shared

SimpleModule.Host                 → FileStorage, SimpleModule.Storage.Local (or .Azure/.S3)
```

---

## Storage Abstraction (`SimpleModule.Storage`)

### `IStorageProvider`

```csharp
public interface IStorageProvider
{
    Task<StorageResult> SaveAsync(string path, Stream content, string contentType, CancellationToken ct = default);
    Task<Stream?> GetAsync(string path, CancellationToken ct = default);
    Task<bool> DeleteAsync(string path, CancellationToken ct = default);
    Task<bool> ExistsAsync(string path, CancellationToken ct = default);
    Task<IReadOnlyList<StorageEntry>> ListAsync(string prefix, CancellationToken ct = default);
}
```

### Models

```csharp
public sealed record StorageResult(string Path, long Size, string ContentType);

public sealed record StorageEntry(
    string Path,
    string Name,
    long Size,
    string ContentType,
    DateTimeOffset LastModified,
    bool IsFolder
);
```

### Path conventions

- Forward slashes only, no leading slash: `products/images/photo.jpg`
- Each provider normalizes to its native format internally
- Trailing slashes ignored/trimmed

### Configuration

```json
{
  "Storage": {
    "Provider": "Local",
    "Local": {
      "BasePath": "./storage"
    },
    "Azure": {
      "ConnectionString": "...",
      "ContainerName": "files"
    },
    "S3": {
      "ServiceUrl": "https://s3.amazonaws.com",
      "BucketName": "my-bucket",
      "AccessKey": "...",
      "SecretKey": "...",
      "Region": "us-east-1",
      "ForcePathStyle": false
    }
  }
}
```

`ForcePathStyle: true` for MinIO, Cloudflare R2, DigitalOcean Spaces, and other S3-compatible services that use path-style addressing.

### DI Registration

Each provider package exposes one extension method:

```csharp
// SimpleModule.Storage.Local
services.AddLocalStorage(configuration);

// SimpleModule.Storage.Azure
services.AddAzureBlobStorage(configuration);

// SimpleModule.Storage.S3
services.AddS3Storage(configuration);
```

Each binds its options section and registers `IStorageProvider` as a singleton. The Host calls the one matching its deployment.

---

## Provider Implementations

### `SimpleModule.Storage.Local`

- Stores files on disk relative to `BasePath`
- `SaveAsync`: creates directories via `Directory.CreateDirectory`, writes via `FileStream`
- `ListAsync`: `Directory.GetFiles` + `Directory.GetDirectories`
- `ExistsAsync`: `File.Exists`
- Converts `/` to `Path.DirectorySeparatorChar` internally
- No external NuGet dependencies

### `SimpleModule.Storage.Azure`

- Depends on `Azure.Storage.Blobs`
- `BlobServiceClient` from connection string, single container
- Paths map directly to blob names (folders are virtual via `/` delimiter)
- `ListAsync`: `GetBlobsByHierarchyAsync` with delimiter `/` for folder simulation
- `SaveAsync`: `BlobClient.UploadAsync` with `BlobHttpHeaders` for content type

### `SimpleModule.Storage.S3`

- Depends on `AWSSDK.S3`
- `AmazonS3Client` with explicit `ServiceURL` + `ForcePathStyle` for S3-compatible services
- Paths map to S3 object keys within configured bucket
- `ListAsync`: `ListObjectsV2Async` with `Prefix` and `Delimiter` for folder simulation
- `SaveAsync`: `PutObjectAsync` with content type metadata

### Shared behavior

- All methods accept `CancellationToken`
- `GetAsync` returns `null` if file doesn't exist (no throwing)
- `DeleteAsync` returns `false` if file didn't exist, `true` if deleted
- Paths normalized: no leading slash, forward slashes only, trimmed

---

## FileStorage Module

### Contracts (`FileStorage.Contracts`)

```csharp
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

**`StoredFile` DTO:**

```csharp
[Dto]
public class StoredFile
{
    public FileStorageId Id { get; set; }
    public string FileName { get; set; }
    public string StoragePath { get; set; }
    public string ContentType { get; set; }
    public long Size { get; set; }
    public string? Folder { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
```

**`FileStorageId`** — strongly-typed ID via Vogen.

### Service (`FileStorageService`)

- Injects `IStorageProvider` (framework) + `FileStorageDbContext`
- Upload: validates file size/extension against settings → saves to storage provider → creates DB record
- Delete: removes from storage provider → deletes DB record
- Download: looks up DB record → streams from storage provider
- Folder listing: queries DB for distinct `Folder` values with prefix

### Database

- `FileStorageDbContext` with `DbSet<StoredFile>`
- Entity configuration: `FileStorageId` as PK with value generation, required fields, index on `Folder`
- Module schema isolation via `ApplyModuleSchema("FileStorage", ...)`

### Endpoints

| Type | Route | Method | Permission | Description |
|------|-------|--------|------------|-------------|
| `IEndpoint` | `GET /` | GetAll | FileStorage.View | List files, optional `?folder=` query |
| `IEndpoint` | `GET /{id}` | GetById | FileStorage.View | Get file metadata |
| `IEndpoint` | `GET /{id}/download` | Download | FileStorage.View | Stream file content |
| `IEndpoint` | `POST /` | Upload | FileStorage.Upload | Upload file (`IFormFile`) |
| `IEndpoint` | `DELETE /{id}` | Delete | FileStorage.Delete | Delete file |
| `IEndpoint` | `GET /folders` | ListFolders | FileStorage.View | List folders, optional `?parent=` |
| `IViewEndpoint` | `GET /browse` | Browse | FileStorage.View | File browser UI |

### Permissions

```csharp
public sealed class FileStoragePermissions : IModulePermissions
{
    public const string View = "FileStorage.View";
    public const string Upload = "FileStorage.Upload";
    public const string Delete = "FileStorage.Delete";
}
```

### Settings

| Key | Display Name | Default | Type |
|-----|-------------|---------|------|
| `FileStorage.MaxFileSizeMb` | Max File Size (MB) | `50` | Number |
| `FileStorage.AllowedExtensions` | Allowed File Extensions | `.jpg,.jpeg,.png,.gif,.pdf,.doc,.docx,.xls,.xlsx,.zip` | Text |

Validated in `FileStorageService` before saving. Rejected with a clear error message on failure.

### Menu

```csharp
menus.Add(new MenuItem
{
    Title = "Files",
    Icon = "folder",
    Url = "/files/browse",
    Permission = FileStoragePermissions.View,
    Order = 50
});
```

### Module class

```csharp
[Module("FileStorage", RoutePrefix = "/api/files", ViewPrefix = "/files")]
public class FileStorageModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<FileStorageDbContext>(configuration, "FileStorage");
        services.AddScoped<IFileStorageContracts, FileStorageService>();
    }

    public void ConfigureMenu(IMenuBuilder menus) { /* as above */ }
    public void ConfigureSettings(ISettingsBuilder settings) { /* as above */ }
    public void ConfigurePermissions(PermissionRegistryBuilder builder) { /* register permissions */ }
}
```

---

## Frontend UI

### Single view: `Browse` (`/files/browse`)

**Props:**

```typescript
interface BrowseProps {
  files: StoredFile[];
  folders: string[];
  currentFolder: string | null;
  parentFolder: string | null;
}
```

**Layout:**
- Top bar: breadcrumb navigation from folder path + upload button
- Main area: table showing folders and files in current directory
- Folders render as clickable rows that navigate deeper
- Files show: name, size (human-readable), content type, date, download + delete actions

**Interactions:**
- Click folder → Inertia navigation to `/files/browse?folder={path}`
- Click breadcrumb → navigate up the hierarchy
- Upload → file input, POST multipart to `POST /api/files?folder={current}`
- Download → direct browser navigation to `GET /api/files/{id}/download`
- Delete → confirmation dialog → `DELETE /api/files/{id}` → page refresh

**Components:** Uses `@simplemodule/ui` — table/DataGrid, Button, breadcrumbs from folder path.

**Pages registry:**

```typescript
export const pages: Record<string, unknown> = {
  'FileStorage/Browse': () => import('../Views/Browse'),
};
```

---

## Testing

- Unit tests for `FileStorageService` using a fake `IStorageProvider` (in-memory dictionary)
- Unit tests for each provider implementation (Local uses temp directory, Azure/S3 use mocked SDK clients)
- Integration tests using `SimpleModuleWebApplicationFactory` with Local provider
- Validation tests: file size limits, extension filtering, folder listing
