using System.Net.Http.Json;
using System.Security.Cryptography;
using NBomber.CSharp;
using NBomber.Contracts;
using SimpleModule.Products.Contracts;
using SimpleModule.Tests.Shared.Fakes;

namespace SimpleModule.LoadTests.Scenarios;

public static class MixedWorkloadScenario
{
    public static ScenarioProps Create(HttpClient client)
    {
        return Scenario.Create("mixed_realistic", async context =>
        {
            // Weighted random: 70% reads, 20% creates, 10% updates
            var roll = RandomNumberGenerator.GetInt32(100);

            if (roll < 70)
            {
                // Read operations across modules
                var endpoint = RandomNumberGenerator.GetInt32(5) switch
                {
                    0 => "/api/products",
                    1 => "/api/orders",
                    2 => "/api/users",
                    3 => "/api/settings",
                    _ => "/api/audit-logs",
                };

                var response = await client.GetAsync(endpoint);
                return response.IsSuccessStatusCode
                    ? Response.Ok(statusCode: ((int)response.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture))
                    : Response.Fail(statusCode: ((int)response.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            if (roll < 90)
            {
                // Create operations
                var createRoll = RandomNumberGenerator.GetInt32(2);
                if (createRoll == 0)
                {
                    var request = FakeDataGenerators.CreateProductRequestFaker.Generate();
                    var response = await client.PostAsJsonAsync("/api/products", request);
                    return response.IsSuccessStatusCode
                        ? Response.Ok(statusCode: ((int)response.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture))
                        : Response.Fail(statusCode: ((int)response.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));
                }
                else
                {
                    var request = FakeDataGenerators.CreateOrderRequestFaker.Generate();
                    var response = await client.PostAsJsonAsync("/api/orders", request);
                    return response.IsSuccessStatusCode
                        ? Response.Ok(statusCode: ((int)response.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture))
                        : Response.Fail(statusCode: ((int)response.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));
                }
            }

            // Update operations (10%)
            {
                // Create then update a product
                var createReq = FakeDataGenerators.CreateProductRequestFaker.Generate();
                var createResp = await client.PostAsJsonAsync("/api/products", createReq);
                if (!createResp.IsSuccessStatusCode)
                    return Response.Fail(statusCode: ((int)createResp.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));

                var product = await createResp.Content.ReadFromJsonAsync<Product>();
                var updateReq = FakeDataGenerators.UpdateProductRequestFaker.Generate();
                var updateResp = await client.PutAsJsonAsync($"/api/products/{product!.Id}", updateReq);
                return updateResp.IsSuccessStatusCode
                    ? Response.Ok(statusCode: ((int)updateResp.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture))
                    : Response.Fail(statusCode: ((int)updateResp.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.RampingConstant(copies: 50, during: TimeSpan.FromSeconds(15)),
            Simulation.KeepConstant(copies: 50, during: TimeSpan.FromMinutes(1))
        );
    }
}
