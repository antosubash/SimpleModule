using SimpleModule.Core.Entities;

namespace SimpleModule.Products.Contracts;

public class Product : Entity<ProductId>
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
