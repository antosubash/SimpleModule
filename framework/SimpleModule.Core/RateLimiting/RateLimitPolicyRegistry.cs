namespace SimpleModule.Core.RateLimiting;

public sealed class RateLimitPolicyRegistry : IRateLimitBuilder, IRateLimitPolicyRegistry
{
    private readonly List<RateLimitPolicyDefinition> _policies = [];

    public IRateLimitBuilder Add(RateLimitPolicyDefinition policy)
    {
        _policies.Add(policy);
        return this;
    }

    public IReadOnlyList<RateLimitPolicyDefinition> GetPolicies() => _policies.AsReadOnly();

    public RateLimitPolicyDefinition? GetPolicy(string name) =>
        _policies.Find(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
}
