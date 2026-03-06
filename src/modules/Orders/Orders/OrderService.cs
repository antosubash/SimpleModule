using Microsoft.EntityFrameworkCore;
using SimpleModule.Orders.Contracts;
using SimpleModule.Products.Contracts;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Orders;

public class OrderService(OrdersDbContext db, IUserContracts users, IProductContracts products)
    : IOrderContracts
{
    public async Task<IEnumerable<Order>> GetAllOrdersAsync() =>
        await db.Orders.Include(o => o.Items).ToListAsync();

    public async Task<Order?> GetOrderByIdAsync(int id) =>
        await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);

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
            UserId = request.UserId,
            Items = request.Items,
            Total = total,
            CreatedAt = DateTime.UtcNow,
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return order;
    }
}
