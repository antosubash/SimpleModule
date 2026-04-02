using System.Net.Http.Json;
using NBomber.Contracts;
using NBomber.CSharp;
using SimpleModule.Orders.Contracts;

namespace SimpleModule.LoadTests.Scenarios;

public static class OrdersScenario
{
    public static ScenarioProps Create(
        HttpClient client,
        string userId,
        int[] productIds,
        LoadProfile? profile = null
    )
    {
        return Scenario
            .Create(
                "orders_crud",
                async context =>
                {
                    // Build order items using real product IDs
                    var items = productIds
                        .Select(pid => new OrderItem
                        {
                            ProductId = pid,
                            Quantity = context.ScenarioInfo.InstanceNumber % 4 + 1,
                        })
                        .ToList();

                    var createRequest = new CreateOrderRequest { UserId = userId, Items = items };
                    var createResponse = await client.PostAsJsonAsync("/api/orders", createRequest);
                    if (!createResponse.IsSuccessStatusCode)
                        return Response.Fail(
                            statusCode: ((int)createResponse.StatusCode).ToString(
                                System.Globalization.CultureInfo.InvariantCulture
                            )
                        );

                    var order = await createResponse.Content.ReadFromJsonAsync<Order>();

                    // Get all
                    var getAllResponse = await client.GetAsync("/api/orders");
                    if (!getAllResponse.IsSuccessStatusCode)
                        return Response.Fail(
                            statusCode: ((int)getAllResponse.StatusCode).ToString(
                                System.Globalization.CultureInfo.InvariantCulture
                            )
                        );

                    // Get by ID
                    var getResponse = await client.GetAsync($"/api/orders/{order!.Id}");
                    if (!getResponse.IsSuccessStatusCode)
                        return Response.Fail(
                            statusCode: ((int)getResponse.StatusCode).ToString(
                                System.Globalization.CultureInfo.InvariantCulture
                            )
                        );

                    // Update order
                    var updateRequest = new UpdateOrderRequest { UserId = userId, Items = items };
                    var updateResponse = await client.PutAsJsonAsync(
                        $"/api/orders/{order.Id}",
                        updateRequest
                    );
                    if (!updateResponse.IsSuccessStatusCode)
                        return Response.Fail(
                            statusCode: ((int)updateResponse.StatusCode).ToString(
                                System.Globalization.CultureInfo.InvariantCulture
                            )
                        );

                    // Delete
                    var deleteResponse = await client.DeleteAsync($"/api/orders/{order.Id}");
                    if (!deleteResponse.IsSuccessStatusCode)
                        return Response.Fail(
                            statusCode: ((int)deleteResponse.StatusCode).ToString(
                                System.Globalization.CultureInfo.InvariantCulture
                            )
                        );

                    return Response.Ok(statusCode: "200");
                }
            )
            .WithoutWarmUp()
            .WithLoadSimulations((profile ?? LoadProfile.Individual).ToSimulations());
    }
}
