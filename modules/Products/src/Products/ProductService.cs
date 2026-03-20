using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SimpleModule.Products.Contracts;

namespace SimpleModule.Products;

public partial class ProductService(ProductsDbContext db, ILogger<ProductService> logger)
    : IProductContracts
{
    public async Task<IEnumerable<Product>> GetAllProductsAsync() =>
        await db.Products.AsNoTracking().ToListAsync();

    public async Task<Product?> GetProductByIdAsync(ProductId id)
    {
        var product = await db.Products.FindAsync(id);
        if (product is null)
        {
            LogProductNotFound(logger, id);
        }

        return product;
    }

    public async Task<IReadOnlyList<Product>> GetProductsByIdsAsync(IEnumerable<ProductId> ids)
    {
        var idList = ids.ToList();
        return await db.Products.AsNoTracking().Where(p => idList.Contains(p.Id)).ToListAsync();
    }

    public async Task<Product> CreateProductAsync(CreateProductRequest request)
    {
        var product = new Product { Name = request.Name, Price = request.Price };

        db.Products.Add(product);
        await db.SaveChangesAsync();

        LogProductCreated(logger, product.Id, product.Name);

        return product;
    }

    public async Task<Product> UpdateProductAsync(ProductId id, UpdateProductRequest request)
    {
        var product = await db.Products.FindAsync(id);
        if (product is null)
        {
            throw new Core.Exceptions.NotFoundException("Product", id);
        }

        product.Name = request.Name;
        product.Price = request.Price;

        await db.SaveChangesAsync();

        LogProductUpdated(logger, product.Id, product.Name);

        return product;
    }

    public async Task DeleteProductAsync(ProductId id)
    {
        var product = await db.Products.FindAsync(id);
        if (product is null)
        {
            throw new Core.Exceptions.NotFoundException("Product", id);
        }

        db.Products.Remove(product);
        await db.SaveChangesAsync();

        LogProductDeleted(logger, id);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Product with ID {ProductId} not found")]
    private static partial void LogProductNotFound(ILogger logger, ProductId productId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Product {ProductId} created: {ProductName}"
    )]
    private static partial void LogProductCreated(
        ILogger logger,
        ProductId productId,
        string productName
    );

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Product {ProductId} updated: {ProductName}"
    )]
    private static partial void LogProductUpdated(
        ILogger logger,
        ProductId productId,
        string productName
    );

    [LoggerMessage(Level = LogLevel.Information, Message = "Product {ProductId} deleted")]
    private static partial void LogProductDeleted(ILogger logger, ProductId productId);
}
