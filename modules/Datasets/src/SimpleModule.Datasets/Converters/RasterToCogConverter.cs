using SimpleModule.Datasets.Contracts;
using SimpleModule.Datasets.Infrastructure;

namespace SimpleModule.Datasets.Converters;

/// <summary>
/// Converts a raster source (GeoTIFF) to Cloud-Optimized GeoTIFF (COG) using gdal_translate.
/// </summary>
public sealed class RasterToCogConverter : IDatasetConverter
{
    public DatasetFormat TargetFormat => DatasetFormat.Cog;

    public bool CanConvertFrom(DatasetFormat source) => source.IsRaster();

    public async Task<Stream> ConvertAsync(
        Stream source,
        DatasetFormat sourceFormat,
        CancellationToken ct
    )
    {
        using var tmp = new TempDirectory("cog");
        var inputPath = Path.Combine(tmp.Path, "input.tif");
        var outputPath = Path.Combine(tmp.Path, "output.tif");

        await using (var fs = File.Create(inputPath))
        {
            await source.CopyToAsync(fs, ct);
        }

        await CliRunner.RunAsync(
            "gdal_translate",
            ["-of", "COG", "-co", "COMPRESS=DEFLATE", inputPath, outputPath],
            ct
        );

        var result = new MemoryStream();
        await using (var fs = File.OpenRead(outputPath))
        {
            await fs.CopyToAsync(result, ct);
        }
        result.Position = 0;
        return result;
    }
}
