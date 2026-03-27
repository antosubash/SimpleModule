namespace SimpleModule.Orders.Contracts;

public class UpdateOrderRequest
{
    public string UserId { get; set; } = string.Empty;
    public List<OrderItem> Items { get; set; } = new();
}
