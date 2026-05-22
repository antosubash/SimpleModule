using FluentValidation;
using SimpleModule.RateLimiting.Contracts;

namespace SimpleModule.RateLimiting.Endpoints.Policies;

public sealed class UpdateRequestValidator : AbstractValidator<UpdateRateLimitRuleRequest>
{
    public UpdateRequestValidator()
    {
        RuleFor(x => x.PermitLimit).GreaterThan(0);
        RuleFor(x => x.WindowSeconds).GreaterThan(0);
        RuleFor(x => x.SegmentsPerWindow).GreaterThan(0);
        RuleFor(x => x.ReplenishmentPeriodSeconds).GreaterThan(0);
        RuleFor(x => x.TokenLimit).GreaterThan(0);
        RuleFor(x => x.TokensPerPeriod).GreaterThan(0);
        RuleFor(x => x.QueueLimit).GreaterThanOrEqualTo(0);
    }
}
