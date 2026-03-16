using SimpleModule.Core;

namespace SimpleModule.Products.Contracts;

[Dto]
public class CreateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
