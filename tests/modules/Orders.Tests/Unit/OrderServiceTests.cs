using FluentAssertions;
using NSubstitute;
using SimpleModule.Orders;
using SimpleModule.Orders.Contracts;
using SimpleModule.Products.Contracts;
using SimpleModule.Users.Contracts;

namespace Orders.Tests.Unit;

public class OrderServiceTests
{
    private readonly IUserContracts _users = Substitute.For<IUserContracts>();
    private readonly IProductContracts _products = Substitute.For<IProductContracts>();
    private readonly OrderService _sut;

    public OrderServiceTests()
    {
        _sut = new OrderService(_users, _products);
    }

    [Fact]
    public async Task CreateOrderAsync_WithValidUserAndProduct_CalculatesCorrectTotal()
    {
        _users.GetUserByIdAsync(1).Returns(new User { Id = 1, Name = "Test" });
        _products
            .GetProductByIdAsync(1)
            .Returns(
                new Product
                {
                    Id = 1,
                    Name = "Widget",
                    Price = 25.00m,
                }
            );

        var request = new CreateOrderRequest
        {
            UserId = 1,
            Items = [new OrderItem { ProductId = 1, Quantity = 3 }],
        };

        var order = await _sut.CreateOrderAsync(request);

        order.Total.Should().Be(75.00m);
        order.UserId.Should().Be(1);
        order.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateOrderAsync_WithInvalidUser_ThrowsInvalidOperationException()
    {
        _users.GetUserByIdAsync(999).Returns((User?)null);

        var request = new CreateOrderRequest
        {
            UserId = 999,
            Items = [new OrderItem { ProductId = 1, Quantity = 1 }],
        };

        var act = () => _sut.CreateOrderAsync(request);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*User*999*not found*");
    }

    [Fact]
    public async Task CreateOrderAsync_WithInvalidProduct_ThrowsInvalidOperationException()
    {
        _users.GetUserByIdAsync(1).Returns(new User { Id = 1, Name = "Test" });
        _products.GetProductByIdAsync(999).Returns((Product?)null);

        var request = new CreateOrderRequest
        {
            UserId = 1,
            Items = [new OrderItem { ProductId = 999, Quantity = 1 }],
        };

        var act = () => _sut.CreateOrderAsync(request);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*Product*999*not found*");
    }

    [Fact]
    public async Task GetAllOrdersAsync_ReturnsOrdersList()
    {
        var orders = await _sut.GetAllOrdersAsync();

        orders.Should().NotBeNull();
    }

    [Fact]
    public async Task GetOrderByIdAsync_ReturnsMatchingOrder()
    {
        // First create an order
        _users.GetUserByIdAsync(1).Returns(new User { Id = 1, Name = "Test" });
        _products
            .GetProductByIdAsync(1)
            .Returns(
                new Product
                {
                    Id = 1,
                    Name = "Widget",
                    Price = 10.00m,
                }
            );

        var request = new CreateOrderRequest
        {
            UserId = 1,
            Items = [new OrderItem { ProductId = 1, Quantity = 1 }],
        };

        var created = await _sut.CreateOrderAsync(request);
        var found = await _sut.GetOrderByIdAsync(created.Id);

        found.Should().NotBeNull();
        found!.Id.Should().Be(created.Id);
    }
}
