using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleModule.Map.Contracts;

namespace SimpleModule.Map.EntityConfigurations;

public class LayerSourceConfiguration : IEntityTypeConfiguration<LayerSource>
{
    /// <summary>
    /// Toggles mapping of the spatial <see cref="LayerSource.Coverage"/> column.
    /// Defaults to <c>true</c> (production providers: PostGIS, SQL Server geometry,
    /// SpatiaLite). Set to <c>false</c> in environments without spatial support
    /// such as in-memory SQLite test fixtures.
    /// </summary>
    public static bool EnableSpatial { get; set; } = true;

    public void Configure(EntityTypeBuilder<LayerSource> builder)
    {
        var enableSpatial = EnableSpatial;
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Description).HasMaxLength(2000);
        builder.Property(s => s.Url).IsRequired().HasMaxLength(2048);
        builder.Property(s => s.Attribution).HasMaxLength(500);
        builder.Property(s => s.ConcurrencyStamp).IsConcurrencyToken();

        if (enableSpatial)
        {
            // Spatial coverage column. NetTopologySuite Geometry maps to:
            //   - PostgreSQL: geometry(Geometry, 4326) via PostGIS
            //   - SQL Server: geometry
            //   - SQLite:     SpatiaLite GEOMETRY
            // SRID 4326 (WGS84) is the standard for web maps (MapLibre, GeoJSON).
            builder.Property(s => s.Coverage).HasColumnType("geometry");
        }
        else
        {
            builder.Ignore(s => s.Coverage);
        }

        var jsonOptions = new JsonSerializerOptions();

