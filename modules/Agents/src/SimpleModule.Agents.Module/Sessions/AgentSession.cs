using SimpleModule.Core.Entities;

namespace SimpleModule.Agents.Module;

public sealed class AgentSession : Entity<string>
{
    public AgentSession()
    {
        Id = Guid.NewGuid().ToString();
    }

    public string AgentName { get; set; } = "";
    public string? UserId { get; set; }
    public DateTimeOffset LastMessageAt { get; set; } = DateTimeOffset.UtcNow;
}
