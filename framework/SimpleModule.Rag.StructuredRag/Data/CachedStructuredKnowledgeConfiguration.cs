using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SimpleModule.Rag.StructuredRag.Data;

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

        // Composite index for cache lookups: find structured content for a doc+type
        builder
            .HasIndex(e => new
            {
                e.CollectionName,
                e.DocumentHash,
                e.StructureType,
            })
            .IsUnique();

        // Index for expiration cleanup
        builder.HasIndex(e => e.ExpiresAt);
    }
}
