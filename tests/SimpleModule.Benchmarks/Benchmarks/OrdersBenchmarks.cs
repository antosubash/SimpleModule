using System.Net.Http.Json;
using BenchmarkDotNet.Attributes;
using SimpleModule.Orders;
using SimpleModule.Orders.Contracts;
using SimpleModule.Tests.Shared.Fakes;
using SimpleModule.Tests.Shared.Fixtures;

namespace SimpleModule.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public sealed class OrdersBenchmarks : IDisposable
{
    private SimpleModuleWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private int _seedOrderId;

    [GlobalSetup]
    public async Task Setup()
    {
        _factory = new SimpleModuleWebApplicationFactory();
        _client = _factory.CreateAuthenticatedClient([
            OrdersPermissions.View,
            OrdersPermissions.Create,
            OrdersPermissions.Update,
            OrdersPermissions.Delete,
        ]);

        // Seed an order for read/update/delete benchmarks
        var request = FakeDataGenerators.CreateOrderRequestFaker.Generate();
        var response = await _client.PostAsJsonAsync("/api/orders", request);
        var order = await response.Content.ReadFromJsonAsync<Order>();
        _seedOrderId = order!.Id.Value;
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
    public async Task<HttpResponseMessage> GetAllOrders() =>
        await _client.GetAsync("/api/orders");

    [Benchmark]
    public async Task<HttpResponseMessage> GetOrderById() =>
        await _client.GetAsync($"/api/orders/{_seedOrderId}");

    [Benchmark]
    public async Task<HttpResponseMessage> CreateOrder()
    {
        var request = FakeDataGenerators.CreateOrderRequestFaker.Generate();
        return await _client.PostAsJsonAsync("/api/orders", request);
    }

    [Benchmark]
    public async Task<HttpResponseMessage> UpdateOrder()
    {
        var request = FakeDataGenerators.UpdateOrderRequestFaker.Generate();
        return await _client.PutAsJsonAsync($"/api/orders/{_seedOrderId}", request);
    }

    [Benchmark]
    public async Task CreateAndDeleteOrder()
    {
        var request = FakeDataGenerators.CreateOrderRequestFaker.Generate();
        var createResponse = await _client.PostAsJsonAsync("/api/orders", request);
        var order = await createResponse.Content.ReadFromJsonAsync<Order>();
        await _client.DeleteAsync($"/api/orders/{order!.Id}");
    }
}
