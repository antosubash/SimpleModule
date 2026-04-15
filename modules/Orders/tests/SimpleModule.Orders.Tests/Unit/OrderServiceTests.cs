using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using SimpleModule.Core.Exceptions;
using SimpleModule.Database;
using SimpleModule.Orders;
using SimpleModule.Orders.Contracts;
using SimpleModule.Products.Contracts;
using SimpleModule.Users.Contracts;
using Wolverine;

namespace Orders.Tests.Unit;

public sealed class OrderServiceTests : IDisposable
{
    private readonly OrdersDbContext _db;
    private readonly IUserContracts _users = Substitute.For<IUserContracts>();
    private readonly IProductContracts _products = Substitute.For<IProductContracts>();
    private readonly IMessageBus _bus = Substitute.For<IMessageBus>();
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
        _sut = new OrderService(_db, _users, _products, _bus, NullLogger<OrderService>.Instance);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task CreateOrderAsync_WithValidUserAndProduct_CalculatesCorrectTotal()
    {
        _users
            .GetUserByIdAsync(UserId.From("1"))
            .Returns(new UserDto { Id = UserId.From("1"), DisplayName = "Test" });
        var widget = new Product
        {
            Id = ProductId.From(1),
            Name = "Widget",
            Price = 25.00m,
        };
        _products
            .GetProductsByIdsAsync(Arg.Any<IEnumerable<ProductId>>())
            .Returns(callInfo =>
            {
                var ids = callInfo.Arg<IEnumerable<ProductId>>().ToHashSet();
                return new List<Product> { widget }
                        .Where(p => ids.Contains(p.Id))
                        .ToList() as IReadOnlyList<Product>;
            });

        var request = new CreateOrderRequest
        {
            UserId = "1",
            Items = [new OrderItem { ProductId = 1, Quantity = 3 }],
        };

        var order = await _sut.CreateOrderAsync(request);

        order.Total.Should().Be(75.00m);
        order.UserId.Should().Be("1");
        order.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateOrderAsync_WithInvalidUser_ThrowsNotFoundException()
    {
        _users.GetUserByIdAsync(UserId.From("999")).Returns((UserDto?)null);

        var request = new CreateOrderRequest
        {
            UserId = "999",
            Items = [new OrderItem { ProductId = 1, Quantity = 1 }],
        };

        var act = () => _sut.CreateOrderAsync(request);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*User*999*not found*");
    }

    [Fact]
    public async Task CreateOrderAsync_WithInvalidProduct_ThrowsNotFoundException()
    {
        _users
            .GetUserByIdAsync(UserId.From("1"))
            .Returns(new UserDto { Id = UserId.From("1"), DisplayName = "Test" });
        _products
            .GetProductsByIdsAsync(Arg.Any<IEnumerable<ProductId>>())
            .Returns(new List<Product>() as IReadOnlyList<Product>);

        var request = new CreateOrderRequest
        {
            UserId = "1",
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
        _users
            .GetUserByIdAsync(UserId.From("1"))
            .Returns(new UserDto { Id = UserId.From("1"), DisplayName = "Test" });
        var widget = new Product
        {
            Id = ProductId.From(1),
            Name = "Widget",
            Price = 10.00m,
        };
        _products
            .GetProductsByIdsAsync(Arg.Any<IEnumerable<ProductId>>())
            .Returns(callInfo =>
            {
                var ids = callInfo.Arg<IEnumerable<ProductId>>().ToHashSet();
                return new List<Product> { widget }
                        .Where(p => ids.Contains(p.Id))
                        .ToList() as IReadOnlyList<Product>;
            });

        var request = new CreateOrderRequest
        {
            UserId = "1",
            Items = [new OrderItem { ProductId = 1, Quantity = 1 }],
        };

        var created = await _sut.CreateOrderAsync(request);
        var found = await _sut.GetOrderByIdAsync(created.Id);

        found.Should().NotBeNull();
        found!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task UpdateOrderAsync_WithValidData_UpdatesOrder()
    {
        _users
            .GetUserByIdAsync(UserId.From("1"))
            .Returns(new UserDto { Id = UserId.From("1"), DisplayName = "Test" });
        var widget = new Product
        {
            Id = ProductId.From(1),
            Name = "Widget",
            Price = 10.00m,
        };
        _products
            .GetProductsByIdsAsync(Arg.Any<IEnumerable<ProductId>>())
            .Returns(callInfo =>
            {
                var ids = callInfo.Arg<IEnumerable<ProductId>>().ToHashSet();
                return new List<Product> { widget }
                        .Where(p => ids.Contains(p.Id))
                        .ToList() as IReadOnlyList<Product>;
            });

        var createRequest = new CreateOrderRequest
        {
            UserId = "1",
            Items = [new OrderItem { ProductId = 1, Quantity = 1 }],
        };
        var created = await _sut.CreateOrderAsync(createRequest);

        var updateRequest = new UpdateOrderRequest
        {
            UserId = "1",
            Items = [new OrderItem { ProductId = 1, Quantity = 5 }],
        };
        var updated = await _sut.UpdateOrderAsync(created.Id, updateRequest);

        updated.Total.Should().Be(50.00m);
        updated.Items.Should().HaveCount(1);
        updated.Items[0].Quantity.Should().Be(5);
    }

    [Fact]
    public async Task UpdateOrderAsync_WithNonExistentOrder_ThrowsNotFoundException()
    {
        var request = new UpdateOrderRequest
        {
            UserId = "1",
            Items = [new OrderItem { ProductId = 1, Quantity = 1 }],
        };

        var act = () => _sut.UpdateOrderAsync(OrderId.From(99999), request);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*Order*99999*not found*");
    }

    [Fact]
    public async Task DeleteOrderAsync_WithExistingOrder_RemovesOrder()
    {
        _users
            .GetUserByIdAsync(UserId.From("1"))
            .Returns(new UserDto { Id = UserId.From("1"), DisplayName = "Test" });
        var widget = new Product
        {
            Id = ProductId.From(1),
            Name = "Widget",
            Price = 10.00m,
        };
        _products
            .GetProductsByIdsAsync(Arg.Any<IEnumerable<ProductId>>())
            .Returns(callInfo =>
            {
                var ids = callInfo.Arg<IEnumerable<ProductId>>().ToHashSet();
                return new List<Product> { widget }
                        .Where(p => ids.Contains(p.Id))
                        .ToList() as IReadOnlyList<Product>;
            });

        var createRequest = new CreateOrderRequest
        {
            UserId = "1",
            Items = [new OrderItem { ProductId = 1, Quantity = 1 }],
        };
        var created = await _sut.CreateOrderAsync(createRequest);

        await _sut.DeleteOrderAsync(created.Id);

        var found = await _sut.GetOrderByIdAsync(created.Id);
        found.Should().BeNull();
    }

    [Fact]
    public async Task DeleteOrderAsync_WithNonExistentOrder_ThrowsNotFoundException()
    {
        var act = () => _sut.DeleteOrderAsync(OrderId.From(99999));

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*Order*99999*not found*");
    }
}
