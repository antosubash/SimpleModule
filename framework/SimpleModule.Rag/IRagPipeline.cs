namespace SimpleModule.Rag;

public interface IRagPipeline
{
    Task<RagResult> QueryAsync(
        string query,
        RagQueryOptions? options = null,
        CancellationToken cancellationToken = default
    );
}
