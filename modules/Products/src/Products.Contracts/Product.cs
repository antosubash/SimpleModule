namespace SimpleModule.Products.Contracts;

public class Product
{
    public ProductId Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
