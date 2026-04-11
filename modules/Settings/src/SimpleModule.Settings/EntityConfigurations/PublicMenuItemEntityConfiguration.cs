using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.Settings.EntityConfigurations;

public class PublicMenuItemEntityConfiguration : IEntityTypeConfiguration<PublicMenuItemEntity>
{
    public void Configure(EntityTypeBuilder<PublicMenuItemEntity> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedOnAdd();
        builder.Property(m => m.Label).IsRequired().HasMaxLength(200);
        builder.Property(m => m.Url).HasMaxLength(2048);
        builder.Property(m => m.PageRoute).HasMaxLength(256);
        builder.Property(m => m.Icon).HasMaxLength(4000);
        builder.Property(m => m.CssClass).HasMaxLength(500);
        builder.Property(m => m.ConcurrencyStamp).HasMaxLength(64);

        builder
            .HasOne(m => m.Parent)
            .WithMany(m => m.Children)
            .HasForeignKey(m => m.ParentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => new { m.ParentId, m.SortOrder });
    }
}
