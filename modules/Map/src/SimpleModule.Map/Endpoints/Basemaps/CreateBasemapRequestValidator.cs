using FluentValidation;
using SimpleModule.Map.Contracts;

namespace SimpleModule.Map.Endpoints.Basemaps;

public sealed class CreateBasemapRequestValidator : AbstractValidator<CreateBasemapRequest>
{
    public CreateBasemapRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
        RuleFor(x => x.StyleUrl)
            .NotEmpty()
            .WithMessage("StyleUrl is required.")
            .MaximumLength(2048)
            .WithMessage("StyleUrl must be 2048 characters or fewer.");
    }
}
