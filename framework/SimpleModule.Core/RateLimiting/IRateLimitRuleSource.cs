using Microsoft.AspNetCore.Http;

namespace SimpleModule.Core.RateLimiting;

/// <summary>
/// Snapshot of database-defined rate-limit rules consulted by the global rate
/// limiter on every request. Modules that own the rule storage register the
/// implementation as a singleton and call <see cref="RefreshAsync"/> whenever
/// rules change so admin edits take effect without a restart.
/// </summary>
public interface IRateLimitRuleSource
{
    RateLimitPolicyDefinition? FindForPath(PathString path);

    Task RefreshAsync(CancellationToken cancellationToken = default);
}
