using SimpleModule.Core.Rag;

namespace SimpleModule.Rag.StructuredRag.Data;

public interface IStructuredKnowledgeCache
{
    Task<CachedStructuredKnowledge?> GetAsync(
        string collectionName,
        string documentHash,
        StructureType structureType,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<CachedStructuredKnowledge>> GetByCollectionAsync(
        string collectionName,
        StructureType structureType,
        CancellationToken cancellationToken = default
    );

    Task SaveAsync(CachedStructuredKnowledge entry, CancellationToken cancellationToken = default);

    Task SaveBatchAsync(
        IReadOnlyList<CachedStructuredKnowledge> entries,
        CancellationToken cancellationToken = default
    );

    Task CleanExpiredAsync(CancellationToken cancellationToken = default);
}
