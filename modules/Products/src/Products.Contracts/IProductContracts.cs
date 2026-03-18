
namespace SimpleModule.Products.Contracts;

public interface IProductContracts
{
    Task<IEnumerable<Product>> GetAllProductsAsync();
    Task<Product?> GetProductByIdAsync(ProductId id);
    Task<IReadOnlyList<Product>> GetProductsByIdsAsync(IEnumerable<ProductId> ids);
    Task<Product> CreateProductAsync(CreateProductRequest request);
    Task<Product> UpdateProductAsync(ProductId id, UpdateProductRequest request);
    Task DeleteProductAsync(ProductId id);
}
