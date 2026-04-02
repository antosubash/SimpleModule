using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using SimpleModule.Core.Rag;

namespace SimpleModule.Rag.StructuredRag;

public sealed class LlmStructureRouter(
    IChatClient chatClient,
    IOptions<StructuredRagOptions> options
) : IStructureRouter
{
    public async Task<StructureType> SelectStructureAsync(
        string query,
        IReadOnlyList<string> documentSummaries,
        CancellationToken cancellationToken = default
    )
    {
        if (!options.Value.EnableRouter)
            return options.Value.DefaultStructure;

        var summariesText = string.Join("\n---\n", documentSummaries.Take(5));

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, StructurePrompts.RouterSystem),
            new(ChatRole.User, $"Query: {query}\n\nDocument summaries:\n{summariesText}"),
        };

        var response = await chatClient.GetResponseAsync(
            messages,
            cancellationToken: cancellationToken
        );
        var text = response.Text?.Trim() ?? "";

        return text.ToUpperInvariant() switch
        {
            "TABLE" => StructureType.Table,
            "GRAPH" => StructureType.Graph,
            "ALGORITHM" => StructureType.Algorithm,
            "CATALOGUE" or "CATALOG" => StructureType.Catalogue,
            "CHUNK" => StructureType.Chunk,
            _ => options.Value.DefaultStructure,
        };
    }
}
