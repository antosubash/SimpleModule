using FluentAssertions;
using SimpleModule.Core.RateLimiting;

namespace SimpleModule.RateLimiting.Tests;

public class RateLimitingModuleTests
{
    [Fact]
    public void ConfigureRateLimits_ShouldRegisterBuiltInPolicies()
    {
        var module = new RateLimitingModule();
        var registry = new RateLimitPolicyRegistry();

        module.ConfigureRateLimits(registry);

        var policies = registry.GetPolicies();
        policies.Should().HaveCount(4);
        policies.Should().Contain(p => p.Name == "fixed-default");
        policies.Should().Contain(p => p.Name == "sliding-strict");
        policies.Should().Contain(p => p.Name == "token-bucket");
        policies.Should().Contain(p => p.Name == "auth-strict");
    }

    [Fact]
    public void ConfigureRateLimits_FixedDefault_ShouldHaveCorrectSettings()
    {
        var module = new RateLimitingModule();
        var registry = new RateLimitPolicyRegistry();

        module.ConfigureRateLimits(registry);

        var policy = registry.GetPolicy("fixed-default");
        policy.Should().NotBeNull();
        policy!.PolicyType.Should().Be(RateLimitPolicyType.FixedWindow);
        policy.Target.Should().Be(RateLimitTarget.Ip);
        policy.PermitLimit.Should().Be(60);
        policy.Window.Should().Be(TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void ConfigureRateLimits_AuthStrict_ShouldHaveLowLimit()
    {
        var module = new RateLimitingModule();
        var registry = new RateLimitPolicyRegistry();

        module.ConfigureRateLimits(registry);

        var policy = registry.GetPolicy("auth-strict");
        policy.Should().NotBeNull();
        policy!.PermitLimit.Should().Be(10);
    }

    [Fact]
    public void ConfigureRateLimits_TokenBucket_ShouldHaveCorrectSettings()
    {
        var module = new RateLimitingModule();
        var registry = new RateLimitPolicyRegistry();

        module.ConfigureRateLimits(registry);

        var policy = registry.GetPolicy("token-bucket");
        policy.Should().NotBeNull();
        policy!.PolicyType.Should().Be(RateLimitPolicyType.TokenBucket);
        policy.TokenLimit.Should().Be(100);
        policy.TokensPerPeriod.Should().Be(10);
        policy.ReplenishmentPeriod.Should().Be(TimeSpan.FromSeconds(10));
    }
}
