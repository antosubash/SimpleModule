using SimpleModule.Core.Validation;
using SimpleModule.Map.Contracts;

namespace SimpleModule.Map.Endpoints.LayerSources;

public static class CreateLayerSourceRequestValidator
{
    public static ValidationResult Validate(CreateLayerSourceRequest request) =>
        new ValidationBuilder()
            .AddErrorIf(string.IsNullOrWhiteSpace(request.Name), "Name", "Name is required.")
            .AddErrorIf(string.IsNullOrWhiteSpace(request.Url), "Url", "Url is required.")
            .AddErrorIf(request.Url?.Length > 2048, "Url", "Url must be 2048 characters or fewer.")
            .AddErrorIf(
                !Enum.IsDefined(request.Type),
                "Type",
                "Type must be a known LayerSourceType."
            )
            .Build();
}
