using FluentAssertions;
using SimpleModule.Core.RateLimiting;

namespace SimpleModule.Core.Tests.RateLimiting;

public sealed class RateLimitDefaultsTests
{
    [Fact]
    public void EnsureFrameworkDefaults_RegistersAuthStrict_WhenRegistryEmpty()
    {
        // Simulates a host WITHOUT the SimpleModule.RateLimiting module: the
        // generator hands AddSimpleModuleRateLimiting an empty registry. The
        // auth-strict policy that framework auth endpoints attach must still be
        // registered, otherwise login 500s with "no such policy exists" (#222).
        var registry = new RateLimitPolicyRegistry();

        RateLimitDefaults.EnsureFrameworkDefaults(registry);

        var policy = registry.GetPolicy(RateLimitPolicies.AuthStrict);
        policy.Should().NotBeNull();
        policy!.PolicyType.Should().Be(RateLimitPolicyType.FixedWindow);
        policy.Target.Should().Be(RateLimitTarget.Ip);
        policy.PermitLimit.Should().Be(10);
        policy.Window.Should().Be(TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void EnsureFrameworkDefaults_DoesNotOverride_ModuleSuppliedPolicy()
    {
        // When SimpleModule.RateLimiting is installed it defines auth-strict before
        // AddSimpleModuleRateLimiting runs; the module's definition must win.
        var registry = new RateLimitPolicyRegistry();
        ((IRateLimitBuilder)registry).Add(
            new RateLimitPolicyDefinition
            {
                Name = RateLimitPolicies.AuthStrict,
                PolicyType = RateLimitPolicyType.SlidingWindow,
                PermitLimit = 999,
            }
        );

        RateLimitDefaults.EnsureFrameworkDefaults(registry);

        var policy = registry.GetPolicy(RateLimitPolicies.AuthStrict);
        policy!.PermitLimit.Should().Be(999);
        policy.PolicyType.Should().Be(RateLimitPolicyType.SlidingWindow);
    }
}
