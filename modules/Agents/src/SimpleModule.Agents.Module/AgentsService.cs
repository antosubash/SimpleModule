using SimpleModule.Agents.Contracts;

namespace SimpleModule.Agents.Module;

public sealed class AgentsService(IAgentSessionStore sessionStore) : IAgentsContracts
{
    public async Task<AgentSessionDto?> GetSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default
    )
    {
        var session = await sessionStore
            .GetSessionAsync(sessionId, cancellationToken)
            .ConfigureAwait(false);
        return session is null ? null : ToDto(session);
    }

    public async Task<AgentSessionDto> CreateSessionAsync(
        string agentName,
        string? userId,
        CancellationToken cancellationToken = default
    )
    {
        var session = await sessionStore
            .CreateSessionAsync(agentName, userId, cancellationToken)
            .ConfigureAwait(false);
        return ToDto(session);
    }

    public async Task<IReadOnlyList<AgentMessageDto>> GetSessionHistoryAsync(
        string sessionId,
        int? maxMessages = null,
        CancellationToken cancellationToken = default
    )
    {
        var messages = await sessionStore
            .GetHistoryAsync(sessionId, maxMessages, cancellationToken)
            .ConfigureAwait(false);
        return messages.Select(ToDto).ToList();
    }

    private static AgentSessionDto ToDto(AgentSession session) =>
        new()
        {
            Id = session.Id.Value,
            AgentName = session.AgentName,
            UserId = session.UserId,
            CreatedAt = session.CreatedAt,
            LastMessageAt = session.LastMessageAt,
        };

    private static AgentMessageDto ToDto(AgentMessage message) =>
        new()
        {
            Id = message.Id.Value,
            SessionId = message.SessionId.Value,
            Role = message.Role,
            Content = message.Content,
            Timestamp = message.Timestamp,
            TokenCount = message.TokenCount,
        };
}
