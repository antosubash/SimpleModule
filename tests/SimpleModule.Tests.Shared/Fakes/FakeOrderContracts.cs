using SimpleModule.Core.Ids;
using SimpleModule.Orders.Contracts;

namespace SimpleModule.Tests.Shared.Fakes;

public class FakeOrderContracts : IOrderContracts
{
    public List<Order> Orders { get; set; } = FakeDataGenerators.OrderFaker.Generate(2);

    private int _nextId = 100;

    public Task<IEnumerable<Order>> GetAllOrdersAsync() =>
        Task.FromResult<IEnumerable<Order>>(Orders);

    public Task<Order?> GetOrderByIdAsync(OrderId id) =>
        Task.FromResult(Orders.FirstOrDefault(o => o.Id == id));

    public Task<Order> CreateOrderAsync(CreateOrderRequest request)
    {
        var order = new Order
        {
            Id = OrderId.From(_nextId++),
            UserId = request.UserId,
            Items = request.Items,
            Total = 0m,
            CreatedAt = DateTime.UtcNow,
        };
        Orders.Add(order);
        return Task.FromResult(order);
    }

    public Task<Order> UpdateOrderAsync(OrderId id, UpdateOrderRequest request)
    {
        var order = Orders.FirstOrDefault(o => o.Id == id);
        if (order is null)
        {
            throw new SimpleModule.Core.Exceptions.NotFoundException("Order", id);
        }

        order.UserId = request.UserId;
        order.Items = request.Items;
        return Task.FromResult(order);
    }

    public Task DeleteOrderAsync(OrderId id)
    {
        var order = Orders.FirstOrDefault(o => o.Id == id);
        if (order is null)
        {
            throw new SimpleModule.Core.Exceptions.NotFoundException("Order", id);
        }

        Orders.Remove(order);
        return Task.CompletedTask;
    }
}
