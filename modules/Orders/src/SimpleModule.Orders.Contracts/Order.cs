using SimpleModule.Core.Entities;

namespace SimpleModule.Orders.Contracts;

public class Order : AuditableEntity<OrderId>
{
    public string UserId { get; set; } = string.Empty;
    public List<OrderItem> Items { get; set; } = new();
    public decimal Total { get; set; }
}
