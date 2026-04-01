namespace SimpleModule.Agents.Dtos;

public sealed record AgentChatRequest(
    string Message,
    string? SessionId = null,
    string? ResponseType = null
);
