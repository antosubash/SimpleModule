using SimpleModule.Datasets.Contracts;

namespace SimpleModule.Datasets.Converters;

/// <summary>
/// Converts any vector source to GeoJSON. For GeoJSON sources this is a pass-through.
/// Non-GeoJSON vector formats require their respective processors to produce a normalized
/// GeoJSON cache first; this converter delegates to that cache via ConvertDatasetJob.
/// </summary>
public sealed class VectorToGeoJsonConverter : IDatasetConverter
{
    public DatasetFormat TargetFormat => DatasetFormat.GeoJson;

    public bool CanConvertFrom(DatasetFormat source) => source.IsVector();

    public async Task<Stream> ConvertAsync(
        Stream source,
        DatasetFormat sourceFormat,
        CancellationToken ct
    )
    {
        if (sourceFormat == DatasetFormat.GeoJson)
        {
            var ms = new MemoryStream();
            await source.CopyToAsync(ms, ct);
            ms.Position = 0;
            return ms;
        }
        throw new NotSupportedException(
            $"Converting {sourceFormat} to GeoJSON requires a processor that produces a normalized GeoJSON cache; read from the dataset's normalized.geojson instead."
        );
    }
}
