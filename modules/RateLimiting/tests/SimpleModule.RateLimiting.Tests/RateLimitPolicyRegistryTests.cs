using FluentAssertions;
using SimpleModule.Core.RateLimiting;

namespace SimpleModule.RateLimiting.Tests;

public class RateLimitPolicyRegistryTests
{
    [Fact]
    public void Add_ShouldRegisterPolicy()
    {
        var registry = new RateLimitPolicyRegistry();

        registry.Add(
            new RateLimitPolicyDefinition
            {
                Name = "test-policy",
                PolicyType = RateLimitPolicyType.FixedWindow,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
            }
        );

        registry.GetPolicies().Should().HaveCount(1);
        registry.GetPolicies()[0].Name.Should().Be("test-policy");
    }

    [Fact]
    public void GetPolicy_ShouldReturnPolicy_WhenExists()
    {
        var registry = new RateLimitPolicyRegistry();
        registry.Add(
            new RateLimitPolicyDefinition
            {
                Name = "my-policy",
                PolicyType = RateLimitPolicyType.SlidingWindow,
                PermitLimit = 50,
            }
        );

        var policy = registry.GetPolicy("my-policy");

        policy.Should().NotBeNull();
        policy!.PolicyType.Should().Be(RateLimitPolicyType.SlidingWindow);
        policy.PermitLimit.Should().Be(50);
    }

    [Fact]
    public void GetPolicy_ShouldReturnNull_WhenNotFound()
    {
        var registry = new RateLimitPolicyRegistry();

        var policy = registry.GetPolicy("nonexistent");

        policy.Should().BeNull();
    }

    [Fact]
    public void GetPolicy_ShouldBeCaseInsensitive()
    {
        var registry = new RateLimitPolicyRegistry();
        registry.Add(new RateLimitPolicyDefinition { Name = "Test-Policy" });

        var policy = registry.GetPolicy("test-policy");

        policy.Should().NotBeNull();
    }

    [Fact]
    public void Add_ShouldSupportChainingMultiplePolicies()
    {
        var registry = new RateLimitPolicyRegistry();

        registry
            .Add(
                new RateLimitPolicyDefinition
                {
                    Name = "fixed",
                    PolicyType = RateLimitPolicyType.FixedWindow,
                }
            )
            .Add(
                new RateLimitPolicyDefinition
                {
                    Name = "sliding",
                    PolicyType = RateLimitPolicyType.SlidingWindow,
                }
            )
            .Add(
                new RateLimitPolicyDefinition
                {
                    Name = "token",
                    PolicyType = RateLimitPolicyType.TokenBucket,
                }
            );

        registry.GetPolicies().Should().HaveCount(3);
    }

    [Fact]
    public void DefaultValues_ShouldBeReasonable()
    {
        var definition = new RateLimitPolicyDefinition { Name = "defaults" };

        definition.PolicyType.Should().Be(RateLimitPolicyType.FixedWindow);
        definition.Target.Should().Be(RateLimitTarget.Ip);
        definition.PermitLimit.Should().Be(60);
        definition.Window.Should().Be(TimeSpan.FromMinutes(1));
        definition.SegmentsPerWindow.Should().Be(4);
        definition.TokenLimit.Should().Be(100);
        definition.TokensPerPeriod.Should().Be(10);
        definition.ReplenishmentPeriod.Should().Be(TimeSpan.FromSeconds(10));
        definition.QueueLimit.Should().Be(0);
    }
}
