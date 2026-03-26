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
