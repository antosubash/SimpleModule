namespace SimpleModule.Core.RateLimiting;

public interface IRateLimitBuilder
{
    IRateLimitBuilder Add(RateLimitPolicyDefinition policy);
}
