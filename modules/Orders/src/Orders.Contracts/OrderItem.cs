using SimpleModule.Products.Contracts;

namespace SimpleModule.Orders.Contracts;

public class OrderItem
{
    public ProductId ProductId { get; set; }
    public int Quantity { get; set; }
}
