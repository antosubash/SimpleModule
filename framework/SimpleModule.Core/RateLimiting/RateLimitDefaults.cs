namespace SimpleModule.Core.RateLimiting;

/// <summary>
/// Permissive baseline definitions for the named rate-limit policies that
/// framework endpoints attach (currently <see cref="RateLimitPolicies.AuthStrict"/>
/// on the auth pages). These guarantee the policy names are always registered,
/// even when the optional <c>SimpleModule.RateLimiting</c> module isn't installed,
/// so the endpoints don't fail at request time with "no such policy exists" (#222).
/// When <c>SimpleModule.RateLimiting</c> is present, its own definitions take
/// precedence — see <see cref="EnsureFrameworkDefaults"/>.
/// </summary>
public static class RateLimitDefaults
{
    /// <summary>
    /// The framework-default policies, keyed by name. Values mirror the
    /// <c>SimpleModule.RateLimiting</c> module's own <c>auth-strict</c> baseline so
    /// behaviour is identical whether or not that module is installed.
    /// </summary>
    public static IReadOnlyList<RateLimitPolicyDefinition> FrameworkPolicies { get; } =
    [
        new RateLimitPolicyDefinition
        {
            Name = RateLimitPolicies.AuthStrict,
            PolicyType = RateLimitPolicyType.FixedWindow,
            Target = RateLimitTarget.Ip,
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
        },
    ];

    /// <summary>
    /// Adds each framework default to <paramref name="registry"/> only when a policy
    /// of the same name is not already present, so module-supplied definitions always
    /// win. No-op if the registry can't be written to.
    /// </summary>
    public static void EnsureFrameworkDefaults(IRateLimitPolicyRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        if (registry is not IRateLimitBuilder builder)
        {
            return;
        }

        foreach (var policy in FrameworkPolicies)
        {
            if (registry.GetPolicy(policy.Name) is null)
            {
                builder.Add(policy);
            }
        }
    }
}
