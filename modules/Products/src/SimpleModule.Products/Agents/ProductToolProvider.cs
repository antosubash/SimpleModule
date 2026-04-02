using SimpleModule.Core.Agents;
using SimpleModule.Products.Contracts;

namespace SimpleModule.Products.Agents;

public class ProductToolProvider(IProductContracts products) : IAgentToolProvider
{
    [AgentTool(Description = "Search and list all products")]
    public async Task<IEnumerable<Product>> SearchProducts() =>
        await products.GetAllProductsAsync();

    [AgentTool(Description = "Get a specific product by its ID")]
    public async Task<Product?> GetProduct(int productId) =>
        await products.GetProductByIdAsync(ProductId.From(productId));
}
