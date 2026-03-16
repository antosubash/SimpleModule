using SimpleModule.Core;

namespace SimpleModule.Orders.Contracts;

[Dto]
public class UpdateOrderRequest
{
    public string UserId { get; set; } = string.Empty;
    public List<OrderItem> Items { get; set; } = new();
}
