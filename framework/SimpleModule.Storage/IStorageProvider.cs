namespace SimpleModule.Storage;

public interface IStorageProvider
{
    Task<StorageResult> SaveAsync(
        string path,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default
    );
    Task<Stream?> GetAsync(string path, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string path, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StorageEntry>> ListAsync(
        string prefix,
        CancellationToken cancellationToken = default
    );
}
