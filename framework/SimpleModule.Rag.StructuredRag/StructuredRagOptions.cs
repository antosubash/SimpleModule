using SimpleModule.Core.Rag;

namespace SimpleModule.Rag.StructuredRag;

public sealed class StructuredRagOptions
{
    public string? RouterModel { get; set; }
    public StructureType DefaultStructure { get; set; } = StructureType.Chunk;
    public bool EnableRouter { get; set; } = true;
    public int StructurizerMaxTokens { get; set; } = 4096;
}
