using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.Datasets.Contracts;
using SimpleModule.Storage;

namespace SimpleModule.Datasets.Jobs;

/// <summary>
/// Deletes every blob associated with a soft-deleted dataset: the original upload,
/// the normalized GeoJSON cache, and any derivative files produced by <see cref="ConvertDatasetJob"/>.
/// Runs after <see cref="DatasetsContractsService.DeleteAsync"/> so the HTTP request returns
/// immediately while the (potentially large) storage cleanup happens in the background.
/// </summary>
public sealed partial class PurgeDatasetJob(
    DatasetsDbContext db,
    IStorageProvider storage,
    ILogger<PurgeDatasetJob> logger
) : IModuleJob
{
    public async Task ExecuteAsync(
        IJobExecutionContext context,
        CancellationToken cancellationToken
    )
    {
        var payload = context.GetData<PurgeDatasetJobData>();
        var datasetId = DatasetId.From(payload.DatasetId);

        // The soft-delete query filter hides deleted rows by default — bypass it.
        var row = await db
            .Datasets.AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.Id == datasetId, cancellationToken);
        if (row is null)
        {
            LogDatasetMissing(logger, payload.DatasetId);
            return;
        }

        var paths = new List<string>();
        if (!string.IsNullOrWhiteSpace(row.StoragePath))
        {
            paths.Add(row.StoragePath);
        }
        if (!string.IsNullOrWhiteSpace(row.NormalizedPath))
        {
            paths.Add(row.NormalizedPath);
        }
        if (!string.IsNullOrWhiteSpace(row.MetadataJson))
        {
            var metadata = JsonSerializer.Deserialize<DatasetMetadata>(row.MetadataJson);
            if (metadata is not null)
            {
                paths.AddRange(
                    metadata
                        .Derivatives.Select(d => d.StoragePath)
                        .Where(p => !string.IsNullOrWhiteSpace(p))
                );
            }
        }

        await Task.WhenAll(paths.Select(p => storage.DeleteAsync(p, cancellationToken)));
        LogDatasetPurged(logger, payload.DatasetId, paths.Count);
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Dataset {Id} not found for purge; nothing to delete"
    )]
    private static partial void LogDatasetMissing(ILogger logger, Guid id);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Dataset {Id} purged: {BlobCount} storage path(s) processed"
    )]
    private static partial void LogDatasetPurged(ILogger logger, Guid id, int blobCount);
}

public sealed class PurgeDatasetJobData
{
    public Guid DatasetId { get; set; }
}
