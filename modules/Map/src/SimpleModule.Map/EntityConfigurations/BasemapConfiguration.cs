using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleModule.Map.Contracts;

namespace SimpleModule.Map.EntityConfigurations;

public class BasemapConfiguration : IEntityTypeConfiguration<Basemap>
{
    /// <summary>
    /// Fixed ids for the basemaps seeded via <see cref="EntityTypeBuilder.HasData"/>.
    /// Reference these from other seeders that link back to this catalog.
    /// </summary>
    public static class SeedIds
    {
        public static readonly BasemapId MapLibreDemotiles = BasemapId.From(
            new Guid("22222222-2222-2222-2222-000000000001")
        );
        public static readonly BasemapId OpenFreeMapLiberty = BasemapId.From(
            new Guid("22222222-2222-2222-2222-000000000002")
        );
        public static readonly BasemapId OpenFreeMapPositron = BasemapId.From(
            new Guid("22222222-2222-2222-2222-000000000003")
        );
        public static readonly BasemapId OpenFreeMapBright = BasemapId.From(
            new Guid("22222222-2222-2222-2222-000000000004")
        );
        public static readonly BasemapId VersatilesColorful = BasemapId.From(
            new Guid("22222222-2222-2222-2222-000000000005")
        );

        public static IReadOnlyList<BasemapId> All { get; } =
        [
            MapLibreDemotiles,
            OpenFreeMapLiberty,
            OpenFreeMapPositron,
            OpenFreeMapBright,
            VersatilesColorful,
        ];
    }

    public void Configure(EntityTypeBuilder<Basemap> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Name).IsRequired().HasMaxLength(200);
        builder.Property(b => b.Description).HasMaxLength(2000);
        builder.Property(b => b.StyleUrl).IsRequired().HasMaxLength(2048);
        builder.Property(b => b.Attribution).HasMaxLength(500);
        builder.Property(b => b.ThumbnailUrl).HasMaxLength(2048);
        builder.Property(b => b.ConcurrencyStamp).IsConcurrencyToken();

        builder.HasData(GenerateSeedBasemaps());
    }

    /// <summary>
    /// Seed catalog of free, publicly-hosted basemap styles. The first one
    /// (MapLibre demotiles) is the global fallback used by every newly-created map.
    /// </summary>
    private static Basemap[] GenerateSeedBasemaps()
    {
        var seededAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        return
        [
            new Basemap
            {
                Id = SeedIds.MapLibreDemotiles,
                Name = "MapLibre Demotiles",
                Description = "Official MapLibre demo vector style. Free for development.",
                StyleUrl = "https://demotiles.maplibre.org/style.json",
                Attribution = "MapLibre",
                CreatedAt = seededAt,
                UpdatedAt = seededAt,
                ConcurrencyStamp = "seed-basemap-demotiles",
            },
            new Basemap
            {
                Id = SeedIds.OpenFreeMapLiberty,
                Name = "OpenFreeMap Liberty",
                Description = "OpenFreeMap free vector basemap, Liberty style.",
                StyleUrl = "https://tiles.openfreemap.org/styles/liberty",
                Attribution = "© OpenStreetMap contributors, OpenFreeMap",
                CreatedAt = seededAt,
                UpdatedAt = seededAt,
                ConcurrencyStamp = "seed-basemap-openfreemap-liberty",
            },
            new Basemap
            {
                Id = SeedIds.OpenFreeMapPositron,
                Name = "OpenFreeMap Positron",
                Description = "OpenFreeMap free vector basemap, light Positron style.",
                StyleUrl = "https://tiles.openfreemap.org/styles/positron",
                Attribution = "© OpenStreetMap contributors, OpenFreeMap",
                CreatedAt = seededAt,
                UpdatedAt = seededAt,
                ConcurrencyStamp = "seed-basemap-openfreemap-positron",
            },
            new Basemap
            {
                Id = SeedIds.OpenFreeMapBright,
                Name = "OpenFreeMap Bright",
                Description = "OpenFreeMap free vector basemap, Bright style.",
                StyleUrl = "https://tiles.openfreemap.org/styles/bright",
                Attribution = "© OpenStreetMap contributors, OpenFreeMap",
                CreatedAt = seededAt,
                UpdatedAt = seededAt,
                ConcurrencyStamp = "seed-basemap-openfreemap-bright",
            },
            new Basemap
            {
                Id = SeedIds.VersatilesColorful,
                Name = "Versatiles Colorful",
                Description = "VersaTiles free OSM-based vector basemap, Colorful style.",
                StyleUrl = "https://tiles.versatiles.org/assets/styles/colorful/style.json",
                Attribution = "© OpenStreetMap contributors, VersaTiles",
                CreatedAt = seededAt,
                UpdatedAt = seededAt,
                ConcurrencyStamp = "seed-basemap-versatiles-colorful",
            },
        ];
    }
}
