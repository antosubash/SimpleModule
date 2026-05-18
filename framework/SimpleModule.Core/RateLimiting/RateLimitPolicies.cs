namespace SimpleModule.Core.RateLimiting;

/// <summary>
/// Canonical names of the default rate-limit policies registered by the
/// RateLimiting module. Use these constants wherever a policy is referenced
/// (e.g. <c>.RateLimit(RateLimitPolicies.AuthStrict)</c>) so a rename can
/// never silently miss a call site.
/// </summary>
public static class RateLimitPolicies
{
    public const string FixedDefault = "fixed-default";
    public const string SlidingStrict = "sliding-strict";
    public const string TokenBucket = "token-bucket";
    public const string AuthStrict = "auth-strict";
}
