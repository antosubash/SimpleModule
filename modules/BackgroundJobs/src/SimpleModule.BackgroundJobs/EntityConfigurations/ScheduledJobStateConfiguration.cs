using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleModule.BackgroundJobs.Contracts;

namespace SimpleModule.BackgroundJobs.EntityConfigurations;

public class ScheduledJobStateConfiguration : IEntityTypeConfiguration<ScheduledJobState>
{
    public void Configure(EntityTypeBuilder<ScheduledJobState> builder)
    {
        builder.ToTable("ScheduledJobStates");
        builder.HasKey(s => s.Name);
        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.JobTypeName).HasMaxLength(500).IsRequired();
        builder.Property(s => s.CronExpression).HasMaxLength(100).IsRequired();
        builder.Property(s => s.TimeZoneId).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Payload);
        builder.Property(s => s.IsEnabled).IsRequired();
        builder.Property(s => s.WithoutOverlapping).IsRequired();
        builder.Property(s => s.OnOneServer).IsRequired();
        builder.Property(s => s.LastRunAt);
        builder.Property(s => s.NextRunAt);
        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.UpdatedAt).IsRequired();
        builder.Property(s => s.ConcurrencyStamp).HasMaxLength(64);

        builder
            .HasIndex(s => new { s.IsEnabled, s.NextRunAt })
            .HasDatabaseName("IX_ScheduledJobStates_Enabled_NextRunAt");
    }
}
