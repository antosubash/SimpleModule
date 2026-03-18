using FluentAssertions;
using SimpleModule.Products.Contracts;
using SimpleModule.Products.Endpoints.Products;

namespace Products.Tests.Unit;

public class UpdateRequestValidatorTests
{
    [Fact]
    public void Validate_WithValidRequest_ReturnsSuccess()
    {
        var request = new UpdateProductRequest { Name = "Widget", Price = 9.99m };

        var result = UpdateRequestValidator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyName_ReturnsError()
    {
        var request = new UpdateProductRequest { Name = "", Price = 9.99m };

        var result = UpdateRequestValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainKey("Name");
    }

    [Fact]
    public void Validate_WithWhitespaceOnlyName_ReturnsError()
    {
        var request = new UpdateProductRequest { Name = "   ", Price = 9.99m };

        var result = UpdateRequestValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainKey("Name");
    }

    [Fact]
    public void Validate_WithZeroPrice_ReturnsError()
    {
        var request = new UpdateProductRequest { Name = "Widget", Price = 0m };

        var result = UpdateRequestValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainKey("Price");
    }

    [Fact]
    public void Validate_WithNegativePrice_ReturnsError()
    {
        var request = new UpdateProductRequest { Name = "Widget", Price = -5.00m };

        var result = UpdateRequestValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainKey("Price");
    }
}
