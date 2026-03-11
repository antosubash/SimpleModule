using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SimpleModule.Products.Contracts;

namespace SimpleModule.Products;

public partial class ProductService(ProductsDbContext db, ILogger<ProductService> logger)
    : IProductContracts
{
    public async Task<IEnumerable<Product>> GetAllProductsAsync() =>
        await db.Products.ToListAsync();

    public async Task<Product?> GetProductByIdAsync(int id)
    {
        var product = await db.Products.FindAsync(id);
        if (product is null)
        {
            LogProductNotFound(logger, id);
        }

        return product;
    }

    public async Task<IReadOnlyList<Product>> GetProductsByIdsAsync(IEnumerable<int> ids)
    {
        var idList = ids.ToList();
        return await db.Products.Where(p => idList.Contains(p.Id)).ToListAsync();
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Product with ID {ProductId} not found")]
    private static partial void LogProductNotFound(ILogger logger, int productId);
}
