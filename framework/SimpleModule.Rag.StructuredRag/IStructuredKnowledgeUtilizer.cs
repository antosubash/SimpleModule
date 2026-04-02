namespace SimpleModule.Rag.StructuredRag;

public interface IStructuredKnowledgeUtilizer
{
    Task<string> AnswerAsync(
        string query,
        StructuredKnowledge knowledge,
        CancellationToken cancellationToken = default
    );
}
