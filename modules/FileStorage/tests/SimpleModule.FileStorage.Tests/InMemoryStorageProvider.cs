using System.Collections.Concurrent;
using SimpleModule.Storage;

namespace SimpleModule.FileStorage.Tests;

public sealed class InMemoryStorageProvider : IStorageProvider
{
    private readonly ConcurrentDictionary<
        string,
        (byte[] Data, string ContentType, DateTimeOffset Modified)
    > _files = new();

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
