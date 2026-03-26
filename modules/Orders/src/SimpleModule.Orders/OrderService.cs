using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SimpleModule.Core.Events;
using SimpleModule.Core.Exceptions;
using SimpleModule.Orders.Contracts;
using SimpleModule.Orders.Contracts.Events;
using SimpleModule.Products.Contracts;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Orders;

public sealed partial class OrderService(
    OrdersDbContext db,
    IUserContracts users,
    IProductContracts products,
    IEventBus eventBus,
    ILogger<OrderService> logger
) : IOrderContracts
{
    public async Task<IEnumerable<Order>> GetAllOrdersAsync() =>
        await db.Orders.AsNoTracking().Include(o => o.Items).ToListAsync();

    public async Task<Order?> GetOrderByIdAsync(OrderId id)
    {
        var order = await db
            .Orders.AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);
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

        decimal total = 0;
        foreach (var item in request.Items)
        {
            if (!productMap.TryGetValue(item.ProductId, out var product))
            {
                throw new NotFoundException("Product", item.ProductId);
            }

            total += product.Price * item.Quantity;
        }

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

    public async Task<Order> UpdateOrderAsync(OrderId id, UpdateOrderRequest request)
    {
        var order = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
        if (order is null)
        {
            throw new NotFoundException("Order", id);
        }

        var user = await users.GetUserByIdAsync(request.UserId);
        if (user is null)
        {
            throw new NotFoundException("User", request.UserId);
        }

        var productIds = request.Items.Select(i => i.ProductId).Distinct();
        var productList = await products.GetProductsByIdsAsync(productIds);
        var productMap = productList.ToDictionary(p => p.Id);

        decimal total = 0;
        foreach (var item in request.Items)
        {
            if (!productMap.TryGetValue(item.ProductId, out var product))
            {
                throw new NotFoundException("Product", item.ProductId);
            }

            total += product.Price * item.Quantity;
        }

        order.UserId = request.UserId;
        order.Items = request.Items;
        order.Total = total;

        await db.SaveChangesAsync();

        LogOrderUpdated(logger, order.Id, order.UserId, order.Total);

        return order;
    }

    public async Task DeleteOrderAsync(OrderId id)
    {
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == id);
        if (order is null)
        {
            throw new NotFoundException("Order", id);
        }

        db.Orders.Remove(order);
        await db.SaveChangesAsync();

        LogOrderDeleted(logger, id);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Order with ID {OrderId} not found")]
    private static partial void LogOrderNotFound(ILogger logger, OrderId orderId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Order {OrderId} created for user {UserId}, total: {Total}"
    )]
    private static partial void LogOrderCreated(
        ILogger logger,
        OrderId orderId,
        UserId userId,
        decimal total
    );

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Order {OrderId} updated for user {UserId}, total: {Total}"
    )]
    private static partial void LogOrderUpdated(
        ILogger logger,
        OrderId orderId,
        UserId userId,
        decimal total
    );

    [LoggerMessage(Level = LogLevel.Information, Message = "Order {OrderId} deleted")]
    private static partial void LogOrderDeleted(ILogger logger, OrderId orderId);
}
