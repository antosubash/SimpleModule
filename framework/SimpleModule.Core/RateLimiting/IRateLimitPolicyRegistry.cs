namespace SimpleModule.Core.RateLimiting;

public interface IRateLimitPolicyRegistry
{
    IReadOnlyList<RateLimitPolicyDefinition> GetPolicies();
    RateLimitPolicyDefinition? GetPolicy(string name);
}
