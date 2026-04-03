using SimpleModule.Core.RateLimiting;

namespace SimpleModule.RateLimiting.Contracts;

public class UpdateRateLimitRuleRequest
{
    public RateLimitPolicyType PolicyType { get; set; } = RateLimitPolicyType.FixedWindow;
    public RateLimitTarget Target { get; set; } = RateLimitTarget.Ip;
    public int PermitLimit { get; set; } = 60;
    public int WindowSeconds { get; set; } = 60;
    public int SegmentsPerWindow { get; set; } = 4;
    public int TokenLimit { get; set; } = 100;
    public int TokensPerPeriod { get; set; } = 10;
    public int ReplenishmentPeriodSeconds { get; set; } = 10;
    public int QueueLimit { get; set; }
    public string? EndpointPattern { get; set; }
    public bool IsEnabled { get; set; } = true;
}
