using SimpleModule.Agents.Contracts;

namespace SimpleModule.Agents.Module;

public sealed class AgentMessage
{
    public AgentMessageId Id { get; set; } = AgentMessageId.From(Guid.NewGuid().ToString());
    public AgentSessionId SessionId { get; set; } = AgentSessionId.From(string.Empty);
    public string Role { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public int? TokenCount { get; set; }
}
