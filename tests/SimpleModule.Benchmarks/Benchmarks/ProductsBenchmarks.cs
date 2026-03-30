using System.Net.Http.Json;
using BenchmarkDotNet.Attributes;
using SimpleModule.Products;
using SimpleModule.Products.Contracts;
using SimpleModule.Tests.Shared.Fakes;
using SimpleModule.Tests.Shared.Fixtures;

namespace SimpleModule.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public sealed class ProductsBenchmarks : IDisposable
{
    private SimpleModuleWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private int _seedProductId;

    [GlobalSetup]
    public async Task Setup()
    {
        _factory = new SimpleModuleWebApplicationFactory();
        _client = _factory.CreateAuthenticatedClient([
            ProductsPermissions.View,
            ProductsPermissions.Create,
            ProductsPermissions.Update,
            ProductsPermissions.Delete,
        ]);

        // Seed a product for read/update/delete benchmarks
        var request = FakeDataGenerators.CreateProductRequestFaker.Generate();
        var response = await _client.PostAsJsonAsync("/api/products", request);
        var product = await response.Content.ReadFromJsonAsync<Product>();
        _seedProductId = product!.Id.Value;
    }

    [GlobalCleanup]
    public void Cleanup() => Dispose();

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Benchmark]
    public async Task<HttpResponseMessage> GetAllProducts() =>
        await _client.GetAsync("/api/products");

    [Benchmark]
    public async Task<HttpResponseMessage> GetProductById() =>
        await _client.GetAsync($"/api/products/{_seedProductId}");

    [Benchmark]
    public async Task<HttpResponseMessage> CreateProduct()
    {
        var request = FakeDataGenerators.CreateProductRequestFaker.Generate();
        return await _client.PostAsJsonAsync("/api/products", request);
    }

    [Benchmark]
    public async Task<HttpResponseMessage> UpdateProduct()
    {
        var request = FakeDataGenerators.UpdateProductRequestFaker.Generate();
        return await _client.PutAsJsonAsync($"/api/products/{_seedProductId}", request);
    }

    [Benchmark]
    public async Task CreateAndDeleteProduct()
    {
        var request = FakeDataGenerators.CreateProductRequestFaker.Generate();
        var createResponse = await _client.PostAsJsonAsync("/api/products", request);
        var product = await createResponse.Content.ReadFromJsonAsync<Product>();
        await _client.DeleteAsync($"/api/products/{product!.Id}");
    }
}
