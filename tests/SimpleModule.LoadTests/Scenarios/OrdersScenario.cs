using System.Net.Http.Json;
using NBomber.CSharp;
using NBomber.Contracts;
using SimpleModule.Orders.Contracts;
using SimpleModule.Tests.Shared.Fakes;

namespace SimpleModule.LoadTests.Scenarios;

public static class OrdersScenario
{
    public static ScenarioProps Create(HttpClient client, string userId)
    {
        return Scenario.Create("orders_crud", async context =>
        {
            // Create order with pre-seeded user ID
            var createRequest = FakeDataGenerators.CreateOrderRequestFaker.Generate();
            createRequest.UserId = userId;
            var createResponse = await client.PostAsJsonAsync("/api/orders", createRequest);
            if (!createResponse.IsSuccessStatusCode)
                return Response.Fail(statusCode: ((int)createResponse.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));

            var order = await createResponse.Content.ReadFromJsonAsync<Order>();

            // Get all
            var getAllResponse = await client.GetAsync("/api/orders");
            if (!getAllResponse.IsSuccessStatusCode)
                return Response.Fail(statusCode: ((int)getAllResponse.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));

            // Get by ID
            var getResponse = await client.GetAsync($"/api/orders/{order!.Id}");
            if (!getResponse.IsSuccessStatusCode)
                return Response.Fail(statusCode: ((int)getResponse.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));

            // Delete
            var deleteResponse = await client.DeleteAsync($"/api/orders/{order.Id}");
            if (!deleteResponse.IsSuccessStatusCode)
                return Response.Fail(statusCode: ((int)deleteResponse.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));

            return Response.Ok(statusCode: "200");
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.RampingConstant(copies: 5, during: TimeSpan.FromSeconds(5)),
            Simulation.KeepConstant(copies: 5, during: TimeSpan.FromSeconds(30))
        );
    }
}
