using FluentAssertions;
using SimpleModule.Products.Contracts;
using SimpleModule.Products.Endpoints.Products;

namespace Products.Tests.Unit;

public class CreateRequestValidatorTests
{
    [Fact]
    public void Validate_WithValidRequest_ReturnsSuccess()
    {
        var request = new CreateProductRequest { Name = "Widget", Price = 9.99m };

        var result = CreateRequestValidator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyName_ReturnsError()
    {
        var request = new CreateProductRequest { Name = "", Price = 9.99m };

        var result = CreateRequestValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainKey("Name");
    }

    [Fact]
    public void Validate_WithZeroPrice_ReturnsError()
    {
        var request = new CreateProductRequest { Name = "Widget", Price = 0 };

        var result = CreateRequestValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainKey("Price");
    }
}
