using SimpleModule.Core;
using SimpleModule.Core.Ids;

namespace SimpleModule.Orders.Contracts;

[Dto]
public class OrderItem
{
    public ProductId ProductId { get; set; }
    public int Quantity { get; set; }
}
