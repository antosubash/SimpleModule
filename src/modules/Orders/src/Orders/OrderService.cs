using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SimpleModule.Core.Events;
using SimpleModule.Core.Exceptions;
using SimpleModule.Orders.Contracts;
using SimpleModule.Orders.Contracts.Events;
using SimpleModule.Products.Contracts;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Orders;

public partial class OrderService(
    OrdersDbContext db,
    IUserContracts users,
    IProductContracts products,
    IEventBus eventBus,
    ILogger<OrderService> logger
) : IOrderContracts
{
    public async Task<IEnumerable<Order>> GetAllOrdersAsync() =>
        await db.Orders.Include(o => o.Items).ToListAsync();

    public async Task<Order?> GetOrderByIdAsync(int id)
    {
        var order = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
        if (order is null)
        {
            LogOrderNotFound(logger, id);
        }

        return order;
    }

    public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
    {
        var user = await users.GetUserByIdAsync(request.UserId);
        if (user is null)
        {
            throw new NotFoundException("User", request.UserId);
        }

        var productIds = request.Items.Select(i => i.ProductId).Distinct();
        var productList = await products.GetProductsByIdsAsync(productIds);
        var productMap = productList.ToDictionary(p => p.Id);

        foreach (var item in request.Items)
        {
            if (!productMap.ContainsKey(item.ProductId))
            {
                throw new NotFoundException("Product", item.ProductId);
            }
        }

        var total = request.Items.Sum(item => productMap[item.ProductId].Price * item.Quantity);

        var order = new Order
        {
            UserId = request.UserId,
            Items = request.Items,
            Total = total,
            CreatedAt = DateTime.UtcNow,
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        LogOrderCreated(logger, order.Id, order.UserId, order.Total);

        await eventBus.PublishAsync(new OrderCreatedEvent(order.Id, order.UserId, order.Total));

        return order;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Order with ID {OrderId} not found")]
    private static partial void LogOrderNotFound(ILogger logger, int orderId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Order {OrderId} created for user {UserId}, total: {Total}"
    )]
    private static partial void LogOrderCreated(
        ILogger logger,
        int orderId,
        string userId,
        decimal total
    );
}
