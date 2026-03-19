using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SimpleModule.Admin.Entities;

public class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.UserId).IsRequired();
        builder.Property(e => e.PerformedByUserId).IsRequired();
        builder.Property(e => e.Action).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Details).HasMaxLength(4000);
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.Timestamp);
    }
}
