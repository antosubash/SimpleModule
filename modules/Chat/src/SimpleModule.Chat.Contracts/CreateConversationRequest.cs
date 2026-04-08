namespace SimpleModule.Chat.Contracts;

public sealed class CreateConversationRequest
{
    public string AgentName { get; set; } = string.Empty;
    public string? Title { get; set; }
}

public sealed class RenameConversationRequest
{
    public string Title { get; set; } = string.Empty;
}
