using SimpleModule.Core;
using SimpleModule.Core.Ids;

namespace SimpleModule.Products.Contracts;

[Dto]
public class Product
{
    public ProductId Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
