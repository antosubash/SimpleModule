namespace SimpleModule.Core.RateLimiting;

public sealed class RateLimitPolicyRegistry : IRateLimitBuilder, IRateLimitPolicyRegistry
{
    private readonly Dictionary<string, RateLimitPolicyDefinition> _policies = new(
        StringComparer.OrdinalIgnoreCase
    );

    public IRateLimitBuilder Add(RateLimitPolicyDefinition policy)
    {
        _policies[policy.Name] = policy;
        return this;
    }

    public IReadOnlyList<RateLimitPolicyDefinition> GetPolicies() =>
        _policies.Values.ToList().AsReadOnly();

    public RateLimitPolicyDefinition? GetPolicy(string name) => _policies.GetValueOrDefault(name);
}
