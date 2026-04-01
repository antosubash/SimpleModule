namespace SimpleModule.Core.Rag;

/// <summary>
/// Provides documents for RAG indexing. Discovered by the source generator per module.
/// Modules implement this to feed knowledge into the RAG pipeline.
/// </summary>
public interface IKnowledgeSource
{
    /// <summary>Collection name for grouping documents in the vector store.</summary>
    string CollectionName { get; }

    /// <summary>Returns the documents to index for RAG retrieval.</summary>
    Task<IReadOnlyList<KnowledgeDocument>> GetDocumentsAsync(CancellationToken cancellationToken);
}
