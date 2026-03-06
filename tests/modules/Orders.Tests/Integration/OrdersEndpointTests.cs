using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SimpleModule.Orders.Contracts;
using SimpleModule.Tests.Shared.Fixtures;

namespace Orders.Tests.Integration;

public class OrdersEndpointTests : IClassFixture<SimpleModuleWebApplicationFactory>
{
    private readonly HttpClient _client;

    public OrdersEndpointTests(SimpleModuleWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAllOrders_Returns200()
    {
        var response = await _client.GetAsync("/api/orders");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetOrderById_WithNonExistentId_Returns404()
    {
        var response = await _client.GetAsync("/api/orders/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateOrder_WithValidBody_Returns201()
    {
        var request = new CreateOrderRequest
        {
            UserId = 1,
            Items = [new OrderItem { ProductId = 1, Quantity = 2 }],
        };

        var response = await _client.PostAsJsonAsync("/api/orders", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await response.Content.ReadFromJsonAsync<Order>();
        order.Should().NotBeNull();
        order!.UserId.Should().Be(1);
        order.Total.Should().BeGreaterThan(0);
    }
}
