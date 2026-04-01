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

    [VectorStoreVector(1536)]
    public ReadOnlyMemory<float> Embedding { get; set; }
}
