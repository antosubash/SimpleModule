using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleModule.Map.Contracts;

namespace SimpleModule.Map.EntityConfigurations;

public class SavedMapConfiguration : IEntityTypeConfiguration<SavedMap>
{
    /// <summary>
    /// Toggles mapping of the spatial <see cref="SavedMap.Center"/> column.
    /// Defaults to <c>false</c>; flipped on by <c>MapModule.ConfigureServices</c>
    /// when the host opts in via <c>Modules:Map:EnableSpatial = true</c>.
    /// </summary>
    public static bool EnableSpatial { get; set; }

    public void Configure(EntityTypeBuilder<SavedMap> builder)
    {
        var enableSpatial = EnableSpatial;
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Name).IsRequired().HasMaxLength(200);
        builder.Property(m => m.Description).HasMaxLength(2000);
        builder.Property(m => m.BaseStyleUrl).IsRequired().HasMaxLength(2048);
        builder.Property(m => m.ConcurrencyStamp).IsConcurrencyToken();

        if (enableSpatial)
        {
            // Spatial point of the map's center, SRID 4326 (WGS84). Backed by:
            //   - PostgreSQL: geometry(Point, 4326) via PostGIS
            //   - SQL Server: geometry
            //   - SQLite:     SpatiaLite POINT
            builder.Property(m => m.Center).HasColumnType("geometry");
        }
        else
        {
            builder.Ignore(m => m.Center);
        }

        builder.OwnsMany(
            m => m.Basemaps,
            bm =>
            {
                bm.WithOwner().HasForeignKey("SavedMapId");
                bm.Property<int>("Id").ValueGeneratedOnAdd();
                bm.HasKey("Id");
                bm.Property(b => b.BasemapId).IsRequired();
                bm.Property(b => b.Order);
            }
        );

        builder.OwnsMany(
            m => m.Layers,
            layer =>
            {
                layer.WithOwner().HasForeignKey("SavedMapId");
                layer.Property<int>("Id").ValueGeneratedOnAdd();
                layer.HasKey("Id");
                layer.Property(l => l.LayerSourceId).IsRequired();
                layer.Property(l => l.Order);
                layer.Property(l => l.Visible);
                layer.Property(l => l.Opacity);

                layer.Property(l => l.StyleOverrides).HasJsonDictionaryConversion();
            }
        );
    }
}
