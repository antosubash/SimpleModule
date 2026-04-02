using SimpleModule.Core.Rag;

namespace SimpleModule.Rag.StructuredRag;

public interface IStructureRouter
{
    Task<StructureType> SelectStructureAsync(
        string query,
        IReadOnlyList<string> documentSummaries,
        CancellationToken cancellationToken = default
    );
}
