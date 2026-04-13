using System.Diagnostics;
using System.Text.Json;
using SimpleModule.Datasets.Contracts;
using SimpleModule.Datasets.Infrastructure;

namespace SimpleModule.Datasets.Processing;

/// <summary>
/// Common base for vector formats that GDAL/OGR can read natively (Shapefile, KML, GeoPackage).
/// Shells out to ogrinfo -json for metadata and ogr2ogr for GeoJSON normalization.
/// </summary>
public abstract class OgrVectorProcessor : IDatasetProcessor
{
    public abstract DatasetFormat Format { get; }

    protected abstract string TempFileExtension { get; }

    /// <summary>
    /// For Shapefiles (.zip), unzip and return the .shp path.
    /// Default returns the input path unchanged.
    /// </summary>
    protected virtual Task<string> PrepareInputAsync(
        string inputPath,
        string tempDir,
        CancellationToken ct
    ) => Task.FromResult(inputPath);

    public async Task<DatasetProcessingResult> ProcessAsync(Stream content, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        using var tmp = new TempDirectory("ogr");
        var inputPath = Path.Combine(tmp.Path, $"input{TempFileExtension}");

        await using (var fs = File.Create(inputPath))
        {
            await content.CopyToAsync(fs, ct);
        }

        var ogrPath = await PrepareInputAsync(inputPath, tmp.Path, ct);

        var infoJson = await CliRunner.RunAsync("ogrinfo", ["-json", "-al", "-so", ogrPath], ct);
        using var ogrInfo = JsonDocument.Parse(infoJson);
        var parsed = ParseOgrInfo(ogrInfo);

        var geojsonPath = Path.Combine(tmp.Path, "output.geojson");
        await CliRunner.RunAsync(
            "ogr2ogr",
            ["-f", "GeoJSON", "-t_srs", "EPSG:4326", geojsonPath, ogrPath],
            ct
        );
        var normalizedGeoJson = await File.ReadAllBytesAsync(geojsonPath, ct);

        var metadata = new DatasetMetadata
        {
            Common = new CommonMetadata
            {
                SourceFormat = Format.ToString(),
                SourceSrid = parsed.Srid ?? 4326,
                TargetSrid = 4326,
                BoundingBox = parsed.BoundingBox,
                ProcessingDurationMs = sw.Elapsed.TotalMilliseconds,
            },
            Vector = new VectorMetadata
            {
                FeatureCount = parsed.FeatureCount,
                GeometryTypes = parsed.GeometryTypes,
                AttributeSchema = parsed.Attributes,
                LayerNames = parsed.LayerNames,
                CrsWkt = parsed.CrsWkt,
            },
        };

        return new DatasetProcessingResult
        {
            SourceSrid = parsed.Srid ?? 4326,
            TargetSrid = 4326,
            BoundingBox = parsed.BoundingBox,
            FeatureCount = parsed.FeatureCount,
            Metadata = metadata,
            NormalizedGeoJson = normalizedGeoJson,
        };
    }

    private static OgrInfoParsed ParseOgrInfo(JsonDocument doc)
    {
        var result = new OgrInfoParsed();
        var root = doc.RootElement;

        if (!root.TryGetProperty("layers", out var layers))
            return result;

        double minX = double.PositiveInfinity,
            minY = double.PositiveInfinity;
        double maxX = double.NegativeInfinity,
            maxY = double.NegativeInfinity;

        foreach (var layer in layers.EnumerateArray())
        {
            if (layer.TryGetProperty("name", out var layerName))
            {
                result.LayerNames.Add(layerName.GetString() ?? "");
            }

            if (layer.TryGetProperty("featureCount", out var fc))
            {
                result.FeatureCount += fc.GetInt32();
            }

            if (
                layer.TryGetProperty("geometryFields", out var geoFields)
                && geoFields.GetArrayLength() > 0
            )
            {
                foreach (var gf in geoFields.EnumerateArray())
                {
                    if (gf.TryGetProperty("type", out var gt))
                    {
                        var gtype = gt.GetString() ?? "";
                        if (gtype.Length > 0 && !result.GeometryTypes.Contains(gtype))
                        {
                            result.GeometryTypes.Add(gtype);
                        }
                    }

                    if (gf.TryGetProperty("extent", out var extent) && extent.GetArrayLength() >= 4)
                    {
                        var eMinX = extent[0].GetDouble();
                        var eMinY = extent[1].GetDouble();
                        var eMaxX = extent[2].GetDouble();
                        var eMaxY = extent[3].GetDouble();

                        if (eMinX < minX)
                            minX = eMinX;
                        if (eMinY < minY)
                            minY = eMinY;
                        if (eMaxX > maxX)
                            maxX = eMaxX;
                        if (eMaxY > maxY)
                            maxY = eMaxY;
                    }

                    if (gf.TryGetProperty("coordinateSystem", out var cs))
                    {
                        if (cs.TryGetProperty("wkt", out var wkt))
                        {
                            result.CrsWkt = wkt.GetString();
                        }
                        if (
                            cs.TryGetProperty("projjson", out var projjson)
                            && projjson.TryGetProperty("id", out var id)
                            && id.TryGetProperty("code", out var code)
                        )
                        {
                            result.Srid = code.GetInt32();
                        }
                    }
                }
            }

            if (layer.TryGetProperty("fields", out var fields))
            {
                foreach (var field in fields.EnumerateArray())
                {
                    var name = field.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                    var type = field.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "";
                    result.Attributes.Add(new AttributeField { Name = name, Type = type });
                }
            }
        }

        if (!double.IsInfinity(minX))
        {
            result.BoundingBox = new BoundingBoxDto
            {
                MinX = minX,
                MinY = minY,
                MaxX = maxX,
                MaxY = maxY,
            };
        }

        return result;
    }

    private sealed class OgrInfoParsed
    {
        public int FeatureCount { get; set; }
        public List<string> GeometryTypes { get; } = [];
        public List<AttributeField> Attributes { get; } = [];
        public List<string> LayerNames { get; } = [];
        public BoundingBoxDto? BoundingBox { get; set; }
        public int? Srid { get; set; }
        public string? CrsWkt { get; set; }
    }
}

public sealed class ShapefileProcessor : OgrVectorProcessor
{
    public override DatasetFormat Format => DatasetFormat.Shapefile;
    protected override string TempFileExtension => ".zip";

    protected override async Task<string> PrepareInputAsync(
        string inputPath,
        string tempDir,
        CancellationToken ct
    )
    {
        var extractDir = Path.Combine(tempDir, "shp");
        Directory.CreateDirectory(extractDir);

        await CliRunner.RunAsync("unzip", ["-o", inputPath, "-d", extractDir], ct);

        var shpFile = Directory
            .EnumerateFiles(extractDir, "*.shp", SearchOption.AllDirectories)
            .FirstOrDefault();

        return shpFile
            ?? throw new InvalidOperationException(
                "No .shp file found inside the uploaded zip archive."
            );
    }
}

public sealed class KmlProcessor : OgrVectorProcessor
{
    public override DatasetFormat Format => DatasetFormat.Kml;
    protected override string TempFileExtension => ".kml";
}

public sealed class GeoPackageProcessor : OgrVectorProcessor
{
    public override DatasetFormat Format => DatasetFormat.GeoPackage;
    protected override string TempFileExtension => ".gpkg";
}
