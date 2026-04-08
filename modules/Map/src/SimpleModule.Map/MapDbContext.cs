using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimpleModule.Database;
using SimpleModule.Map.Contracts;
using SimpleModule.Map.EntityConfigurations;

namespace SimpleModule.Map;

public class MapDbContext(
    DbContextOptions<MapDbContext> options,
    IOptions<DatabaseOptions> dbOptions
) : DbContext(options)
{
    public DbSet<LayerSource> LayerSources => Set<LayerSource>();
    public DbSet<SavedMap> SavedMaps => Set<SavedMap>();
    public DbSet<Basemap> Basemaps => Set<Basemap>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new LayerSourceConfiguration());
        modelBuilder.ApplyConfiguration(new SavedMapConfiguration());
        modelBuilder.ApplyConfiguration(new BasemapConfiguration());
        modelBuilder.ApplyModuleSchema("Map", dbOptions.Value);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<LayerSourceId>()
            .HaveConversion<
                LayerSourceId.EfCoreValueConverter,
                LayerSourceId.EfCoreValueComparer
            >();
        configurationBuilder
            .Properties<SavedMapId>()
            .HaveConversion<SavedMapId.EfCoreValueConverter, SavedMapId.EfCoreValueComparer>();
        configurationBuilder
            .Properties<BasemapId>()
            .HaveConversion<BasemapId.EfCoreValueConverter, BasemapId.EfCoreValueComparer>();
    }
}
