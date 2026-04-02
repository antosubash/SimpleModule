using SimpleModule.Core.Events;

namespace SimpleModule.Agents.Events;

public sealed class AgentToolCalledEvent : IEvent
{
    public required string AgentName { get; init; }
    public required string ToolName { get; init; }
    public required string SessionId { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
