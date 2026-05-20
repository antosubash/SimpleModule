using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleModule.BackgroundJobs.Contracts;

namespace SimpleModule.BackgroundJobs.EntityConfigurations;

public class JobMutexConfiguration : IEntityTypeConfiguration<JobMutex>
{
    public void Configure(EntityTypeBuilder<JobMutex> builder)
    {
        builder.ToTable("JobMutexes");
        builder.HasKey(m => m.Name);
        builder.Property(m => m.Name).HasMaxLength(200).IsRequired();
        builder.Property(m => m.OwnerWorkerId).HasMaxLength(100).IsRequired();
        builder.Property(m => m.AcquiredAt).IsRequired();
        builder.Property(m => m.ExpiresAt).IsRequired();
        builder.Property(m => m.ConcurrencyStamp).HasMaxLength(64).IsConcurrencyToken();
    }
}
