using Microsoft.EntityFrameworkCore;
using SimpleModule.Agents.Contracts;

namespace SimpleModule.Agents.Module;

public sealed class EfAgentSessionStore(AgentsDbContext db) : IAgentSessionStore
{
    public async Task<AgentSession?> GetSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default
    )
    {
        var id = AgentSessionId.From(sessionId);
        return await db.Sessions.FindAsync([id], cancellationToken);
    }

    public async Task<AgentSession> CreateSessionAsync(
        string agentName,
        string? userId,
        CancellationToken cancellationToken = default
    )
    {
        var session = new AgentSession { AgentName = agentName, UserId = userId };
        db.Sessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);
        return session;
    }

    public async Task SaveMessageAsync(
        string sessionId,
        AgentMessage message,
        CancellationToken cancellationToken = default
    )
    {
        var id = AgentSessionId.From(sessionId);
        message.SessionId = id;
        db.Messages.Add(message);

        var session = await db.Sessions.FindAsync([id], cancellationToken);
        if (session is not null)
        {
            session.LastMessageAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AgentMessage>> GetHistoryAsync(
        string sessionId,
        int? maxMessages = null,
        CancellationToken cancellationToken = default
    )
    {
        var id = AgentSessionId.From(sessionId);
        var query = db.Messages.Where(m => m.SessionId == id);

        if (maxMessages.HasValue)
        {
            return await query
                .OrderByDescending(m => m.Timestamp)
                .Take(maxMessages.Value)
                .OrderBy(m => m.Timestamp)
                .ToListAsync(cancellationToken);
        }

        return await query.OrderBy(m => m.Timestamp).ToListAsync(cancellationToken);
    }
}
