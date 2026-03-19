using SimpleModule.Core;
using SimpleModule.Products.Contracts;

namespace SimpleModule.Orders.Contracts;

[Dto]
public class OrderItem
{
    public ProductId ProductId { get; set; }
    public int Quantity { get; set; }
}
