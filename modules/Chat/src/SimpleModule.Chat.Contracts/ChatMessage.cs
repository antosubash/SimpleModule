using SimpleModule.Core.Entities;

namespace SimpleModule.Chat.Contracts;

public class ChatMessage : Entity<ChatMessageId>
{
    public ConversationId ConversationId { get; set; }
    public ChatRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
}

public enum ChatRole
{
    User = 0,
    Assistant = 1,
    System = 2,
}

public static class ChatRoleExtensions
{
    public static string ToWire(ChatRole role) =>
        role switch
        {
            ChatRole.User => "user",
            ChatRole.Assistant => "assistant",
            ChatRole.System => "system",
            _ => "user",
        };
}
