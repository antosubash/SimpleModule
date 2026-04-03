using SimpleModule.Core.Validation;
using SimpleModule.RateLimiting.Contracts;

namespace SimpleModule.RateLimiting.Endpoints.Policies;

public static class CreateRequestValidator
{
    public static ValidationResult Validate(CreateRateLimitRuleRequest request) =>
        new ValidationBuilder()
            .AddErrorIf(
                string.IsNullOrWhiteSpace(request.PolicyName),
                "PolicyName",
                "Policy name is required."
            )
            .AddErrorIf(
                request.PermitLimit <= 0,
                "PermitLimit",
                "Permit limit must be greater than zero."
            )
            .Build();
}
