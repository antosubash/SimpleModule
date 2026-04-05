using SimpleModule.Core;

namespace SimpleModule.Agents.Contracts;

[Dto]
public class AgentMessageDto
{
    public string Id { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public int? TokenCount { get; set; }
}
