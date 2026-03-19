using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder.EntityConfigurations;

public class PageTemplateConfiguration : IEntityTypeConfiguration<PageTemplate>
{
    public void Configure(EntityTypeBuilder<PageTemplate> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedOnAdd();
        builder.Property(t => t.Name).IsRequired().HasMaxLength(200);
        builder.HasIndex(t => t.Name).IsUnique();
        builder.Property(t => t.Content).IsRequired();
    }
}
