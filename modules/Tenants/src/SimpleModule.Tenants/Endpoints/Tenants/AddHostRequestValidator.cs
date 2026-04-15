using FluentValidation;
using SimpleModule.Tenants.Contracts;

namespace SimpleModule.Tenants.Endpoints.Tenants;

public sealed class AddHostRequestValidator : AbstractValidator<AddTenantHostRequest>
{
    public AddHostRequestValidator()
    {
        RuleFor(x => x.HostName)
            .NotEmpty()
            .WithMessage("Host name is required.")
            .MaximumLength(512)
            .WithMessage("Host name must not exceed 512 characters.");
    }
}
