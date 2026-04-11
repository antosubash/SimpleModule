using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.Core.Settings;
using SimpleModule.Datasets.Contracts;
using SimpleModule.Datasets.Entities;
using SimpleModule.Datasets.Jobs;
using SimpleModule.Settings.Contracts;
using SimpleModule.Storage;

namespace SimpleModule.Datasets;

public sealed partial class DatasetsContractsService(
    DatasetsDbContext db,
    IStorageProvider storage,
    IBackgroundJobs jobs,
    ISettingsContracts settings,
    ILogger<DatasetsContractsService> logger
) : IDatasetsContracts
{
    public async Task<IReadOnlyList<DatasetDto>> GetAllAsync(CancellationToken ct = default)
    {
        var rows = await db
            .Datasets.AsNoTracking()
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    public async Task<DatasetDto?> GetByIdAsync(DatasetId id, CancellationToken ct = default)
    {
        var row = await db.Datasets.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, ct);
        return row is null ? null : ToDto(row);
    }

    public async Task<DatasetDto> CreateAsync(
        Stream content,
        string fileName,
        string? name,
        CancellationToken ct = default
    )
    {
        var format = DatasetFormatExtensions.FromFileName(fileName);
        if (format == DatasetFormat.Unknown)
        {
            throw new InvalidOperationException(
                $"Unknown or unsupported dataset format: {fileName}"
            );
        }

        var prefix =
            await settings.GetSettingAsync<string>(
                DatasetsConstants.SettingKeys.StoragePrefix,
                SettingScope.Application
            ) ?? "datasets";

        var id = DatasetId.From(Guid.NewGuid());
        var ext = Path.GetExtension(fileName);
        var storagePath = StoragePathHelper.Combine(prefix, $"{id.Value}/original{ext}");

        using var hasher = SHA256.Create();
        await using var hashing = new CryptoStream(
            content,
            hasher,
            CryptoStreamMode.Read,
            leaveOpen: true
        );
        var saveResult = await storage.SaveAsync(
            storagePath,
            hashing,
            "application/octet-stream",
            ct
        );
        var hash = Convert.ToHexString(hasher.Hash ?? []);

        var row = new Dataset
        {
            Id = id,
            Name = string.IsNullOrWhiteSpace(name)
                ? Path.GetFileNameWithoutExtension(fileName)
                : name,
            OriginalFileName = fileName,
            ContentHash = hash,
            Format = format,
            Status = DatasetStatus.Pending,
            SizeBytes = saveResult.Size,
            StoragePath = saveResult.Path,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            ConcurrencyStamp = Guid.NewGuid().ToString("N"),
        };
        db.Datasets.Add(row);
        await db.SaveChangesAsync(ct);

        await jobs.EnqueueAsync<ProcessDatasetJob>(
            new ProcessDatasetJobData { DatasetId = id.Value },
            ct
        );
        LogDatasetCreated(logger, id.Value, fileName);
        return ToDto(row);
    }

    public async Task DeleteAsync(DatasetId id, CancellationToken ct = default)
    {
        var row = await db.Datasets.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (row is null)
        {
            return;
        }
        row.IsDeleted = true;
        row.DeletedAt = DateTimeOffset.UtcNow;
        row.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        LogDatasetDeleted(logger, id.Value);

        // Hand off blob cleanup (original + normalized + every derivative) to a background
        // job so large uploads don't block the HTTP response.
        await jobs.EnqueueAsync<PurgeDatasetJob>(
            new PurgeDatasetJobData { DatasetId = id.Value },
            ct
        );
    }

    public async Task<Stream?> GetOriginalAsync(DatasetId id, CancellationToken ct = default)
    {
        var row = await db.Datasets.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, ct);
        if (row is null)
        {
            return null;
        }
        return await storage.GetAsync(row.StoragePath, ct);
    }

    public async Task<Stream?> GetDerivativeAsync(
        DatasetId id,
        DatasetFormat format,
        CancellationToken ct = default
    )
    {
        var row = await db.Datasets.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, ct);
        if (row is null)
        {
            return null;
        }
        var metadata = DeserializeMetadata(row.MetadataJson);
        var derivative = metadata?.Derivatives.FirstOrDefault(d => d.Format == format);
        if (derivative is null)
        {
            // Special case: vector "normalized" GeoJSON cache written by ProcessDatasetJob.
            if (format == DatasetFormat.GeoJson && row.NormalizedPath is not null)
            {
                return await storage.GetAsync(row.NormalizedPath, ct);
            }
            return null;
        }
        return await storage.GetAsync(derivative.StoragePath, ct);
    }

    public async Task<string> GetFeaturesGeoJsonAsync(
        DatasetId id,
        BoundingBoxDto? bbox = null,
        int? limit = null,
        CancellationToken ct = default
    )
    {
        var row = await db.Datasets.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, ct);
        if (row is null)
        {
            throw new InvalidOperationException($"Dataset {id.Value} not found");
        }
        if (!row.Format.IsVector() || row.NormalizedPath is null)
        {
            throw new InvalidOperationException(
                "Feature query is only supported for vector datasets that have been processed."
            );
        }

        await using var stream = await storage.GetAsync(row.NormalizedPath, ct);
        if (stream is null)
        {
            throw new InvalidOperationException("Normalized GeoJSON not found in storage.");
        }

        var effectiveLimit =
            limit
            ?? await settings.GetSettingAsync<int?>(
                DatasetsConstants.SettingKeys.FeatureQueryLimit,
                SettingScope.Application
            )
            ?? 1000;

        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var root = doc.RootElement;
        if (
            !root.TryGetProperty("features", out var features)
            || features.ValueKind != JsonValueKind.Array
        )
        {
            return """{"type":"FeatureCollection","features":[]}""";
        }

        using var ms = new MemoryStream();
        await using (var writer = new Utf8JsonWriter(ms))
        {
            writer.WriteStartObject();
            writer.WriteString("type", "FeatureCollection");
            writer.WriteStartArray("features");
            var count = 0;
            foreach (var feature in features.EnumerateArray())
            {
                if (count >= effectiveLimit)
                {
                    break;
                }
                if (bbox is not null && !FeatureIntersectsBbox(feature, bbox))
                {
                    continue;
                }
                feature.WriteTo(writer);
                count++;
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return System.Text.Encoding.UTF8.GetString(ms.GetBuffer().AsSpan(0, (int)ms.Length));
    }

    public async Task<IReadOnlyList<DatasetDto>> FindByBoundingBoxAsync(
        BoundingBoxDto bbox,
        CancellationToken ct = default
    )
    {
        var rows = await db
            .Datasets.AsNoTracking()
            .Where(d =>
                d.BboxMinX != null
                && d.BboxMaxX != null
                && d.BboxMinY != null
                && d.BboxMaxY != null
                && d.BboxMinX <= bbox.MaxX
                && d.BboxMaxX >= bbox.MinX
                && d.BboxMinY <= bbox.MaxY
                && d.BboxMaxY >= bbox.MinY
            )
            .ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    public async Task EnqueueConversionAsync(
        DatasetId id,
        DatasetFormat? targetFormat = null,
        CancellationToken ct = default
    )
    {
        var row = await db.Datasets.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, ct);
        if (row is null)
        {
            throw new InvalidOperationException($"Dataset {id.Value} not found");
        }
        var target = targetFormat ?? await ResolveDefaultDerivativeAsync(row.Format);
        await jobs.EnqueueAsync<ConvertDatasetJob>(
            new ConvertDatasetJobData { DatasetId = id.Value, TargetFormat = (int)target },
            ct
        );
    }

    private async Task<DatasetFormat> ResolveDefaultDerivativeAsync(DatasetFormat source)
    {
        var key = source.IsRaster()
            ? DatasetsConstants.SettingKeys.DefaultRasterConversionFormat
            : DatasetsConstants.SettingKeys.DefaultVectorConversionFormat;
        var name = await settings.GetSettingAsync<string>(key, SettingScope.Application);
        if (!string.IsNullOrWhiteSpace(name) && Enum.TryParse<DatasetFormat>(name, out var parsed))
        {
            return parsed;
        }
        return source.IsRaster() ? DatasetFormat.Cog : DatasetFormat.PmTiles;
    }

    internal static DatasetDto ToDto(Dataset row) =>
        new()
        {
            Id = row.Id.Value,
            Name = row.Name,
            OriginalFileName = row.OriginalFileName,
            Format = row.Format,
            Status = row.Status,
            SourceSrid = row.SourceSrid,
            Srid = row.Srid,
            BoundingBox = row.BboxMinX is null
                ? null
                : new BoundingBoxDto
                {
                    MinX = row.BboxMinX.Value,
                    MinY = row.BboxMinY!.Value,
                    MaxX = row.BboxMaxX!.Value,
                    MaxY = row.BboxMaxY!.Value,
                },
            FeatureCount = row.FeatureCount,
            SizeBytes = row.SizeBytes,
            ErrorMessage = row.ErrorMessage,
            Metadata = DeserializeMetadata(row.MetadataJson),
            CreatedAt = row.CreatedAt,
            ProcessedAt = row.ProcessedAt,
        };

    private static DatasetMetadata? DeserializeMetadata(string? json) =>
        string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<DatasetMetadata>(json);

    private static bool FeatureIntersectsBbox(JsonElement feature, BoundingBoxDto bbox)
    {
        if (
            !feature.TryGetProperty("geometry", out var geometry)
            || geometry.ValueKind != JsonValueKind.Object
            || !geometry.TryGetProperty("coordinates", out var coords)
        )
        {
            return false;
        }

        double minX = double.PositiveInfinity,
            minY = double.PositiveInfinity;
        double maxX = double.NegativeInfinity,
            maxY = double.NegativeInfinity;
        Processing.GeoJsonBboxWalker.Expand(coords, ref minX, ref minY, ref maxX, ref maxY);
        if (double.IsInfinity(minX))
        {
            return false;
        }
        return !(minX > bbox.MaxX || maxX < bbox.MinX || minY > bbox.MaxY || maxY < bbox.MinY);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Dataset created: {Id} ({FileName})")]
    private static partial void LogDatasetCreated(ILogger logger, Guid id, string fileName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Dataset soft-deleted: {Id}")]
    private static partial void LogDatasetDeleted(ILogger logger, Guid id);
}
