namespace SimpleModule.Chat.Contracts;

public interface IChatContracts
{
    Task<IReadOnlyList<Conversation>> GetUserConversationsAsync(
        string userId,
        CancellationToken cancellationToken = default
    );

    Task<Conversation> StartConversationAsync(
        string userId,
        string agentName,
        string? title,
        CancellationToken cancellationToken = default
    );
}
