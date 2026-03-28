using System.Net;
using SimpleModule.LoadTests.Infrastructure;

namespace SimpleModule.LoadTests;

public sealed class SmokeTest : IClassFixture<LoadTestWebApplicationFactory>, IDisposable
{
    private readonly HttpClient _client;

    public SmokeTest(LoadTestWebApplicationFactory factory)
    {
        _client = factory.CreateServiceAccountClient();
    }

    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task GetProducts_Returns200()
    {
        var response = await _client.GetAsync("/api/products");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetOrders_Returns200()
    {
        var response = await _client.GetAsync("/api/orders");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetSettings_Returns200()
    {
        var response = await _client.GetAsync("/api/settings");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
