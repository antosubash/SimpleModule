namespace SimpleModule.Core.Rag;

/// <summary>
/// A document to be indexed in the RAG knowledge store.
/// </summary>
/// <param name="Title">Document title for display and citation.</param>
/// <param name="Content">The full text content to embed and search.</param>
/// <param name="Metadata">Optional key-value metadata for filtering.</param>
public sealed record KnowledgeDocument(
    string Title,
    string Content,
    Dictionary<string, string>? Metadata = null
);
