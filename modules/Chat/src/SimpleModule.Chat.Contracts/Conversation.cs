using SimpleModule.Core.Entities;

namespace SimpleModule.Chat.Contracts;

public class Conversation : Entity<ConversationId>
{
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = "New conversation";
    public string AgentName { get; set; } = string.Empty;
    public bool Pinned { get; set; }

    public List<ChatMessage> Messages { get; set; } = [];
}
