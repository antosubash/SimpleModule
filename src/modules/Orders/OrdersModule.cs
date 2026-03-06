using System.Text.Json.Serialization;
using SimpleModule.Core;
using SimpleModule.Products;
using SimpleModule.Users;

namespace SimpleModule.Orders;

[Module("Orders")]
public class OrdersModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IOrderService, OrderService>();
    }

    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/orders");

        group.MapGet(
            "/",
            async (IOrderService orderService) =>
            {
                var orders = await orderService.GetAllOrdersAsync();
                return Results.Ok(orders);
            }
        );

        group.MapGet(
            "/{id}",
            async (int id, IOrderService orderService) =>
            {
                var order = await orderService.GetOrderByIdAsync(id);
                return order is not null ? Results.Ok(order) : Results.NotFound();
            }
        );

        group.MapPost(
            "/",
            async (CreateOrderRequest request, IOrderService orderService) =>
            {
                var order = await orderService.CreateOrderAsync(request);
                return Results.Created($"/api/orders/{order.Id}", order);
            }
        );
    }
}

public interface IOrderService
{
    Task<IEnumerable<Order>> GetAllOrdersAsync();
    Task<Order?> GetOrderByIdAsync(int id);
    Task<Order> CreateOrderAsync(CreateOrderRequest request);
}

public class OrderService : IOrderService
{
    private readonly IUserService _userService;
    private readonly IProductService _productService;
    private static int _nextId = 1;
    private static readonly List<Order> _orders = new();

    public OrderService(IUserService userService, IProductService productService)
    {
        _userService = userService;
        _productService = productService;
    }

    public async Task<IEnumerable<Order>> GetAllOrdersAsync()
    {
        return _orders;
    }

    public async Task<Order?> GetOrderByIdAsync(int id)
    {
        return _orders.FirstOrDefault(o => o.Id == id);
    }

    public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
    {
        // Validate user exists
        var user = await _userService.GetUserByIdAsync(request.UserId);
        if (user is null)
            throw new InvalidOperationException($"User with ID {request.UserId} not found");

        // Validate products exist and calculate total
        decimal total = 0;
        foreach (var item in request.Items)
        {
            var product = await _productService.GetProductByIdAsync(item.ProductId);
            if (product is null)
                throw new InvalidOperationException($"Product with ID {item.ProductId} not found");

            total += product.Price * item.Quantity;
        }

        var order = new Order
        {
            Id = _nextId++,
            UserId = request.UserId,
            Items = request.Items,
            Total = total,
            CreatedAt = DateTime.UtcNow,
        };

        _orders.Add(order);
        return order;
    }
}

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public List<OrderItem> Items { get; set; } = new();
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class OrderItem
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public class CreateOrderRequest
{
    public int UserId { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}

[JsonSerializable(typeof(Order))]
[JsonSerializable(typeof(IEnumerable<Order>))]
[JsonSerializable(typeof(CreateOrderRequest))]
public partial class OrdersJsonContext : JsonSerializerContext;
