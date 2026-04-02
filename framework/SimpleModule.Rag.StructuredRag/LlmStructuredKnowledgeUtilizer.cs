using Microsoft.Extensions.AI;

namespace SimpleModule.Rag.StructuredRag;

public sealed class LlmStructuredKnowledgeUtilizer(IChatClient chatClient)
    : IStructuredKnowledgeUtilizer
{
    public async Task<string> AnswerAsync(
        string query,
        StructuredKnowledge knowledge,
        CancellationToken cancellationToken = default
    )
    {
        var structureLabel = knowledge.Type.ToString().ToUpperInvariant();

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, StructurePrompts.UtilizerSystem),
            new(
                ChatRole.User,
                $"## Structured Knowledge ({structureLabel} format):\n\n{knowledge.Content}\n\n## Question:\n{query}"
            ),
        };

        var response = await chatClient.GetResponseAsync(
            messages,
            cancellationToken: cancellationToken
        );

        return response.Text ?? "";
    }
}
