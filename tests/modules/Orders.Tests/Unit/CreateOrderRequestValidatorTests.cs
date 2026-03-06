using FluentAssertions;
using SimpleModule.Orders.Contracts;
using SimpleModule.Orders.Features.CreateOrder;

namespace Orders.Tests.Unit;

public sealed class CreateOrderRequestValidatorTests
{
    [Fact]
    public void Validate_WithValidRequest_ReturnsSuccess()
    {
        var request = new CreateOrderRequest
        {
            UserId = 1,
            Items = [new OrderItem { ProductId = 1, Quantity = 2 }],
        };

        var result = CreateOrderRequestValidator.Validate(request);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithZeroUserId_ReturnsError()
    {
        var request = new CreateOrderRequest
        {
            UserId = 0,
            Items = [new OrderItem { ProductId = 1, Quantity = 1 }],
        };

        var result = CreateOrderRequestValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainKey("UserId");
    }

    [Fact]
    public void Validate_WithNegativeUserId_ReturnsError()
    {
        var request = new CreateOrderRequest
        {
            UserId = -1,
            Items = [new OrderItem { ProductId = 1, Quantity = 1 }],
        };

        var result = CreateOrderRequestValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainKey("UserId");
    }

    [Fact]
    public void Validate_WithEmptyItems_ReturnsError()
    {
        var request = new CreateOrderRequest { UserId = 1, Items = [] };

        var result = CreateOrderRequestValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainKey("Items");
    }

    [Fact]
    public void Validate_WithZeroQuantity_ReturnsError()
    {
        var request = new CreateOrderRequest
        {
            UserId = 1,
            Items = [new OrderItem { ProductId = 1, Quantity = 0 }],
        };

        var result = CreateOrderRequestValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainKey("Items");
    }

    [Fact]
    public void Validate_WithMultipleErrors_ReturnsAllErrors()
    {
        var request = new CreateOrderRequest { UserId = 0, Items = [] };

        var result = CreateOrderRequestValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainKey("UserId");
        result.Errors.Should().ContainKey("Items");
    }
}
