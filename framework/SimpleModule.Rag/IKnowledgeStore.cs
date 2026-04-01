using SimpleModule.Core.Rag;

namespace SimpleModule.Rag;

public interface IKnowledgeStore
{
    Task IndexDocumentsAsync(
        string collectionName,
        IReadOnlyList<KnowledgeDocument> documents,
        CancellationToken cancellationToken = default
    );
    Task<IReadOnlyList<KnowledgeSearchResult>> SearchAsync(
        string collectionName,
        string query,
        int topK = 5,
        float minScore = 0.0f,
        CancellationToken cancellationToken = default
    );
    Task DeleteCollectionAsync(
        string collectionName,
        CancellationToken cancellationToken = default
    );
}
