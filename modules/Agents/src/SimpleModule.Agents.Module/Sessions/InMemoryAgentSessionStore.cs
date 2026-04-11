using System.Collections.Concurrent;
using SimpleModule.Agents.Contracts;

namespace SimpleModule.Agents.Module;

public sealed class InMemoryAgentSessionStore : IAgentSessionStore
{
    private readonly ConcurrentDictionary<AgentSessionId, AgentSession> _sessions = new();
    private readonly ConcurrentDictionary<AgentSessionId, List<AgentMessage>> _messages = new();

    public Task<AgentSession?> GetSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default
    )
    {
        _sessions.TryGetValue(AgentSessionId.From(sessionId), out var session);
        return Task.FromResult(session);
    }

    public Task<AgentSession> CreateSessionAsync(
        string agentName,
        string? userId,
        CancellationToken cancellationToken = default
    )
    {
        var session = new AgentSession { AgentName = agentName, UserId = userId };
        _sessions[session.Id] = session;
        _messages.GetOrAdd(session.Id, _ => []);
        return Task.FromResult(session);
    }

    public Task SaveMessageAsync(
        string sessionId,
        AgentMessage message,
        CancellationToken cancellationToken = default
    )
    {
        var id = AgentSessionId.From(sessionId);
        message.SessionId = id;
        var messages = _messages.GetOrAdd(id, _ => []);
        lock (messages)
        {
            messages.Add(message);
            if (_sessions.TryGetValue(id, out var session))
            {
                session.LastMessageAt = DateTimeOffset.UtcNow;
            }
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AgentMessage>> GetHistoryAsync(
        string sessionId,
        int? maxMessages = null,
        CancellationToken cancellationToken = default
    )
    {
        if (!_messages.TryGetValue(AgentSessionId.From(sessionId), out var messages))
            return Task.FromResult<IReadOnlyList<AgentMessage>>([]);

        lock (messages)
        {
            IReadOnlyList<AgentMessage> result = maxMessages.HasValue
                ? messages.TakeLast(maxMessages.Value).ToList()
                : messages.ToList();
            return Task.FromResult(result);
        }
    }
}
