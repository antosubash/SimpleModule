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

        // Configure TickerQ entities with basic key mappings.
        // When TickerQ's model customizer is active (production), it applies its own
        // full configuration. In test mode (no TickerQ), these ensure tables are created.
        modelBuilder.Entity<TimeTickerEntity>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Id).ValueGeneratedNever();
        });
        modelBuilder.Entity<CronTickerEntity>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).ValueGeneratedNever();
        });
        modelBuilder.Entity<CronTickerOccurrenceEntity<CronTickerEntity>>(e =>
        {
            e.HasKey(o => o.Id);
            e.Property(o => o.Id).ValueGeneratedNever();
        });

        modelBuilder.ApplyModuleSchema(BackgroundJobsConstants.ModuleName, dbOptions.Value);
    }
}
