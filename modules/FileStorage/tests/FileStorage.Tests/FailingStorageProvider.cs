using SimpleModule.Storage;

namespace SimpleModule.FileStorage.Tests;

/// <summary>
/// A storage provider that wraps another provider but can be configured to fail on specific operations.
/// Used to test error handling and cleanup behavior.
/// </summary>
public sealed class FailingStorageProvider(IStorageProvider inner) : IStorageProvider
{
    public bool FailOnSave { get; set; }
    public bool FailOnDelete { get; set; }

    public async Task<StorageResult> SaveAsync(
        string path,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default
    )
    {
        if (FailOnSave)
        {
            throw new InvalidOperationException("Storage save failed.");
        }

        return await inner.SaveAsync(path, content, contentType, cancellationToken);
    }

    public Task<Stream?> GetAsync(string path, CancellationToken cancellationToken = default) =>
        inner.GetAsync(path, cancellationToken);

    public async Task<bool> DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        if (FailOnDelete)
        {
            throw new InvalidOperationException("Storage delete failed.");
        }

        return await inner.DeleteAsync(path, cancellationToken);
    }

    public Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default) =>
        inner.ExistsAsync(path, cancellationToken);

    public Task<IReadOnlyList<StorageEntry>> ListAsync(
        string prefix,
        CancellationToken cancellationToken = default
    ) => inner.ListAsync(prefix, cancellationToken);
}
