using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace SimpleModule.Agents.Middleware;

public sealed class RateLimitingMiddleware(IOptions<AgentOptions> options) : IAgentMiddleware
{
    private readonly ConcurrentDictionary<string, RateLimitEntry> _entries = new();

    public async Task InvokeAsync(AgentContext context, AgentMiddlewareDelegate next)
    {
        var userId = context.User?.Identity?.Name ?? "anonymous";
        var key = $"{userId}:{context.AgentName}";
        var limit = options.Value.RateLimit.RequestsPerMinute;

        var entry = _entries.GetOrAdd(key, _ => new RateLimitEntry());
        lock (entry)
        {
            entry.CleanExpired();
            if (entry.Count >= limit)
            {
                throw new InvalidOperationException(
                    $"Rate limit exceeded: {limit} requests per minute for agent '{context.AgentName}'"
                );
            }

            entry.Add();

            // Remove empty entries to prevent unbounded dictionary growth
            if (entry.Count == 0)
            {
                _entries.TryRemove(key, out _);
            }
        }

        await next(context);
    }

    private sealed class RateLimitEntry
    {
        private readonly Queue<DateTimeOffset> _timestamps = new();

        public int Count => _timestamps.Count;

        public void Add() => _timestamps.Enqueue(DateTimeOffset.UtcNow);

        public void CleanExpired()
        {
            var cutoff = DateTimeOffset.UtcNow.AddMinutes(-1);
            while (_timestamps.Count > 0 && _timestamps.Peek() < cutoff)
                _timestamps.Dequeue();
        }
    }
}
