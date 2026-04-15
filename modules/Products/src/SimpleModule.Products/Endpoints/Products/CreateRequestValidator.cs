using FluentValidation;
using SimpleModule.Products.Contracts;

namespace SimpleModule.Products.Endpoints.Products;

public sealed class CreateRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Product name is required.");
        RuleFor(x => x.Price).GreaterThan(0).WithMessage("Price must be greater than zero.");
    }
}
