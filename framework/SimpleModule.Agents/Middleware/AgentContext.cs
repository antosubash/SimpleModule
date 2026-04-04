using System.Security.Claims;
using SimpleModule.Agents.Dtos;

namespace SimpleModule.Agents.Middleware;

public sealed class AgentContext
{
    public required string AgentName { get; init; }
    public required AgentChatRequest Request { get; init; }
    public ClaimsPrincipal? User { get; init; }
    public string? SessionId { get; set; }
    public AgentChatResponse? Response { get; set; }
    public CancellationToken CancellationToken { get; init; }
    public Dictionary<string, object> Properties { get; } = [];
    public DateTimeOffset StartedAt { get; } = DateTimeOffset.UtcNow;
}
