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
