using FluentAssertions;
using SimpleModule.Orders.Contracts;
using SimpleModule.Orders.Endpoints.Orders;

namespace Orders.Tests.Unit;

public sealed class CreateRequestValidatorTests
{
    private readonly CreateRequestValidator _validator = new();

    [Fact]
    public void Validate_WithValidRequest_ReturnsSuccess()
    {
        var request = new CreateOrderRequest
        {
            UserId = "1",
            Items = [new OrderItem { ProductId = 1, Quantity = 2 }],
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithDefaultUserId_ReturnsError()
    {
        var request = new CreateOrderRequest
        {
            Items = [new OrderItem { ProductId = 1, Quantity = 1 }],
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }

    [Fact]
    public void Validate_WithEmptyItems_ReturnsError()
    {
        var request = new CreateOrderRequest { UserId = "1", Items = [] };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Items");
    }

    [Fact]
    public void Validate_WithZeroQuantity_ReturnsError()
    {
        var request = new CreateOrderRequest
        {
            UserId = "1",
            Items = [new OrderItem { ProductId = 1, Quantity = 0 }],
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Items");
    }

    [Fact]
    public void Validate_WithMultipleErrors_ReturnsAllErrors()
    {
        var request = new CreateOrderRequest { Items = [] };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
        result.Errors.Should().Contain(e => e.PropertyName == "Items");
    }
}
