using FluentValidation;
using SimpleModule.Map.Contracts;

namespace SimpleModule.Map.Endpoints.LayerSources;

public sealed class UpdateLayerSourceRequestValidator : AbstractValidator<UpdateLayerSourceRequest>
{
    public UpdateLayerSourceRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
        RuleFor(x => x.Url)
            .NotEmpty()
            .WithMessage("Url is required.")
            .MaximumLength(2048)
            .WithMessage("Url must be 2048 characters or fewer.");
        RuleFor(x => x.Type)
            .Must(t => Enum.IsDefined(t))
            .WithMessage("Type must be a known LayerSourceType.");
    }
}
