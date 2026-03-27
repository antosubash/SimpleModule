using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SimpleModule.Orders;
using SimpleModule.Orders.Contracts;
using SimpleModule.Tests.Shared.Fixtures;

namespace Orders.Tests.Integration;

public class OrdersEndpointTests : IClassFixture<SimpleModuleWebApplicationFactory>
{
    private readonly SimpleModuleWebApplicationFactory _factory;

    public OrdersEndpointTests(SimpleModuleWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAllOrders_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient([OrdersPermissions.View]);

        var response = await client.GetAsync("/api/orders");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetOrderById_WithNonExistentId_Returns404()
    {
        var client = _factory.CreateAuthenticatedClient([OrdersPermissions.View]);

        var response = await client.GetAsync("/api/orders/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateOrder_WithInvalidUserId_Returns404()
    {
        var client = _factory.CreateAuthenticatedClient([OrdersPermissions.Create]);

        var request = new CreateOrderRequest
        {
            UserId = "nonexistent-user-id",
            Items = [new OrderItem { ProductId = 1, Quantity = 2 }],
        };

        var response = await client.PostAsJsonAsync("/api/orders", request);

        // User doesn't exist, so the order service throws NotFoundException
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateOrder_WithNonExistentId_Returns404()
    {
        var client = _factory.CreateAuthenticatedClient([OrdersPermissions.Update]);

        var request = new UpdateOrderRequest
        {
            UserId = "1",
            Items = [new OrderItem { ProductId = 1, Quantity = 1 }],
        };

        var response = await client.PutAsJsonAsync("/api/orders/99999", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteOrder_WithNonExistentId_Returns404()
    {
        var client = _factory.CreateAuthenticatedClient([OrdersPermissions.Delete]);

        var response = await client.DeleteAsync("/api/orders/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
