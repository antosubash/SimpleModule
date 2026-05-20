using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleModule.BackgroundJobs.Contracts;

namespace SimpleModule.BackgroundJobs.EntityConfigurations;

public class JobLeaseConfiguration : IEntityTypeConfiguration<JobLease>
{
    public void Configure(EntityTypeBuilder<JobLease> builder)
    {
        builder.ToTable("JobLeases");
        builder.HasKey(l => l.Name);
        builder.Property(l => l.Name).HasMaxLength(200).IsRequired();
        builder.Property(l => l.OwnerWorkerId).HasMaxLength(100).IsRequired();
        builder.Property(l => l.AcquiredAt).IsRequired();
        builder.Property(l => l.ExpiresAt).IsRequired();
        builder.Property(l => l.ConcurrencyStamp).HasMaxLength(64).IsConcurrencyToken();
    }
}
