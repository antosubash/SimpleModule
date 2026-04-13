using SimpleModule.Datasets.Contracts;
using SimpleModule.Datasets.Infrastructure;

namespace SimpleModule.Datasets.Converters;

/// <summary>
/// Converts a vector GeoJSON source to PMTiles using the tippecanoe CLI.
/// Non-GeoJSON vector formats are normalized to GeoJSON by ConvertDatasetJob
/// before reaching this converter.
/// </summary>
public sealed class VectorToPmTilesConverter : IDatasetConverter
{
    public DatasetFormat TargetFormat => DatasetFormat.PmTiles;

    public bool CanConvertFrom(DatasetFormat source) => source.IsVector();

    public async Task<Stream> ConvertAsync(
        Stream source,
        DatasetFormat sourceFormat,
        CancellationToken ct
    )
    {
        using var tmp = new TempDirectory("pmtiles");
        var inputPath = Path.Combine(tmp.Path, "input.geojson");
        var outputPath = Path.Combine(tmp.Path, "output.pmtiles");

        await using (var fs = File.Create(inputPath))
        {
            await source.CopyToAsync(fs, ct);
        }

        await CliRunner.RunAsync(
            "tippecanoe",
            [
                "-o",
                outputPath,
                "--force",
                "--no-feature-limit",
                "--no-tile-size-limit",
                "--minimum-zoom=0",
                "--maximum-zoom=14",
                inputPath,
            ],
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
