using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Options;
using SimpleModule.Database;
using SimpleModule.Datasets.Contracts;
using SimpleModule.Datasets.Entities;
using SimpleModule.Datasets.EntityConfigurations;

namespace SimpleModule.Datasets;

public class DatasetsDbContext(
    DbContextOptions<DatasetsDbContext> options,
    IOptions<DatabaseOptions> dbOptions
) : DbContext(options)
{
    public DbSet<Dataset> Datasets => Set<Dataset>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new DatasetConfiguration());
        modelBuilder.ApplyModuleSchema(DatasetsConstants.ModuleName, dbOptions.Value);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<DatasetId>()
            .HaveConversion<DatasetId.EfCoreValueConverter, DatasetId.EfCoreValueComparer>();

        // SQLite cannot ORDER BY DateTimeOffset natively. Store as ISO-8601 TEXT so
        // lexicographic ordering matches chronological ordering. Other providers
        // keep their native DateTimeOffset mapping.
        var provider = DatabaseProviderDetector.Detect(
            dbOptions.Value.DefaultConnection,
            dbOptions.Value.Provider
        );
        if (provider == DatabaseProvider.Sqlite)
        {
            configurationBuilder
                .Properties<DateTimeOffset>()
                .HaveConversion<DateTimeOffsetToStringConverter>();

            configurationBuilder
                .Properties<DateTimeOffset?>()
                .HaveConversion<DateTimeOffsetToStringConverter>();
        }
    }
}
