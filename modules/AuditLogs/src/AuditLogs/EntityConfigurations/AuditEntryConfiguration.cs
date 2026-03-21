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
        builder.HasIndex(e => new { e.Timestamp }).IsDescending(true);
        builder.HasIndex(e => new { e.UserId, e.Timestamp }).IsDescending(false, true);
        builder.HasIndex(e => new { e.Module, e.Timestamp }).IsDescending(false, true);
        builder.HasIndex(e => e.CorrelationId);
        builder.HasIndex(e => new { e.EntityType, e.EntityId });
    }
}
