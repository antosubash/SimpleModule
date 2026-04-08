using SimpleModule.Core.Validation;
using SimpleModule.Map.Contracts;

namespace SimpleModule.Map.Endpoints.Maps;

public static class CreateMapRequestValidator
{
    public static ValidationResult Validate(CreateMapRequest request, int maxLayers) =>
        new ValidationBuilder()
            .AddErrorIf(string.IsNullOrWhiteSpace(request.Name), "Name", "Name is required.")
            .AddErrorIf(
                string.IsNullOrWhiteSpace(request.BaseStyleUrl),
                "BaseStyleUrl",
                "Base style URL is required."
            )
            .AddErrorIf(
                request.CenterLat is < -90 or > 90,
                "CenterLat",
                "Latitude must be between -90 and 90."
            )
            .AddErrorIf(
                request.CenterLng is < -180 or > 180,
                "CenterLng",
                "Longitude must be between -180 and 180."
            )
            .AddErrorIf(request.Zoom is < 0 or > 24, "Zoom", "Zoom must be between 0 and 24.")
            .AddErrorIf(
                request.Layers?.Count > maxLayers,
                "Layers",
                $"A map may not contain more than {maxLayers} layers."
            )
            .Build();
}
