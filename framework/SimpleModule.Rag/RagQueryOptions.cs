using SimpleModule.Core.Rag;

namespace SimpleModule.Rag;

public sealed class RagQueryOptions
{
    public string CollectionName { get; set; } = "default";
    public int? TopK { get; set; }
    public float? MinScore { get; set; }
    public StructureType? ForceStructure { get; set; }
    public bool IncludeStructuredContent { get; set; }
}
