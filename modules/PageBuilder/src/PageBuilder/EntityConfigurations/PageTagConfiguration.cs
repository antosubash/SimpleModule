using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder.EntityConfigurations;

public class PageTagConfiguration : IEntityTypeConfiguration<PageTag>
{
    public void Configure(EntityTypeBuilder<PageTag> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedOnAdd();
        builder.Property(t => t.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(t => t.Name).IsUnique();
    }
}
