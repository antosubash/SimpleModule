using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleModule.BackgroundJobs.Entities;

namespace SimpleModule.BackgroundJobs.EntityConfigurations;

public class JobQueueEntryConfiguration : IEntityTypeConfiguration<JobQueueEntryEntity>
{
    public void Configure(EntityTypeBuilder<JobQueueEntryEntity> builder)
    {
        builder.ToTable("JobQueueEntries");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.JobTypeName).HasMaxLength(500).IsRequired();
        builder.Property(e => e.SerializedData);
        builder.Property(e => e.ScheduledAt).IsRequired();
        builder.Property(e => e.State).HasConversion<int>().IsRequired();
        builder.Property(e => e.ClaimedBy).HasMaxLength(100);
        builder.Property(e => e.ClaimedAt);
        builder.Property(e => e.AttemptCount).IsRequired();
        builder.Property(e => e.Error);
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();
        builder.Property(e => e.ConcurrencyStamp).HasMaxLength(64);
        builder.Property(e => e.CompletedAt);
        builder.Property(e => e.CronExpression).HasMaxLength(100);
        builder.Property(e => e.RecurringName).HasMaxLength(200);

        builder
            .HasIndex(e => new { e.State, e.ScheduledAt })
            .HasDatabaseName("IX_JobQueueEntries_State_ScheduledAt");
        builder.HasIndex(e => e.RecurringName).HasDatabaseName("IX_JobQueueEntries_RecurringName");
    }
}
