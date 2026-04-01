using SimpleModule.Core.Rag;

namespace SimpleModule.Rag.StructuredRag;

public interface IKnowledgeStructurizer
{
    Task<StructuredKnowledge> StructurizeAsync(
        StructureType type,
        string query,
        IReadOnlyList<string> documents,
        CancellationToken cancellationToken = default
    );
}
