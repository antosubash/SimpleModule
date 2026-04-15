using System.Text.RegularExpressions;
using FluentValidation;
using SimpleModule.Tenants.Contracts;

namespace SimpleModule.Tenants.Endpoints.Tenants;

public sealed partial class CreateRequestValidator : AbstractValidator<CreateTenantRequest>
{
    public CreateRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Tenant name is required.");
        RuleFor(x => x.Slug).NotEmpty().WithMessage("Tenant slug is required.");
        RuleFor(x => x.Slug)
            .Must(s => string.IsNullOrWhiteSpace(s) || SlugPattern().IsMatch(s))
            .WithMessage("Slug must contain only lowercase letters, numbers, and hyphens.");
        RuleFor(x => x.AdminEmail)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.AdminEmail))
            .WithMessage("Invalid email format.");
    }

    [GeneratedRegex(@"^[a-z0-9][a-z0-9-]*[a-z0-9]$|^[a-z0-9]$")]
    private static partial Regex SlugPattern();
}
