using SimpleModule.Core;

namespace SimpleModule.Datasets.Contracts;

[Dto]
public sealed class DatasetDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public DatasetFormat Format { get; set; }
    public DatasetStatus Status { get; set; }
    public int? SourceSrid { get; set; }
    public int? Srid { get; set; }
    public BoundingBoxDto? BoundingBox { get; set; }
    public long? FeatureCount { get; set; }
    public long SizeBytes { get; set; }
    public string? ErrorMessage { get; set; }
    public DatasetMetadata? Metadata { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
}

[Dto]
public sealed class DatasetFeatureDto
{
    public string? Id { get; set; }
    public string GeometryGeoJson { get; set; } = string.Empty;
    public Dictionary<string, string> Properties { get; set; } = new();
}
