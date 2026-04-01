using Microsoft.Extensions.AI;
using SimpleModule.Core.Rag;

namespace SimpleModule.Rag.StructuredRag;

public sealed class LlmKnowledgeStructurizer(IChatClient chatClient) : IKnowledgeStructurizer
{
    public async Task<StructuredKnowledge> StructurizeAsync(
        StructureType type,
        string query,
        IReadOnlyList<string> documents,
        CancellationToken cancellationToken = default
    )
    {
        if (type == StructureType.Chunk)
        {
            return new StructuredKnowledge(type, string.Join("\n\n---\n\n", documents), query);
        }

        var systemPrompt = StructurePrompts.GetStructurizerPrompt(type);
        var documentsText = string.Join("\n\n---\n\n", documents);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, $"Query: {query}\n\nDocuments:\n{documentsText}"),
        };

        var response = await chatClient.GetResponseAsync(
            messages,
            cancellationToken: cancellationToken
        );

        return new StructuredKnowledge(type, response.Text ?? "", query);
    }
}
