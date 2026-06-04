using System.Data.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SimpleModule.Core.RateLimiting;
using SimpleModule.RateLimiting.Contracts;

namespace SimpleModule.RateLimiting;

/// <summary>
/// Loads enabled <see cref="RateLimitRule"/>s from the database into an
/// immutable snapshot consulted by the global rate limiter. Rebuilt on
/// startup and after every admin write via <see cref="RefreshAsync"/>.
/// </summary>
internal sealed partial class RateLimitRuleCache(
    IServiceScopeFactory scopeFactory,
    ILogger<RateLimitRuleCache> logger
) : IRateLimitRuleSource, IHostedService
{
    // Volatile so non-x86 readers see the swapped reference promptly.
    private volatile CompiledRule[] _rules = [];

    public Task StartAsync(CancellationToken cancellationToken) => RefreshAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RateLimitingDbContext>();

        List<RateLimitRule> rules;
        try
        {
            rules = await db
                .Rules.AsNoTracking()
                .Where(r => r.IsEnabled)
                .ToListAsync(cancellationToken);
        }
        catch (DbException ex)
        {
            // The RateLimiting_Rules table may not exist yet — e.g. the module was
            // added to a dev DB previously created by EnsureCreated(), which does
            // not add tables to an existing database. Because RefreshAsync runs from
            // IHostedService.StartAsync, throwing here would crash the whole host
            // (#223). Degrade to "no DB-defined rules"; static policies from
            // ConfigureRateLimits still apply, and the cache self-heals on the next
            // refresh once the schema exists. DbException covers SQLite/Postgres/
            // SQL Server "missing relation" without swallowing logic errors.
            LogTableUnavailable(logger, ex);
            return;
        }

        var compiled = rules
            .Select(Compile)
            .OrderByDescending(r => Specificity(r.Prefix, r.Kind))
            .ToArray();

        _rules = compiled;
        LogRefreshed(logger, compiled.Length);
    }

    public RateLimitPolicyDefinition? FindForPath(PathString path)
    {
        var snapshot = _rules;
        if (snapshot.Length == 0)
        {
            return null;
        }

        var pathStr = path.HasValue ? path.Value! : "/";
        foreach (var rule in snapshot)
        {
            if (rule.Matches(pathStr))
            {
                return rule.Policy;
            }
        }

        return null;
    }

    private static CompiledRule Compile(RateLimitRule rule)
    {
        var pattern = string.IsNullOrWhiteSpace(rule.EndpointPattern) ? "*" : rule.EndpointPattern!;

        var policy = new RateLimitPolicyDefinition
        {
            Name = rule.PolicyName,
            PolicyType = rule.PolicyType,
            Target = rule.Target,
            PermitLimit = rule.PermitLimit,
            Window = TimeSpan.FromSeconds(rule.WindowSeconds),
            SegmentsPerWindow = rule.SegmentsPerWindow,
            TokenLimit = rule.TokenLimit,
            TokensPerPeriod = rule.TokensPerPeriod,
            ReplenishmentPeriod = TimeSpan.FromSeconds(rule.ReplenishmentPeriodSeconds),
            QueueLimit = rule.QueueLimit,
        };

        if (pattern == "*")
        {
            return new CompiledRule(MatchKind.CatchAll, "", "", policy);
        }

        var wildcard = pattern.IndexOf('*', StringComparison.Ordinal);
        return wildcard < 0
            ? new CompiledRule(MatchKind.Exact, pattern, "", policy)
            : new CompiledRule(
                MatchKind.Wildcard,
                pattern[..wildcard],
                pattern[(wildcard + 1)..],
                policy
            );
    }

    /// <summary>
    /// Higher score = more specific. Catch-all scores zero so it always loses
    /// to any concrete pattern; longer prefixes outrank shorter ones.
    /// </summary>
    private static int Specificity(string prefix, MatchKind kind) =>
        kind == MatchKind.CatchAll ? 0 : prefix.Length;

    private enum MatchKind
    {
        CatchAll,
        Exact,
        Wildcard,
    }

    private readonly record struct CompiledRule(
        MatchKind Kind,
        string Prefix,
        string Suffix,
        RateLimitPolicyDefinition Policy
    )
    {
        public bool Matches(string path) =>
            Kind switch
            {
                MatchKind.CatchAll => true,
                MatchKind.Exact => string.Equals(path, Prefix, StringComparison.OrdinalIgnoreCase),
                MatchKind.Wildcard => path.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)
                    && (
                        Suffix.Length == 0
                        || (
                            path.Length >= Prefix.Length + Suffix.Length
                            && path.EndsWith(Suffix, StringComparison.OrdinalIgnoreCase)
                        )
                    ),
                _ => false,
            };
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Rate-limit rule cache refreshed: {Count} enabled rules loaded"
    )]
    private static partial void LogRefreshed(ILogger logger, int count);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Rate-limit rules table unavailable; serving static policies only "
            + "until the schema is created. Run migrations or recreate the dev database."
    )]
    private static partial void LogTableUnavailable(ILogger logger, Exception exception);
}
