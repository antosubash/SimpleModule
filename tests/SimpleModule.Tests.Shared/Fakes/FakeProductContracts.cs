using SimpleModule.Products.Contracts;

namespace SimpleModule.Tests.Shared.Fakes;

public class FakeProductContracts : IProductContracts
{
    public List<Product> Products { get; set; } = FakeDataGenerators.ProductFaker.Generate(3);

    public Task<IEnumerable<Product>> GetAllProductsAsync() =>
        Task.FromResult<IEnumerable<Product>>(Products);

    public Task<Product?> GetProductByIdAsync(int id) =>
        Task.FromResult(Products.FirstOrDefault(p => p.Id == id));

    public Task<IReadOnlyList<Product>> GetProductsByIdsAsync(IEnumerable<int> ids)
    {
        var idSet = ids.ToHashSet();
        return Task.FromResult<IReadOnlyList<Product>>(
            Products.Where(p => idSet.Contains(p.Id)).ToList()
        );
    }

    private int _nextId = 100;

    public Task<Product> CreateProductAsync(CreateProductRequest request)
    {
        var product = new Product
        {
            Id = _nextId++,
            Name = request.Name,
            Price = request.Price,
        };
        Products.Add(product);
        return Task.FromResult(product);
    }

    public Task<Product> UpdateProductAsync(int id, UpdateProductRequest request)
    {
        var product = Products.FirstOrDefault(p => p.Id == id);
        if (product is null)
        {
            throw new SimpleModule.Core.Exceptions.NotFoundException("Product", id);
        }

        product.Name = request.Name;
        product.Price = request.Price;
        return Task.FromResult(product);
    }

    public Task DeleteProductAsync(int id)
    {
        var product = Products.FirstOrDefault(p => p.Id == id);
        if (product is null)
        {
            throw new SimpleModule.Core.Exceptions.NotFoundException("Product", id);
        }

        Products.Remove(product);
        return Task.CompletedTask;
    }
}
