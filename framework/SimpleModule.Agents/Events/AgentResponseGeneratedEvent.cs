using SimpleModule.Core.Events;

namespace SimpleModule.Agents.Events;

public sealed class AgentResponseGeneratedEvent : IEvent
{
    public required string AgentName { get; init; }
    public required string Response { get; init; }
    public required string SessionId { get; init; }
    public TimeSpan Duration { get; init; }
    public int? EstimatedTokens { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
