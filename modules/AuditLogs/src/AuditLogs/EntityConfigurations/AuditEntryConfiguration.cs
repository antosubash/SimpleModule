using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleModule.AuditLogs.Contracts;

namespace SimpleModule.AuditLogs.EntityConfigurations;

public class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        builder.Property(e => e.Timestamp).IsRequired();
        builder.Property(e => e.Source).IsRequired();
        builder.Property(e => e.CorrelationId).IsRequired();
        builder.Property(e => e.HttpMethod).HasMaxLength(10);
        builder.Property(e => e.Path).HasMaxLength(2048);
        builder.Property(e => e.IpAddress).HasMaxLength(45);
        builder.Property(e => e.UserId).HasMaxLength(256);
        builder.Property(e => e.UserName).HasMaxLength(256);
        builder.Property(e => e.Module).HasMaxLength(128);
        builder.Property(e => e.EntityType).HasMaxLength(256);
        builder.Property(e => e.EntityId).HasMaxLength(256);
        // Primary sort index — used by all time-range queries
        builder.HasIndex(e => new { e.Timestamp }).IsDescending(true);

        // User activity queries (filter by user + sort by time)
        builder.HasIndex(e => new { e.UserId, e.Timestamp }).IsDescending(false, true);

        // Module activity queries (filter by module + sort by time)
        builder.HasIndex(e => new { e.Module, e.Timestamp }).IsDescending(false, true);

        // Correlation tracing
        builder.HasIndex(e => e.CorrelationId);

        // Entity change history
        builder.HasIndex(e => new { e.EntityType, e.EntityId });

        // Dashboard aggregation indexes — supports GROUP BY queries
        builder.HasIndex(e => e.Source);
        builder.HasIndex(e => e.Action);
        builder.HasIndex(e => e.StatusCode);
        builder.HasIndex(e => e.Path);
    }
}
