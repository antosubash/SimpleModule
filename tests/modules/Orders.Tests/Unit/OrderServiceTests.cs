using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using SimpleModule.Core.Events;
using SimpleModule.Core.Exceptions;
using SimpleModule.Database;
using SimpleModule.Orders;
using SimpleModule.Orders.Contracts;
using SimpleModule.Products.Contracts;
using SimpleModule.Users.Contracts;

namespace Orders.Tests.Unit;

public sealed class OrderServiceTests : IDisposable
{
    private readonly OrdersDbContext _db;
    private readonly IUserContracts _users = Substitute.For<IUserContracts>();
    private readonly IProductContracts _products = Substitute.For<IProductContracts>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();
    private readonly OrderService _sut;

    public OrderServiceTests()
    {
        var options = new DbContextOptionsBuilder<OrdersDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        var dbOptions = Options.Create(
            new DatabaseOptions
            {
                ModuleConnections = new Dictionary<string, string>
                {
                    ["Orders"] = "Data Source=:memory:",
                },
            }
        );
        _db = new OrdersDbContext(options, dbOptions);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();
        _sut = new OrderService(
            _db,
            _users,
            _products,
            _eventBus,
            NullLogger<OrderService>.Instance
        );
    }

    public void Dispose() => _db.Dispose();

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
    public async Task CreateOrderAsync_WithInvalidUser_ThrowsNotFoundException()
    {
        _users.GetUserByIdAsync(999).Returns((User?)null);

        var request = new CreateOrderRequest
        {
            UserId = 999,
            Items = [new OrderItem { ProductId = 1, Quantity = 1 }],
        };

        var act = () => _sut.CreateOrderAsync(request);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*User*999*not found*");
    }

    [Fact]
    public async Task CreateOrderAsync_WithInvalidProduct_ThrowsNotFoundException()
    {
        _users.GetUserByIdAsync(1).Returns(new User { Id = 1, Name = "Test" });
        _products.GetProductByIdAsync(999).Returns((Product?)null);

        var request = new CreateOrderRequest
        {
            UserId = 1,
            Items = [new OrderItem { ProductId = 999, Quantity = 1 }],
        };

        var act = () => _sut.CreateOrderAsync(request);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*Product*999*not found*");
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
