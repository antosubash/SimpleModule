using System.Diagnostics;
using System.Text.Json;
using SimpleModule.Datasets.Contracts;
using SimpleModule.Datasets.Infrastructure;

namespace SimpleModule.Datasets.Processing;

/// <summary>
/// Extracts raster metadata from a GeoTIFF/COG using the gdalinfo CLI.
/// NormalizedGeoJson is null because raster sources do not produce a vector cache.
/// </summary>
public sealed class CogProcessor : IDatasetProcessor
{
    public DatasetFormat Format => DatasetFormat.Cog;

    public async Task<DatasetProcessingResult> ProcessAsync(Stream content, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        using var tmp = new TempDirectory("cog-proc");
        var inputPath = Path.Combine(tmp.Path, "input.tif");

        await using (var fs = File.Create(inputPath))
        {
            await content.CopyToAsync(fs, ct);
        }

        var json = await CliRunner.RunAsync("gdalinfo", ["-json", inputPath], ct);
        using var doc = JsonDocument.Parse(json);

        var raster = ParseGdalInfo(doc);
        var srid = raster.Srid ?? 4326;

        var metadata = new DatasetMetadata
        {
            Common = new CommonMetadata
            {
                SourceFormat = nameof(DatasetFormat.Cog),
                SourceSrid = srid,
                TargetSrid = srid,
                BoundingBox = raster.BoundingBox,
                ProcessingDurationMs = sw.Elapsed.TotalMilliseconds,
            },
            Raster = new RasterMetadata
            {
                Width = raster.Width,
                Height = raster.Height,
                BandCount = raster.BandCount,
                PixelSizeX = raster.PixelSizeX,
                PixelSizeY = raster.PixelSizeY,
                Compression = raster.Compression,
            },
        };

        return new DatasetProcessingResult
        {
            SourceSrid = srid,
            TargetSrid = srid,
            BoundingBox = raster.BoundingBox,
            FeatureCount = null,
            Metadata = metadata,
            NormalizedGeoJson = null,
        };
    }

    private static GdalInfoParsed ParseGdalInfo(JsonDocument doc)
    {
        var root = doc.RootElement;
        var result = new GdalInfoParsed();

        if (root.TryGetProperty("size", out var size) && size.GetArrayLength() >= 2)
        {
            result.Width = size[0].GetInt32();
            result.Height = size[1].GetInt32();
        }

        if (root.TryGetProperty("bands", out var bands))
        {
            result.BandCount = bands.GetArrayLength();
        }

        // Extract SRID from projjson (same approach as OgrVectorProcessor)
        if (
            root.TryGetProperty("coordinateSystem", out var cs)
            && cs.TryGetProperty("projjson", out var projjson)
            && projjson.TryGetProperty("id", out var id)
            && id.TryGetProperty("code", out var code)
        )
        {
            result.Srid = code.GetInt32();
        }

        if (root.TryGetProperty("geoTransform", out var gt) && gt.GetArrayLength() >= 6)
        {
            result.PixelSizeX = Math.Abs(gt[1].GetDouble());
            result.PixelSizeY = Math.Abs(gt[5].GetDouble());

            var originX = gt[0].GetDouble();
            var originY = gt[3].GetDouble();
            var pxX = gt[1].GetDouble();
            var pxY = gt[5].GetDouble();

            if (result.Width > 0 && result.Height > 0)
            {
                var x1 = originX;
                var y1 = originY;
                var x2 = originX + pxX * result.Width;
                var y2 = originY + pxY * result.Height;

                result.BoundingBox = new BoundingBoxDto
                {
                    MinX = Math.Min(x1, x2),
                    MinY = Math.Min(y1, y2),
                    MaxX = Math.Max(x1, x2),
                    MaxY = Math.Max(y1, y2),
                };
            }
        }

        if (
            root.TryGetProperty("metadata", out var meta)
            && meta.TryGetProperty("IMAGE_STRUCTURE", out var imgStruct)
            && imgStruct.TryGetProperty("COMPRESSION", out var compression)
        )
        {
            result.Compression = compression.GetString();
        }

        return result;
    }

    private sealed class GdalInfoParsed
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int BandCount { get; set; }
        public int? Srid { get; set; }
        public double PixelSizeX { get; set; }
        public double PixelSizeY { get; set; }
        public string? Compression { get; set; }
        public BoundingBoxDto? BoundingBox { get; set; }
    }
}
