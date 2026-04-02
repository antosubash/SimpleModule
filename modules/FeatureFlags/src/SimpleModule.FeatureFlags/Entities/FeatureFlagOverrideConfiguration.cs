using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SimpleModule.FeatureFlags.Entities;

public class FeatureFlagOverrideConfiguration : IEntityTypeConfiguration<FeatureFlagOverrideEntity>
{
    public void Configure(EntityTypeBuilder<FeatureFlagOverrideEntity> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedOnAdd();
        builder.Property(o => o.FlagName).IsRequired().HasMaxLength(256);
        builder.Property(o => o.OverrideValue).IsRequired().HasMaxLength(256);
        builder
            .HasIndex(o => new
            {
                o.FlagName,
                o.OverrideType,
                o.OverrideValue,
            })
            .IsUnique();
        builder.Property(o => o.ConcurrencyStamp).HasMaxLength(40).IsConcurrencyToken();
    }
}
