using FluentValidation;
using SimpleModule.Tenants.Contracts;

namespace SimpleModule.Tenants.Endpoints.Tenants;

public sealed class UpdateRequestValidator : AbstractValidator<UpdateTenantRequest>
{
    public UpdateRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Tenant name is required.");
        RuleFor(x => x.AdminEmail)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.AdminEmail))
            .WithMessage("Invalid email format.");
    }
}
