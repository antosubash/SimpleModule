using SimpleModule.Core.Rag;

namespace SimpleModule.Rag;

public sealed record RagResult(
    string Answer,
    IReadOnlyList<RagSource> Sources,
    RagMetadata Metadata
);

public sealed record RagSource(
    string Title,
    string Content,
    float Score,
    Dictionary<string, string>? Metadata = null
);

public sealed record RagMetadata(
    string PipelineName,
    StructureType? SelectedStructure,
    TimeSpan Duration
);
