using Microsoft.Extensions.VectorData;

namespace SimpleModule.Rag;

public sealed class KnowledgeRecord
{
    [VectorStoreKey]
    public string Id { get; set; } = "";

    [VectorStoreData]
    public string Title { get; set; } = "";

    [VectorStoreData]
    public string Content { get; set; } = "";

    [VectorStoreData]
    public string CollectionName { get; set; } = "";

    [VectorStoreData(IsIndexed = true)]
    public string? ModuleName { get; set; }

    // Dimension is configured at runtime via VectorStoreCollectionDefinition built from
    // RagOptions.EmbeddingDimension. The attribute default below is only used when callers
    // bypass VectorKnowledgeStore and use attribute-based discovery directly.
    [VectorStoreVector(VectorKnowledgeStore.DefaultEmbeddingDimension)]
    public ReadOnlyMemory<float> Embedding { get; set; }
}
