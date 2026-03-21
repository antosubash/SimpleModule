using SimpleModule.Users.Contracts;

namespace SimpleModule.Orders.Contracts;

public class UpdateOrderRequest
{
    public UserId UserId { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}
