using SimpleModule.Core.Rag;
using SimpleModule.Rag.Contracts;
using SimpleModule.Rag.StructuredRag.Data;

namespace SimpleModule.Rag.Module;

public sealed class RagService(IStructuredKnowledgeCache cache) : IRagContracts
{
    public Task<CachedStructuredKnowledge?> GetCachedKnowledgeAsync(
        string collectionName,
        string documentHash,
        StructureType structureType,
        CancellationToken cancellationToken = default
    )
    {
        return cache.GetAsync(collectionName, documentHash, structureType, cancellationToken);
    }

    public Task<IReadOnlyList<CachedStructuredKnowledge>> GetCollectionEntriesAsync(
        string collectionName,
        StructureType structureType,
        CancellationToken cancellationToken = default
    )
    {
        return cache.GetByCollectionAsync(collectionName, structureType, cancellationToken);
    }

    public async Task<bool> IsCachedAsync(
        string collectionName,
        string documentHash,
        StructureType structureType,
        CancellationToken cancellationToken = default
    )
    {
        var result = await cache
            .GetAsync(collectionName, documentHash, structureType, cancellationToken)
            .ConfigureAwait(false);
        return result is not null;
    }
}
