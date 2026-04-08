using SimpleModule.Datasets.Contracts;

namespace SimpleModule.Datasets.Processing;

public interface IDatasetProcessor
{
    DatasetFormat Format { get; }

    Task<DatasetProcessingResult> ProcessAsync(Stream content, CancellationToken ct);
}

public sealed class DatasetProcessingResult
{
    public int? SourceSrid { get; init; }
    public int TargetSrid { get; init; } = 4326;
    public BoundingBoxDto? BoundingBox { get; init; }
    public long? FeatureCount { get; init; }
    public DatasetMetadata Metadata { get; init; } = new();

    /// <summary>
    /// Normalized (reprojected + canonicalized) GeoJSON bytes for vector datasets.
    /// Null for tile/raster sources which do not produce a normalized GeoJSON cache.
    /// </summary>
#pragma warning disable CA1819
    public byte[]? NormalizedGeoJson { get; init; }
#pragma warning restore CA1819
}
