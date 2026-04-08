using Microsoft.EntityFrameworkCore;
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
    }
}
