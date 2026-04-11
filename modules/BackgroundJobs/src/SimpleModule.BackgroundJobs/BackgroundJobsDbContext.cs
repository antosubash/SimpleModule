using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Options;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.BackgroundJobs.EntityConfigurations;
using SimpleModule.Database;

namespace SimpleModule.BackgroundJobs;

public class BackgroundJobsDbContext(
    DbContextOptions<BackgroundJobsDbContext> options,
    IOptions<DatabaseOptions> dbOptions
) : DbContext(options)
{
    public DbSet<JobProgress> JobProgress => Set<JobProgress>();
    public DbSet<JobQueueEntryEntity> JobQueueEntries => Set<JobQueueEntryEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new JobProgressConfiguration());
        modelBuilder.ApplyConfiguration(new JobQueueEntryConfiguration());
        modelBuilder.ApplyModuleSchema(BackgroundJobsConstants.ModuleName, dbOptions.Value);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<JobId>()
            .HaveConversion<JobId.EfCoreValueConverter, JobId.EfCoreValueComparer>();

        if (
            dbOptions.Value.DetectProvider(BackgroundJobsConstants.ModuleName)
            == DatabaseProvider.Sqlite
        )
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
