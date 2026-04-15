using FluentAssertions;
using SimpleModule.Products.Contracts;
using SimpleModule.Products.Endpoints.Products;

namespace Products.Tests.Unit;

public class CreateRequestValidatorTests
{
    private readonly CreateRequestValidator _validator = new();

    [Fact]
    public void Validate_WithValidRequest_ReturnsSuccess()
    {
        var request = new CreateProductRequest { Name = "Widget", Price = 9.99m };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyName_ReturnsError()
    {
        var request = new CreateProductRequest { Name = "", Price = 9.99m };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_WithZeroPrice_ReturnsError()
    {
        var request = new CreateProductRequest { Name = "Widget", Price = 0 };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Price");
    }
}
