using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimpleModule.BackgroundJobs.Entities;
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
}
