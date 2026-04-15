using FluentValidation;
using Microsoft.Extensions.Options;
using SimpleModule.Map.Contracts;

namespace SimpleModule.Map.Endpoints.DefaultMap;

public sealed class UpdateDefaultMapRequestValidator : AbstractValidator<UpdateDefaultMapRequest>
{
    public UpdateDefaultMapRequestValidator(IOptions<MapModuleOptions> options)
    {
        var maxLayers = options.Value.MaxLayersPerMap;

        RuleFor(x => x.CenterLat)
            .InclusiveBetween(-90, 90)
            .WithMessage("Latitude must be between -90 and 90.");
        RuleFor(x => x.CenterLng)
            .InclusiveBetween(-180, 180)
            .WithMessage("Longitude must be between -180 and 180.");
        RuleFor(x => x.Zoom).InclusiveBetween(0, 24).WithMessage("Zoom must be between 0 and 24.");
        RuleFor(x => x.Layers)
            .Must(layers => layers is null || layers.Count <= maxLayers)
            .WithMessage($"A map may not contain more than {maxLayers} layers.");
    }
}
