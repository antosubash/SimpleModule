using SimpleModule.Products.Contracts;

namespace SimpleModule.Tests.Shared.Fakes;

public class FakeProductContracts : IProductContracts
{
    public List<Product> Products { get; set; } = FakeDataGenerators.ProductFaker.Generate(3);

    public Task<IEnumerable<Product>> GetAllProductsAsync() =>
        Task.FromResult<IEnumerable<Product>>(Products);

    public Task<Product?> GetProductByIdAsync(int id) =>
        Task.FromResult(Products.FirstOrDefault(p => p.Id == id));
}
