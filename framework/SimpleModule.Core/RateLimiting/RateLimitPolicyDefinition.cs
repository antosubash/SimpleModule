namespace SimpleModule.Core.RateLimiting;

public sealed class RateLimitPolicyDefinition
{
    public required string Name { get; init; }
    public RateLimitPolicyType PolicyType { get; init; } = RateLimitPolicyType.FixedWindow;
    public RateLimitTarget Target { get; init; } = RateLimitTarget.Ip;
    public int PermitLimit { get; init; } = 60;
    public TimeSpan Window { get; init; } = TimeSpan.FromMinutes(1);
    public int SegmentsPerWindow { get; init; } = 4;
    public int TokenLimit { get; init; } = 100;
    public int TokensPerPeriod { get; init; } = 10;
    public TimeSpan ReplenishmentPeriod { get; init; } = TimeSpan.FromSeconds(10);
    public int QueueLimit { get; init; }
}
