using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleModule.BackgroundJobs.Entities;

namespace SimpleModule.BackgroundJobs.EntityConfigurations;

public class JobProgressConfiguration : IEntityTypeConfiguration<JobProgress>
{
    public void Configure(EntityTypeBuilder<JobProgress> builder)
    {
        builder.HasKey(j => j.Id);
        builder.Property(j => j.Id).ValueGeneratedNever();
        builder.Property(j => j.JobTypeName).IsRequired().HasMaxLength(500);
        builder.Property(j => j.ModuleName).IsRequired().HasMaxLength(100);
        builder.Property(j => j.ProgressMessage).HasMaxLength(1000);
        builder.Property(j => j.ConcurrencyStamp).HasMaxLength(64);
        builder.HasIndex(j => j.ModuleName);
    }
}
