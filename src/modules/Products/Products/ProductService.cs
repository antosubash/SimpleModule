using Microsoft.EntityFrameworkCore;
using SimpleModule.Products.Contracts;

namespace SimpleModule.Products;

public class ProductService(ProductsDbContext db) : IProductContracts
{
    public async Task<IEnumerable<Product>> GetAllProductsAsync() =>
        await db.Products.ToListAsync();

    public async Task<Product?> GetProductByIdAsync(int id) => await db.Products.FindAsync(id);
}
