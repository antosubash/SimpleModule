using SimpleModule.Core;

namespace SimpleModule.Products.Contracts;

[Dto]
public class UpdateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
