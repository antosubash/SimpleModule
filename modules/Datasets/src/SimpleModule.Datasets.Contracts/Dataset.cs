using SimpleModule.Core;
using SimpleModule.Core.Entities;

namespace SimpleModule.Datasets.Contracts;

[NoDtoGeneration]
public sealed class Dataset : FullAuditableEntity<DatasetId>
{
    public string Name { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string? ContentHash { get; set; }
    public DatasetFormat Format { get; set; }
    public DatasetStatus Status { get; set; }
    public int? SourceSrid { get; set; }
    public int? Srid { get; set; }
    public double? BboxMinX { get; set; }
    public double? BboxMinY { get; set; }
    public double? BboxMaxX { get; set; }
    public double? BboxMaxY { get; set; }
    public long? FeatureCount { get; set; }
    public long SizeBytes { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public string? NormalizedPath { get; set; }
    public string? MetadataJson { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
}
