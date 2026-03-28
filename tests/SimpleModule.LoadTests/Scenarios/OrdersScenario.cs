using System.Net.Http.Json;
using NBomber.CSharp;
using NBomber.Contracts;
using SimpleModule.Orders.Contracts;
using SimpleModule.Tests.Shared.Fakes;

namespace SimpleModule.LoadTests.Scenarios;

public static class OrdersScenario
{
    public static ScenarioProps Create(HttpClient client)
    {
        return Scenario.Create("orders_crud", async context =>
        {
            // Create
            var createRequest = FakeDataGenerators.CreateOrderRequestFaker.Generate();
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

            // Update
            var updateRequest = FakeDataGenerators.UpdateOrderRequestFaker.Generate();
            var updateResponse = await client.PutAsJsonAsync($"/api/orders/{order.Id}", updateRequest);
            if (!updateResponse.IsSuccessStatusCode)
                return Response.Fail(statusCode: ((int)updateResponse.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));

            // Delete
            var deleteResponse = await client.DeleteAsync($"/api/orders/{order.Id}");
            if (!deleteResponse.IsSuccessStatusCode)
                return Response.Fail(statusCode: ((int)deleteResponse.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));

            return Response.Ok(statusCode: "200");
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.RampingConstant(copies: 50, during: TimeSpan.FromSeconds(15)),
            Simulation.KeepConstant(copies: 50, during: TimeSpan.FromMinutes(1))
        );
    }
}
