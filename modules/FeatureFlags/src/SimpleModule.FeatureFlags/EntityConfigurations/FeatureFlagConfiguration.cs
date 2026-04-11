using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleModule.FeatureFlags.Contracts;

namespace SimpleModule.FeatureFlags.EntityConfigurations;

public class FeatureFlagConfiguration : IEntityTypeConfiguration<FeatureFlagEntity>
{
    public void Configure(EntityTypeBuilder<FeatureFlagEntity> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedOnAdd();
        builder.Property(f => f.Name).IsRequired().HasMaxLength(256);
        builder.HasIndex(f => f.Name).IsUnique();
        builder.Property(f => f.ConcurrencyStamp).HasMaxLength(40).IsConcurrencyToken();
    }
}
