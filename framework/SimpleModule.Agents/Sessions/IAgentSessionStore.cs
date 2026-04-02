namespace SimpleModule.Agents.Sessions;

public interface IAgentSessionStore
{
    Task<AgentSession?> GetSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default
    );
    Task<AgentSession> CreateSessionAsync(
        string agentName,
        string? userId,
        CancellationToken cancellationToken = default
    );
    Task SaveMessageAsync(
        string sessionId,
        AgentMessage message,
        CancellationToken cancellationToken = default
    );
    Task<IReadOnlyList<AgentMessage>> GetHistoryAsync(
        string sessionId,
        int? maxMessages = null,
        CancellationToken cancellationToken = default
    );
}
