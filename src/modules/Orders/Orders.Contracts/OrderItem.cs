using SimpleModule.Core;

namespace SimpleModule.Orders.Contracts;

[Dto]
public class OrderItem
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}
