using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace SimpleModule.Storage.S3;

public sealed class S3StorageProvider : IStorageProvider, IDisposable
{
    private readonly AmazonS3Client _client;
    private readonly string _bucketName;

    public S3StorageProvider(IOptions<S3StorageOptions> options)
    {
        var opts = options.Value;
        var config = new AmazonS3Config
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(opts.Region),
            ForcePathStyle = opts.ForcePathStyle,
        };

        if (opts.ServiceUrl is not null)
        {
            config.ServiceURL = opts.ServiceUrl.ToString();
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
