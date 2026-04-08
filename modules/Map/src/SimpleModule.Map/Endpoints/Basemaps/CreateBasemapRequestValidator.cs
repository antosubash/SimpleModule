using SimpleModule.Core.Validation;
using SimpleModule.Map.Contracts;

namespace SimpleModule.Map.Endpoints.Basemaps;

public static class CreateBasemapRequestValidator
{
    public static ValidationResult Validate(CreateBasemapRequest request) =>
        new ValidationBuilder()
            .AddErrorIf(string.IsNullOrWhiteSpace(request.Name), "Name", "Name is required.")
            .AddErrorIf(
                string.IsNullOrWhiteSpace(request.StyleUrl),
                "StyleUrl",
                "StyleUrl is required."
            )
            .AddErrorIf(
                request.StyleUrl?.Length > 2048,
                "StyleUrl",
                "StyleUrl must be 2048 characters or fewer."
            )
            .Build();
}
