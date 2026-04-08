namespace SimpleModule.Chat.Contracts;

public class Conversation
{
    public ConversationId Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = "New conversation";
    public string AgentName { get; set; } = string.Empty;
    public bool Pinned { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public List<ChatMessage> Messages { get; set; } = [];
}
