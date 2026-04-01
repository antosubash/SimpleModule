using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimpleModule.BackgroundJobs.Entities;
using SimpleModule.BackgroundJobs.EntityConfigurations;
using SimpleModule.Database;
using TickerQ.Utilities.Entities;

namespace SimpleModule.BackgroundJobs;

public class BackgroundJobsDbContext(
    DbContextOptions<BackgroundJobsDbContext> options,
    IOptions<DatabaseOptions> dbOptions
) : DbContext(options)
{
    public DbSet<JobProgress> JobProgress => Set<JobProgress>();
    public DbSet<TimeTickerEntity> TimeTickers => Set<TimeTickerEntity>();
    public DbSet<CronTickerEntity> CronTickers => Set<CronTickerEntity>();
    public DbSet<CronTickerOccurrenceEntity<CronTickerEntity>> CronTickerOccurrences =>
        Set<CronTickerOccurrenceEntity<CronTickerEntity>>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new JobProgressConfiguration());
        modelBuilder.ApplyModuleSchema(BackgroundJobsConstants.ModuleName, dbOptions.Value);
    }
}
