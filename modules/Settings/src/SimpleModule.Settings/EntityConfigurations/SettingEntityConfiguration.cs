using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.Settings.EntityConfigurations;

public class SettingEntityConfiguration : IEntityTypeConfiguration<SettingEntity>
{
    public void Configure(EntityTypeBuilder<SettingEntity> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedOnAdd();
        builder.Property(s => s.Key).IsRequired().HasMaxLength(256);
        builder.Property(s => s.Value).IsRequired(false);
        builder.Property(s => s.Scope).HasConversion<int>();
        builder.Property(s => s.UserId).IsRequired(false).HasMaxLength(450);
        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.UpdatedAt).IsRequired();
        builder.Property(s => s.ConcurrencyStamp).HasMaxLength(64);

        builder
            .HasIndex(s => new
            {
                s.Key,
                s.Scope,
                s.UserId,
            })
            .IsUnique();
    }
}
