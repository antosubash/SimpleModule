using SimpleModule.Core;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Orders.Contracts;

[Dto]
public class Order
{
    public OrderId Id { get; set; }
    public UserId UserId { get; set; }
    public List<OrderItem> Items { get; set; } = new();
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
}
