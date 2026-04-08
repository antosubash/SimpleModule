using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleModule.Datasets.Entities;

namespace SimpleModule.Datasets.EntityConfigurations;

public sealed class DatasetConfiguration : IEntityTypeConfiguration<Dataset>
{
    public void Configure(EntityTypeBuilder<Dataset> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();
        builder.Property(d => d.Name).IsRequired().HasMaxLength(256);
        builder.Property(d => d.OriginalFileName).IsRequired().HasMaxLength(512);
        builder.Property(d => d.ContentHash).HasMaxLength(128);
        builder.Property(d => d.StoragePath).IsRequired().HasMaxLength(1024);
        builder.Property(d => d.NormalizedPath).HasMaxLength(1024);
        builder.Property(d => d.ErrorMessage).HasMaxLength(4096);
        builder.Property(d => d.ConcurrencyStamp).IsConcurrencyToken().HasMaxLength(64);
        builder.HasIndex(d => d.Status);
        builder.HasIndex(d => d.Format);
        builder.HasIndex(d => d.ContentHash);
        builder.HasIndex(d => new
        {
            d.BboxMinX,
            d.BboxMaxX,
            d.BboxMinY,
            d.BboxMaxY,
        });
        builder.HasIndex(d => new { d.IsDeleted, d.CreatedAt });
        builder.HasQueryFilter(d => !d.IsDeleted);
    }
}
