using SimpleModule.Core;

namespace SimpleModule.Orders.Contracts;

[Dto]
public class CreateOrderRequest
{
    public int UserId { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}
