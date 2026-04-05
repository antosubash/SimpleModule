using SimpleModule.Core;

namespace SimpleModule.Agents.Contracts;

[Dto]
public class AgentSessionDto
{
    public string Id { get; set; } = string.Empty;
    public string AgentName { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset LastMessageAt { get; set; }
}
