using SimpleModule.Core;

namespace SimpleModule.Orders.Contracts;

[Dto]
public class Order
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public List<OrderItem> Items { get; set; } = new();
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
}
