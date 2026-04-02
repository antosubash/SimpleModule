using SimpleModule.Core.Events;

namespace SimpleModule.Agents.Events;

public sealed class AgentMessageReceivedEvent : IEvent
{
    public required string AgentName { get; init; }
    public required string Message { get; init; }
    public required string SessionId { get; init; }
    public string? UserId { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
