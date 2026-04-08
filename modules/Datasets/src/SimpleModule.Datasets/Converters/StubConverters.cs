using SimpleModule.Datasets.Contracts;

namespace SimpleModule.Datasets.Converters;

// Stub converters that register in DI so the registry is complete but throw a clear error
// when invoked. Replace with real implementations as dependencies land:
//   VectorToPmTilesConverter  → tippecanoe CLI wrapper (shell-out) or managed PMTiles writer
//   RasterToCogConverter      → gdal_translate shell-out or BitMiracle.LibTiff + overview generation

public sealed class VectorToPmTilesConverter : IDatasetConverter
{
    public DatasetFormat TargetFormat => DatasetFormat.PmTiles;

    public bool CanConvertFrom(DatasetFormat source) => source.IsVector();

    public Task<Stream> ConvertAsync(
        Stream source,
        DatasetFormat sourceFormat,
        CancellationToken ct
    ) =>
        throw new NotSupportedException(
            "VectorToPmTiles conversion is not yet implemented. Wire up a tippecanoe shell-out or a managed PMTiles writer."
        );
}

public sealed class RasterToCogConverter : IDatasetConverter
{
    public DatasetFormat TargetFormat => DatasetFormat.Cog;

    public bool CanConvertFrom(DatasetFormat source) => source.IsRaster();

    public Task<Stream> ConvertAsync(
        Stream source,
        DatasetFormat sourceFormat,
        CancellationToken ct
    ) =>
        throw new NotSupportedException(
            "RasterToCog conversion is not yet implemented. Wire up gdal_translate -of COG or a BitMiracle.LibTiff-based implementation."
        );
}