        builder
            .Property(s => s.Bounds)
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, jsonOptions),
                v =>
                    string.IsNullOrEmpty(v)
                        ? null
                        : JsonSerializer.Deserialize<double[]>(v, jsonOptions),
                new ValueComparer<double[]?>(
                    (a, b) =>
                        (a == null && b == null) || (a != null && b != null && a.SequenceEqual(b)),
                    v =>
                        v == null
                            ? 0
                            : v.Aggregate(0, (h, x) => HashCode.Combine(h, x.GetHashCode())),
                    v => v == null ? null : v.ToArray()
                )
            );

        builder.Property(s => s.Metadata).HasJsonDictionaryConversion();

        builder.HasData(GenerateSeedSources());
    }

    /// <summary>
    /// Seed catalog using freely-available demo layers from the official MapLibre
    /// examples and the maplibre-cog-protocol demo. These are stable URLs the project
    /// is happy to depend on for development and integration smoke-testing.
    /// </summary>
    private static LayerSource[] GenerateSeedSources()
    {
        // Fixed deterministic timestamp so HasData isn't dirty on every model snapshot.
        var seededAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // World bbox in WGS84 — every public web tile service covers it.
        double[] worldBounds = [-180, -85, 180, 85];

        return
        [
            // ── Raster basemaps (XYZ) ────────────────────────────────────────────
            new LayerSource
            {
                Id = LayerSourceId.From(new Guid("11111111-1111-1111-1111-000000000001")),
                Name = "OpenStreetMap (raster tiles)",
                Description =
                    "Standard OSM raster tiles. Free for low-volume use; respect the OSMF tile usage policy.",
                Type = LayerSourceType.Xyz,
                Url = "https://tile.openstreetmap.org/{z}/{x}/{y}.png",
                Attribution = "© OpenStreetMap contributors",
                MinZoom = 0,
                MaxZoom = 19,
                Bounds = worldBounds,
                Metadata = [],
                CreatedAt = seededAt,
                UpdatedAt = seededAt,
                ConcurrencyStamp = "seed-osm-xyz",
            },
            // ── WMS (terrestris demo, used in the official MapLibre WMS example) ─
            new LayerSource
            {
                Id = LayerSourceId.From(new Guid("11111111-1111-1111-1111-000000000002")),
                Name = "terrestris OSM-WMS",
                Description =
                    "Public WMS by terrestris. Used in the official MapLibre 'Add a WMS source' example.",
                Type = LayerSourceType.Wms,
                Url = "https://ows.terrestris.de/osm/service",
                Attribution = "© OpenStreetMap contributors, terrestris",
                Bounds = worldBounds,
                Metadata = new Dictionary<string, string>
                {
                    ["layers"] = "OSM-WMS",
                    ["format"] = "image/png",
                    ["version"] = "1.1.1",
                    ["crs"] = "EPSG:3857",
                    ["transparent"] = "true",
                },
                CreatedAt = seededAt,
                UpdatedAt = seededAt,
                ConcurrencyStamp = "seed-terrestris-wms",
            },
            new LayerSource
            {
                Id = LayerSourceId.From(new Guid("11111111-1111-1111-1111-000000000003")),
                Name = "terrestris TOPO-WMS",
                Description = "terrestris topographic WMS overlay layer (transparent).",
                Type = LayerSourceType.Wms,
                Url = "https://ows.terrestris.de/osm/service",
                Attribution = "© OpenStreetMap contributors, terrestris",
                Bounds = worldBounds,
                Metadata = new Dictionary<string, string>
                {
                    ["layers"] = "TOPO-WMS,OSM-Overlay-WMS",
                    ["format"] = "image/png",
                    ["version"] = "1.1.1",
                    ["crs"] = "EPSG:3857",
                    ["transparent"] = "true",
                },
                CreatedAt = seededAt,
                UpdatedAt = seededAt,
                ConcurrencyStamp = "seed-terrestris-topo",
            },
            // ── Vector tiles (MapLibre demotiles) ────────────────────────────────
            new LayerSource
            {
                Id = LayerSourceId.From(new Guid("11111111-1111-1111-1111-000000000004")),
                Name = "MapLibre demotiles (vector)",
                Description = "Official MapLibre demo MVT vector tileset. Free for development.",
                Type = LayerSourceType.VectorTile,
                Url = "https://demotiles.maplibre.org/tiles/{z}/{x}/{y}.pbf",
                Attribution = "MapLibre",
                MinZoom = 0,
                MaxZoom = 14,
                Bounds = worldBounds,
                Metadata = new Dictionary<string, string> { ["sourceLayer"] = "countries" },
                CreatedAt = seededAt,
                UpdatedAt = seededAt,
                ConcurrencyStamp = "seed-maplibre-demotiles",
            },
            // ── PMTiles (Protomaps demo archive used in MapLibre PMTiles example) ─
            new LayerSource
            {
                Id = LayerSourceId.From(new Guid("11111111-1111-1111-1111-000000000005")),
                Name = "Protomaps Firenze (PMTiles)",
                Description =
                    "Public PMTiles vector archive of Florence (ODbL). Used in the MapLibre PMTiles example.",
                Type = LayerSourceType.PmTiles,
                Url = "https://pmtiles.io/protomaps(vector)ODbL_firenze.pmtiles",
                Attribution = "© OpenStreetMap contributors, Protomaps",
                Bounds = [11.154, 43.727, 11.328, 43.823],
                Metadata = new Dictionary<string, string>
                {
                    ["tileType"] = "vector",
                    ["sourceLayer"] = "landuse",
                },
                CreatedAt = seededAt,
                UpdatedAt = seededAt,
                ConcurrencyStamp = "seed-protomaps-firenze",
            },
            // ── COG (geomatico demo Cloud-Optimized GeoTIFF) ─────────────────────
            new LayerSource
            {
                Id = LayerSourceId.From(new Guid("11111111-1111-1111-1111-000000000006")),
                Name = "Geomatico kriging COG (demo)",
                Description =
                    "Public Cloud-Optimized GeoTIFF demo from the maplibre-cog-protocol sample viewer.",
                Type = LayerSourceType.Cog,
                Url = "https://labs.geomatico.es/maplibre-cog-protocol/data/kriging.tif",
                Attribution = "Geomatico",
                Bounds = worldBounds,
                Metadata = [],
                CreatedAt = seededAt,
                UpdatedAt = seededAt,
                ConcurrencyStamp = "seed-geomatico-cog",
            },
            // ── GeoJSON (raw OSM Overpass-style demo: world airports subset) ─────
            new LayerSource
            {
                Id = LayerSourceId.From(new Guid("11111111-1111-1111-1111-000000000007")),
                Name = "MapLibre demotiles point sample (GeoJSON)",
                Description =
                    "Small public GeoJSON FeatureCollection from the MapLibre demo assets.",
                Type = LayerSourceType.GeoJson,
                Url =
                    "https://maplibre.org/maplibre-gl-js/docs/assets/significant-earthquakes-2015.geojson",
                Attribution = "USGS / MapLibre demo",
                Bounds = worldBounds,
                Metadata = new Dictionary<string, string> { ["color"] = "#ef4444" },
                CreatedAt = seededAt,
                UpdatedAt = seededAt,
                ConcurrencyStamp = "seed-maplibre-earthquakes",
            },
        ];
    }
}
