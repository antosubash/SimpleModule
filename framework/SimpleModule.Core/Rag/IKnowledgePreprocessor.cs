namespace SimpleModule.Core.Rag;

/// <summary>
/// Preprocesses documents after indexing. Implementations may convert documents
/// into structured formats, build indexes, or perform other post-indexing work.
/// </summary>
public interface IKnowledgePreprocessor
{
    Task PreprocessAsync(
        string collectionName,
        IReadOnlyList<KnowledgeDocument> documents,
        CancellationToken cancellationToken = default
    );
}
