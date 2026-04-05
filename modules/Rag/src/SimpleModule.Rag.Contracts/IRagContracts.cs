using SimpleModule.Core.Rag;
using SimpleModule.Rag.StructuredRag.Data;

namespace SimpleModule.Rag.Contracts;

public interface IRagContracts
{
    Task<CachedStructuredKnowledge?> GetCachedKnowledgeAsync(
        string collectionName,
        string documentHash,
        StructureType structureType,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<CachedStructuredKnowledge>> GetCollectionEntriesAsync(
        string collectionName,
        StructureType structureType,
        CancellationToken cancellationToken = default
    );

    Task<bool> IsCachedAsync(
        string collectionName,
        string documentHash,
        StructureType structureType,
        CancellationToken cancellationToken = default
    );
}
