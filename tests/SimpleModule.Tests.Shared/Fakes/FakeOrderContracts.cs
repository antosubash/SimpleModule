using SimpleModule.Orders.Contracts;

namespace SimpleModule.Tests.Shared.Fakes;

public class FakeOrderContracts : IOrderContracts
{
    public List<Order> Orders { get; set; } = FakeDataGenerators.OrderFaker.Generate(2);

    private int _nextId = 100;

    public Task<IEnumerable<Order>> GetAllOrdersAsync() =>
        Task.FromResult<IEnumerable<Order>>(Orders);

    public Task<Order?> GetOrderByIdAsync(int id) =>
        Task.FromResult(Orders.FirstOrDefault(o => o.Id == id));

    public Task<Order> CreateOrderAsync(CreateOrderRequest request)
    {
        var order = new Order
        {
            Id = _nextId++,
            UserId = request.UserId,
            Items = request.Items,
            Total = 0m,
            CreatedAt = DateTime.UtcNow,
        };
        Orders.Add(order);
        return Task.FromResult(order);
    }
}
