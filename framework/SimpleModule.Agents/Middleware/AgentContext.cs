using System.Security.Claims;
using SimpleModule.Agents.Dtos;
using SimpleModule.Agents.Sessions;

namespace SimpleModule.Agents.Middleware;

public sealed class AgentContext
{
    public required string AgentName { get; init; }
    public required AgentChatRequest Request { get; init; }
    public ClaimsPrincipal? User { get; init; }
    public AgentSession? Session { get; set; }
    public AgentChatResponse? Response { get; set; }
    public CancellationToken CancellationToken { get; init; }
    public Dictionary<string, object> Properties { get; } = [];
    public DateTimeOffset StartedAt { get; } = DateTimeOffset.UtcNow;
}
