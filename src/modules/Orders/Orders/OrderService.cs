using SimpleModule.Orders.Contracts;
using SimpleModule.Products.Contracts;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Orders;

public class OrderService(IUserContracts users, IProductContracts products) : IOrderContracts
{
    private static int _nextId = 1;
    private static readonly List<Order> _orders = new();

    public Task<IEnumerable<Order>> GetAllOrdersAsync() =>
        Task.FromResult<IEnumerable<Order>>(_orders);

    public Task<Order?> GetOrderByIdAsync(int id) =>
        Task.FromResult(_orders.FirstOrDefault(o => o.Id == id));

    public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
    {
        var user = await users.GetUserByIdAsync(request.UserId);
        if (user is null)
            throw new InvalidOperationException($"User with ID {request.UserId} not found");

        decimal total = 0;
        foreach (var item in request.Items)
        {
            var product = await products.GetProductByIdAsync(item.ProductId);
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
