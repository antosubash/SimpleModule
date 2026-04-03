namespace SimpleModule.Core.RateLimiting;

public enum RateLimitPolicyType
{
    FixedWindow,
    SlidingWindow,
    TokenBucket,
}
