using SimpleModule.Core.Validation;
using SimpleModule.Products.Contracts;

namespace SimpleModule.Products.Endpoints.Products;

public static class CreateRequestValidator
{
    public static ValidationResult Validate(CreateProductRequest request) =>
        new ValidationBuilder()
            .AddErrorIf(
                string.IsNullOrWhiteSpace(request.Name),
                "Name",
                "Product name is required."
            )
            .AddErrorIf(
                request.Price <= 0,
                "Price",
                "Price must be greater than zero."
            )
            .Build();
}
