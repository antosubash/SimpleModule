namespace SimpleModule.RateLimiting.Contracts;

public interface IRateLimitingContracts
{
    Task<IEnumerable<RateLimitRule>> GetAllRulesAsync(int skip = 0, int take = 30);
    Task<RateLimitRule?> GetRuleByIdAsync(RateLimitRuleId id);
    Task<RateLimitRule> CreateRuleAsync(CreateRateLimitRuleRequest request);
    Task<RateLimitRule> UpdateRuleAsync(RateLimitRuleId id, UpdateRateLimitRuleRequest request);
    Task DeleteRuleAsync(RateLimitRuleId id);
}
