using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SimpleModule.RateLimiting.Contracts;

namespace SimpleModule.RateLimiting;

public partial class RateLimitingService(
    RateLimitingDbContext db,
    ILogger<RateLimitingService> logger
) : IRateLimitingContracts
{
    public async Task<IEnumerable<RateLimitRule>> GetAllRulesAsync() =>
        await db.Rules.AsNoTracking().OrderBy(r => r.PolicyName).ToListAsync();

    public async Task<RateLimitRule?> GetRuleByIdAsync(RateLimitRuleId id)
    {
        var rule = await db.Rules.FindAsync(id);
        if (rule is null)
        {
            LogRuleNotFound(logger, id);
        }

        return rule;
    }

    public async Task<RateLimitRule> CreateRuleAsync(CreateRateLimitRuleRequest request)
    {
        var rule = new RateLimitRule
        {
            PolicyName = request.PolicyName,
            PolicyType = request.PolicyType,
            Target = request.Target,
            PermitLimit = request.PermitLimit,
            WindowSeconds = request.WindowSeconds,
            SegmentsPerWindow = request.SegmentsPerWindow,
            TokenLimit = request.TokenLimit,
            TokensPerPeriod = request.TokensPerPeriod,
            ReplenishmentPeriodSeconds = request.ReplenishmentPeriodSeconds,
            QueueLimit = request.QueueLimit,
            EndpointPattern = request.EndpointPattern,
            IsEnabled = request.IsEnabled,
            CreatedAt = DateTime.UtcNow,
        };

        db.Rules.Add(rule);
        await db.SaveChangesAsync();

        LogRuleCreated(logger, rule.Id, rule.PolicyName);

        return rule;
    }

    public async Task<RateLimitRule> UpdateRuleAsync(
        RateLimitRuleId id,
        UpdateRateLimitRuleRequest request
    )
    {
        var rule = await db.Rules.FindAsync(id);
        if (rule is null)
        {
            throw new Core.Exceptions.NotFoundException("RateLimitRule", id);
        }

        rule.PolicyType = request.PolicyType;
        rule.Target = request.Target;
        rule.PermitLimit = request.PermitLimit;
        rule.WindowSeconds = request.WindowSeconds;
        rule.SegmentsPerWindow = request.SegmentsPerWindow;
        rule.TokenLimit = request.TokenLimit;
        rule.TokensPerPeriod = request.TokensPerPeriod;
        rule.ReplenishmentPeriodSeconds = request.ReplenishmentPeriodSeconds;
        rule.QueueLimit = request.QueueLimit;
        rule.EndpointPattern = request.EndpointPattern;
        rule.IsEnabled = request.IsEnabled;
        rule.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        LogRuleUpdated(logger, rule.Id, rule.PolicyName);

        return rule;
    }

    public async Task DeleteRuleAsync(RateLimitRuleId id)
    {
        var rule = await db.Rules.FindAsync(id);
        if (rule is null)
        {
            throw new Core.Exceptions.NotFoundException("RateLimitRule", id);
        }

        db.Rules.Remove(rule);
        await db.SaveChangesAsync();

        LogRuleDeleted(logger, id);
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Rate limit rule with ID {RuleId} not found"
    )]
    private static partial void LogRuleNotFound(ILogger logger, RateLimitRuleId ruleId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Rate limit rule {RuleId} created: {PolicyName}"
    )]
    private static partial void LogRuleCreated(
        ILogger logger,
        RateLimitRuleId ruleId,
        string policyName
    );

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Rate limit rule {RuleId} updated: {PolicyName}"
    )]
    private static partial void LogRuleUpdated(
        ILogger logger,
        RateLimitRuleId ruleId,
        string policyName
    );

    [LoggerMessage(Level = LogLevel.Information, Message = "Rate limit rule {RuleId} deleted")]
    private static partial void LogRuleDeleted(ILogger logger, RateLimitRuleId ruleId);
}
