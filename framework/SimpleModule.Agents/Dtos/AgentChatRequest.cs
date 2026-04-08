namespace SimpleModule.Agents.Dtos;

public sealed record AgentChatRequest(
    string Message,
    string? SessionId = null,
    string? ResponseType = null,
    IReadOnlyList<AgentHistoryMessage>? History = null
);

/// <summary>
/// A prior turn in the conversation. Role must be "user" or "assistant".
/// System messages are provided by the agent definition and should not appear here.
/// </summary>
public sealed record AgentHistoryMessage(string Role, string Content);
