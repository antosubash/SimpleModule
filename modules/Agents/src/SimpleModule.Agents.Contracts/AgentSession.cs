using SimpleModule.Core.Entities;

namespace SimpleModule.Agents.Contracts;

public sealed class AgentSession : Entity<AgentSessionId>
{
    public AgentSession()
    {
        Id = AgentSessionId.From(Guid.NewGuid().ToString());
    }

    public string AgentName { get; set; } = "";
    public string? UserId { get; set; }
    public DateTimeOffset LastMessageAt { get; set; } = DateTimeOffset.UtcNow;
}
