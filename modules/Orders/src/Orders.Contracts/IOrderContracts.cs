using SimpleModule.Core.Ids;

namespace SimpleModule.Orders.Contracts;

public interface IOrderContracts
{
    Task<IEnumerable<Order>> GetAllOrdersAsync();
    Task<Order?> GetOrderByIdAsync(OrderId id);
    Task<Order> CreateOrderAsync(CreateOrderRequest request);
    Task<Order> UpdateOrderAsync(OrderId id, UpdateOrderRequest request);
    Task DeleteOrderAsync(OrderId id);
}
