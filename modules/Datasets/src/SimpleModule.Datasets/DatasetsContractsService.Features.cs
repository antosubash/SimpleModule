using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SimpleModule.Core.Settings;
using SimpleModule.Datasets.Contracts;

namespace SimpleModule.Datasets;

public sealed partial class DatasetsContractsService
{
    public async Task<string> GetFeaturesGeoJsonAsync(
        DatasetId id,
        BoundingBoxDto? bbox = null,
        int? limit = null,
        CancellationToken ct = default
    )
    {
        var row = await db.Datasets.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, ct);
        if (row is null)
        {
            throw new InvalidOperationException($"Dataset {id.Value} not found");
        }
        if (!row.Format.IsVector() || row.NormalizedPath is null)
        {
            throw new InvalidOperationException(
                "Feature query is only supported for vector datasets that have been processed."
            );
        }

        await using var stream = await storage.GetAsync(row.NormalizedPath, ct);
        if (stream is null)
        {
            throw new InvalidOperationException("Normalized GeoJSON not found in storage.");
        }

        var effectiveLimit =
            limit
            ?? await settings.GetSettingAsync<int?>(
                DatasetsConstants.SettingKeys.FeatureQueryLimit,
                SettingScope.Application
            )
            ?? 1000;

        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var root = doc.RootElement;
        if (
            !root.TryGetProperty("features", out var features)
            || features.ValueKind != JsonValueKind.Array
        )
        {
            return """{"type":"FeatureCollection","features":[]}""";
        }

        using var ms = new MemoryStream();
        await using (var writer = new Utf8JsonWriter(ms))
        {
            writer.WriteStartObject();
            writer.WriteString("type", "FeatureCollection");
            writer.WriteStartArray("features");
            var count = 0;
            foreach (var feature in features.EnumerateArray())
            {
                if (count >= effectiveLimit)
                {
                    break;
                }
                if (bbox is not null && !FeatureIntersectsBbox(feature, bbox))
                {
                    continue;
                }
                feature.WriteTo(writer);
                count++;
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return System.Text.Encoding.UTF8.GetString(ms.GetBuffer().AsSpan(0, (int)ms.Length));
    }

    private static bool FeatureIntersectsBbox(JsonElement feature, BoundingBoxDto bbox)
    {
        if (
            !feature.TryGetProperty("geometry", out var geometry)
            || geometry.ValueKind != JsonValueKind.Object
            || !geometry.TryGetProperty("coordinates", out var coords)
        )
        {
            return false;
        }

        double minX = double.PositiveInfinity,
            minY = double.PositiveInfinity;
        double maxX = double.NegativeInfinity,
            maxY = double.NegativeInfinity;
        Processing.GeoJsonBboxWalker.Expand(coords, ref minX, ref minY, ref maxX, ref maxY);
        if (double.IsInfinity(minX))
        {
            return false;
        }
        return !(minX > bbox.MaxX || maxX < bbox.MinX || minY > bbox.MaxY || maxY < bbox.MinY);
    }
}
