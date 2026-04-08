using SimpleModule.Core;

namespace SimpleModule.Datasets.Contracts;

/// <summary>
/// Full set of metadata extracted during dataset processing. Persisted as JSON on the Dataset row.
/// </summary>
[Dto]
public sealed class DatasetMetadata
{
    public CommonMetadata Common { get; set; } = new();
    public VectorMetadata? Vector { get; set; }
    public RasterMetadata? Raster { get; set; }
    public TileMetadata? Tiles { get; set; }
    public List<DatasetDerivative> Derivatives { get; set; } = [];
}

[Dto]
public sealed class CommonMetadata
{
    public string SourceFormat { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string? ContentHash { get; set; }
    public int? SourceSrid { get; set; }
    public int? TargetSrid { get; set; }
    public BoundingBoxDto? BoundingBox { get; set; }
    public double ProcessingDurationMs { get; set; }
    public string ProcessorVersion { get; set; } = "1.0.0";
}

[Dto]
public sealed class VectorMetadata
{
    public int FeatureCount { get; set; }
    public List<string> GeometryTypes { get; set; } = [];
    public List<AttributeField> AttributeSchema { get; set; } = [];
    public List<string> LayerNames { get; set; } = [];
    public string? Encoding { get; set; }
    public string? CrsWkt { get; set; }
}

[Dto]
public sealed class AttributeField
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public List<string> SampleValues { get; set; } = [];
}

[Dto]
public sealed class RasterMetadata
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int BandCount { get; set; }
    public List<string> BandTypes { get; set; } = [];
    public double? NoDataValue { get; set; }
    public double PixelSizeX { get; set; }
    public double PixelSizeY { get; set; }
    public List<int> OverviewLevels { get; set; } = [];
    public string? Compression { get; set; }
    public string? CrsWkt { get; set; }
}

[Dto]
public sealed class TileMetadata
{
    public string? TileFormat { get; set; }
    public int MinZoom { get; set; }
    public int MaxZoom { get; set; }
    public double CenterLon { get; set; }
    public double CenterLat { get; set; }
    public long TileCount { get; set; }
    public int HeaderVersion { get; set; }
    public List<string> LayerNames { get; set; } = [];
}

[Dto]
public sealed class DatasetDerivative
{
    public DatasetFormat Format { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
