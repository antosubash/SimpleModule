namespace SimpleModule.Rag;

public sealed record KnowledgeSearchResult(
    string Title,
    string Content,
    float Score,
    Dictionary<string, string>? Metadata = null
);
