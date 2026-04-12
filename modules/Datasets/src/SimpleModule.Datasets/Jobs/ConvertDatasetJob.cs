using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.Core.Events;
using SimpleModule.Datasets.Contracts;
using SimpleModule.Datasets.Contracts.Events;
using SimpleModule.Datasets.Converters;
using SimpleModule.Storage;

namespace SimpleModule.Datasets.Jobs;

public sealed partial class ConvertDatasetJob(
    DatasetsDbContext db,
    IStorageProvider storage,
    DatasetConverterRegistry converters,
    IEventBus eventBus,
    ILogger<ConvertDatasetJob> logger
) : IModuleJob
{
    public async Task ExecuteAsync(
        IJobExecutionContext context,
        CancellationToken cancellationToken
    )
    {
        var payload = context.GetData<ConvertDatasetJobData>();
        var datasetId = DatasetId.From(payload.DatasetId);
        var target = (DatasetFormat)payload.TargetFormat;

        var row = await db.Datasets.FirstOrDefaultAsync(d => d.Id == datasetId, cancellationToken);
        if (row is null)
        {
            LogDatasetMissing(logger, payload.DatasetId);
            return;
        }
        if (row.Status != DatasetStatus.Ready)
        {
            LogSkippedNotReady(logger, payload.DatasetId, row.Status);
            return;
        }

        try
        {
            var converter = converters.Resolve(row.Format, target);

            // For non-GeoJSON vector sources, read the normalized GeoJSON cache
            // produced during processing instead of the raw original file.
            string sourcePath;
            DatasetFormat sourceFormat;
            if (
                row.Format != DatasetFormat.GeoJson
                && row.Format.IsVector()
                && !string.IsNullOrWhiteSpace(row.NormalizedPath)
            )
            {
                sourcePath = row.NormalizedPath;
                sourceFormat = DatasetFormat.GeoJson;
            }
            else
            {
                sourcePath = row.StoragePath;
                sourceFormat = row.Format;
            }

            await using var source =
                await storage.GetAsync(sourcePath, cancellationToken)
                ?? throw new InvalidOperationException($"Source blob missing: {sourcePath}");
            await using var output = await converter.ConvertAsync(
                source,
                sourceFormat,
                cancellationToken
            );
            output.Position = 0;

            var ext = target.FileExtension();
            var derivativePath = StoragePathHelper.Combine(
                Path.GetDirectoryName(row.StoragePath)?.Replace('\\', '/') ?? "datasets",
                $"derivatives/{target}{ext}"
            );
            var save = await storage.SaveAsync(
                derivativePath,
                output,
                "application/octet-stream",
                cancellationToken
            );

            var metadata = string.IsNullOrWhiteSpace(row.MetadataJson)
                ? new DatasetMetadata()
                : JsonSerializer.Deserialize<DatasetMetadata>(row.MetadataJson) ?? new();
            metadata.Derivatives.RemoveAll(d => d.Format == target);
            metadata.Derivatives.Add(
                new DatasetDerivative
                {
                    Format = target,
                    StoragePath = save.Path,
                    SizeBytes = save.Size,
                    CreatedAt = DateTimeOffset.UtcNow,
                }
            );
            row.MetadataJson = JsonSerializer.Serialize(metadata);
            row.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            LogDerivativeCreated(logger, payload.DatasetId, target);
            await eventBus.PublishAsync(
                new DatasetDerivativeCreated(datasetId, target),
                cancellationToken
            );
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogConversionFailed(logger, payload.DatasetId, target, ex);
            // Conversion failure does not mark the dataset as Failed — original is still Ready.
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Dataset {Id} not found for conversion")]
    private static partial void LogDatasetMissing(ILogger logger, Guid id);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Skipping conversion of {Id} — status {Status} is not Ready"
    )]
    private static partial void LogSkippedNotReady(ILogger logger, Guid id, DatasetStatus status);

    [LoggerMessage(Level = LogLevel.Information, Message = "Derivative created: {Id} → {Format}")]
    private static partial void LogDerivativeCreated(ILogger logger, Guid id, DatasetFormat format);

    [LoggerMessage(Level = LogLevel.Error, Message = "Dataset {Id} conversion to {Format} failed")]
    private static partial void LogConversionFailed(
        ILogger logger,
        Guid id,
        DatasetFormat format,
        Exception exception
    );
}

public sealed class ConvertDatasetJobData
{
    public Guid DatasetId { get; set; }
    public int TargetFormat { get; set; }
}
