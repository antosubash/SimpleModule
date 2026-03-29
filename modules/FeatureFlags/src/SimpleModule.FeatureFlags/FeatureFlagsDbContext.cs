using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimpleModule.Database;
using SimpleModule.FeatureFlags.Entities;

namespace SimpleModule.FeatureFlags;

public class FeatureFlagsDbContext(
    DbContextOptions<FeatureFlagsDbContext> options,
    IOptions<DatabaseOptions> dbOptions
) : DbContext(options)
{
    public DbSet<FeatureFlagEntity> FeatureFlags => Set<FeatureFlagEntity>();
    public DbSet<FeatureFlagOverrideEntity> FeatureFlagOverrides =>
        Set<FeatureFlagOverrideEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FeatureFlagsDbContext).Assembly);
        modelBuilder.ApplyModuleSchema("FeatureFlags", dbOptions.Value);
    }
}
