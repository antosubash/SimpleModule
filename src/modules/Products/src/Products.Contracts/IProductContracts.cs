namespace SimpleModule.Products.Contracts;

public interface IProductContracts
{
    Task<IEnumerable<Product>> GetAllProductsAsync();
    Task<Product?> GetProductByIdAsync(int id);
    Task<IReadOnlyList<Product>> GetProductsByIdsAsync(IEnumerable<int> ids);
    Task<Product> CreateProductAsync(CreateProductRequest request);
    Task<Product> UpdateProductAsync(int id, UpdateProductRequest request);
    Task DeleteProductAsync(int id);
}
