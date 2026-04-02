using SimpleModule.Core.Rag;

namespace SimpleModule.Rag.StructuredRag.Data;

/// <summary>
/// Cached structured knowledge entry. Stored in a relational database
/// via the StructuredRagCache module. Framework code uses
/// <see cref="IStructuredKnowledgeCache"/> to access this data.
/// </summary>
public sealed class CachedStructuredKnowledge
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CollectionName { get; set; } = "";
    public string DocumentHash { get; set; } = "";
    public StructureType StructureType { get; set; }
    public string StructuredContent { get; set; } = "";
    public string SourceTitle { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAt { get; set; }
    public int HitCount { get; set; }
}
