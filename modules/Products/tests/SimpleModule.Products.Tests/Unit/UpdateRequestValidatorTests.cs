using FluentAssertions;
using SimpleModule.Products.Contracts;
using SimpleModule.Products.Endpoints.Products;

namespace Products.Tests.Unit;

public class UpdateRequestValidatorTests
{
    private readonly UpdateRequestValidator _validator = new();

    [Fact]
    public void Validate_WithValidRequest_ReturnsSuccess()
    {
        var request = new UpdateProductRequest { Name = "Widget", Price = 9.99m };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyName_ReturnsError()
    {
        var request = new UpdateProductRequest { Name = "", Price = 9.99m };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_WithWhitespaceOnlyName_ReturnsError()
    {
        var request = new UpdateProductRequest { Name = "   ", Price = 9.99m };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_WithZeroPrice_ReturnsError()
    {
        var request = new UpdateProductRequest { Name = "Widget", Price = 0m };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Price");
    }

    [Fact]
    public void Validate_WithNegativePrice_ReturnsError()
    {
        var request = new UpdateProductRequest { Name = "Widget", Price = -5.00m };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Price");
    }
}
