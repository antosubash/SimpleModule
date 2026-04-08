using System.Diagnostics;
using System.Text.Json;
using SimpleModule.Datasets.Contracts;

namespace SimpleModule.Datasets.Processing;

/// <summary>
/// Fully functional GeoJSON processor. Parses a FeatureCollection (or single Feature/Geometry),
/// computes a bbox, feature count, attribute schema and geometry type summary.
/// Assumes input is in EPSG:4326 (GeoJSON spec default) — no reprojection performed.
/// </summary>
public sealed class GeoJsonProcessor : IDatasetProcessor
{
    public DatasetFormat Format => DatasetFormat.GeoJson;

    public async Task<DatasetProcessingResult> ProcessAsync(Stream content, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        using var doc = await JsonDocument.ParseAsync(content, cancellationToken: ct);
        var root = doc.RootElement;

        double minX = double.PositiveInfinity,
            minY = double.PositiveInfinity;
        double maxX = double.NegativeInfinity,
            maxY = double.NegativeInfinity;

        long featureCount = 0;
        var geometryTypes = new HashSet<string>(StringComparer.Ordinal);
        var attributes = new Dictionary<string, AttributeField>(StringComparer.Ordinal);

        void ConsumeFeature(JsonElement feature)
        {
            featureCount++;
            if (
                feature.TryGetProperty("geometry", out var geom)
                && geom.ValueKind == JsonValueKind.Object
            )
            {
                if (geom.TryGetProperty("type", out var gt) && gt.ValueKind == JsonValueKind.String)
                {
                    geometryTypes.Add(gt.GetString() ?? "Unknown");
                }
                if (geom.TryGetProperty("coordinates", out var coords))
                {
                    GeoJsonBboxWalker.Expand(coords, ref minX, ref minY, ref maxX, ref maxY);
                }
            }
            if (
                feature.TryGetProperty("properties", out var props)
                && props.ValueKind == JsonValueKind.Object
            )
            {
                foreach (var prop in props.EnumerateObject())
                {
                    if (!attributes.TryGetValue(prop.Name, out var field))
                    {
                        field = new AttributeField
                        {
                            Name = prop.Name,
                            Type = prop.Value.ValueKind.ToString(),
                        };
                        attributes[prop.Name] = field;
                    }
                    if (field.SampleValues.Count < 5)
                    {
                        field.SampleValues.Add(prop.Value.ToString());
                    }
                }
            }
        }

        if (root.TryGetProperty("type", out var typeEl) && typeEl.ValueKind == JsonValueKind.String)
        {
            var type = typeEl.GetString();
            if (type == "FeatureCollection" && root.TryGetProperty("features", out var features))
            {
                foreach (var feature in features.EnumerateArray())
                {
                    ConsumeFeature(feature);
                }
            }
            else if (type == "Feature")
            {
                ConsumeFeature(root);
            }
            else if (root.TryGetProperty("coordinates", out var coords))
            {
                featureCount = 1;
                geometryTypes.Add(type ?? "Unknown");
                GeoJsonBboxWalker.Expand(coords, ref minX, ref minY, ref maxX, ref maxY);
            }
        }

        var bbox = double.IsInfinity(minX)
            ? null
            : new BoundingBoxDto
            {
                MinX = minX,
                MinY = minY,
                MaxX = maxX,
                MaxY = maxY,
            };

        var metadata = new DatasetMetadata
        {
            Common = new CommonMetadata
            {
                SourceFormat = nameof(DatasetFormat.GeoJson),
                SourceSrid = 4326,
                TargetSrid = 4326,
                BoundingBox = bbox,
                ProcessingDurationMs = sw.Elapsed.TotalMilliseconds,
            },
            Vector = new VectorMetadata
            {
                FeatureCount = (int)featureCount,
                GeometryTypes = [.. geometryTypes],
                AttributeSchema = [.. attributes.Values],
                CrsWkt = null,
            },
        };

        // Re-serialize the raw bytes as the normalized cache. Parse already validated it.
        content.Position = 0;
        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, ct);

        return new DatasetProcessingResult
        {
            SourceSrid = 4326,
            TargetSrid = 4326,
            BoundingBox = bbox,
            FeatureCount = featureCount,
            Metadata = metadata,
            NormalizedGeoJson = ms.ToArray(),
        };
    }
}
