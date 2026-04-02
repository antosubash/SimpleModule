using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleModule.Rag.StructuredRag.Data;

namespace SimpleModule.StructuredRagCache.EntityConfigurations;

public sealed class CachedStructuredKnowledgeConfiguration
    : IEntityTypeConfiguration<CachedStructuredKnowledge>
{
    public void Configure(EntityTypeBuilder<CachedStructuredKnowledge> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.CollectionName).IsRequired().HasMaxLength(256);
        builder.Property(e => e.DocumentHash).IsRequired().HasMaxLength(64);
        builder.Property(e => e.StructureType).IsRequired();
        builder.Property(e => e.StructuredContent).IsRequired();
        builder.Property(e => e.SourceTitle).IsRequired().HasMaxLength(512);
        builder.Property(e => e.CreatedAt).IsRequired();

        builder
            .HasIndex(e => new
            {
                e.CollectionName,
                e.DocumentHash,
                e.StructureType,
            })
            .IsUnique();

        builder.HasIndex(e => e.ExpiresAt);
    }
}
