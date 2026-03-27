namespace SimpleModule.Orders.Contracts;

public class CreateOrderRequest
{
    public string UserId { get; set; } = string.Empty;
    public List<OrderItem> Items { get; set; } = new();
}
