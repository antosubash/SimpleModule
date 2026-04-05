namespace SimpleModule.Agents.Contracts;

public interface IAgentsContracts
{
    Task<AgentSessionDto?> GetSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default
    );

    Task<AgentSessionDto> CreateSessionAsync(
        string agentName,
        string? userId,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<AgentMessageDto>> GetSessionHistoryAsync(
        string sessionId,
        int? maxMessages = null,
        CancellationToken cancellationToken = default
    );
}
