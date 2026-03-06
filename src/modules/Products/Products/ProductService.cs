using SimpleModule.Products.Contracts;

namespace SimpleModule.Products;

public class ProductService : IProductContracts
{
    public Task<IEnumerable<Product>> GetAllProductsAsync() =>
        Task.FromResult<IEnumerable<Product>>(new[]
        {
            new Product { Id = 1, Name = "Laptop", Price = 999.99m },
            new Product { Id = 2, Name = "Smartphone", Price = 699.99m },
        });

    public Task<Product?> GetProductByIdAsync(int id) =>
        Task.FromResult<Product?>(new Product { Id = id, Name = $"Product {id}", Price = 100.00m });
}
