using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder.EntityConfigurations;

public class PageConfiguration : IEntityTypeConfiguration<Page>
{
    public void Configure(EntityTypeBuilder<Page> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedOnAdd();
        builder.Property(p => p.Title).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Slug).IsRequired().HasMaxLength(200);
        builder.HasIndex(p => p.Slug).IsUnique();
        builder.Property(p => p.Content).IsRequired();
        builder.Property(p => p.DraftContent).IsRequired(false);
        builder.Property(p => p.MetaDescription).IsRequired(false).HasMaxLength(300);
        builder.Property(p => p.MetaKeywords).IsRequired(false).HasMaxLength(500);
        builder.Property(p => p.OgImage).IsRequired(false).HasMaxLength(500);
        builder.Property(p => p.IsPublished).HasDefaultValue(false);
        builder.Property(p => p.Order).HasDefaultValue(0);
    }
}
