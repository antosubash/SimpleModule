using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Options;
using SimpleModule.Database;
using SimpleModule.Settings.Entities;

namespace SimpleModule.Settings;

public class SettingsDbContext(
    DbContextOptions<SettingsDbContext> options,
    IOptions<DatabaseOptions> dbOptions
) : DbContext(options)
{
    public DbSet<SettingEntity> Settings => Set<SettingEntity>();
    public DbSet<PublicMenuItemEntity> PublicMenuItems => Set<PublicMenuItemEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SettingsDbContext).Assembly);
        modelBuilder.ApplyModuleSchema("Settings", dbOptions.Value);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite cannot ORDER BY or compare DateTimeOffset expressions natively.
        // Store as long (binary ticks) only when running against SQLite.
        if (dbOptions.Value.DetectProvider("Settings") == DatabaseProvider.Sqlite)
        {
            configurationBuilder
                .Properties<DateTimeOffset>()
                .HaveConversion<DateTimeOffsetToBinaryConverter>();
            configurationBuilder
                .Properties<DateTimeOffset?>()
                .HaveConversion<DateTimeOffsetToBinaryConverter>();
        }
    }
}
