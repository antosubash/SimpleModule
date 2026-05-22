using FluentValidation;
using SimpleModule.RateLimiting.Contracts;

namespace SimpleModule.RateLimiting.Endpoints.Policies;

public sealed class CreateRequestValidator : AbstractValidator<CreateRateLimitRuleRequest>
{
    public CreateRequestValidator()
    {
        RuleFor(x => x.PolicyName).NotEmpty().WithMessage("Policy name is required.");
        RuleFor(x => x.PermitLimit)
            .GreaterThan(0)
            .WithMessage("Permit limit must be greater than zero.");
        RuleFor(x => x.WindowSeconds).GreaterThan(0);
        RuleFor(x => x.SegmentsPerWindow).GreaterThan(0);
        RuleFor(x => x.ReplenishmentPeriodSeconds).GreaterThan(0);
        RuleFor(x => x.TokenLimit).GreaterThan(0);
        RuleFor(x => x.TokensPerPeriod).GreaterThan(0);
        RuleFor(x => x.QueueLimit).GreaterThanOrEqualTo(0);
    }
}
