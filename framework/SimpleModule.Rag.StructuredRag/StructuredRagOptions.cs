using SimpleModule.Core.Rag;

namespace SimpleModule.Rag.StructuredRag;

public sealed class StructuredRagOptions
{
    public string? RouterModel { get; set; }
    public StructureType DefaultStructure { get; set; } = StructureType.Chunk;
    public bool EnableRouter { get; set; } = true;
    public int StructurizerMaxTokens { get; set; } = 4096;

    /// <summary>When true, documents are preprocessed into all structure types at ingestion time.</summary>
    public bool EnablePreprocessing { get; set; }

    /// <summary>TTL for preprocessed cache entries. Null means no expiration.</summary>
    public TimeSpan? PreprocessedCacheTtl { get; set; }
}
