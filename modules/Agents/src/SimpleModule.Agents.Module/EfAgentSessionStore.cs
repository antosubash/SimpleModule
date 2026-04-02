using Microsoft.EntityFrameworkCore;
using SimpleModule.Agents.Sessions;

namespace SimpleModule.Agents.Module;

public sealed class EfAgentSessionStore(AgentsDbContext db) : IAgentSessionStore
{
    public async Task<AgentSession?> GetSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default
    )
    {
        return await db.Sessions.FindAsync([sessionId], cancellationToken);
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
        message.SessionId = sessionId;
        db.Messages.Add(message);

        var session = await db.Sessions.FindAsync([sessionId], cancellationToken);
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
        var query = db.Messages.Where(m => m.SessionId == sessionId);

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
