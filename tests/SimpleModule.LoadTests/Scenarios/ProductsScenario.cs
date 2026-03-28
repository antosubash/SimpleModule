using System.Net.Http.Json;
using NBomber.CSharp;
using NBomber.Contracts;
using SimpleModule.Products.Contracts;
using SimpleModule.Tests.Shared.Fakes;

namespace SimpleModule.LoadTests.Scenarios;

public static class ProductsScenario
{
    public static ScenarioProps Create(HttpClient client)
    {
        return Scenario.Create("products_crud", async context =>
        {
            // Create
            var createRequest = FakeDataGenerators.CreateProductRequestFaker.Generate();
            var createResponse = await client.PostAsJsonAsync("/api/products", createRequest);
            if (!createResponse.IsSuccessStatusCode)
                return Response.Fail(statusCode: ((int)createResponse.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));

            var product = await createResponse.Content.ReadFromJsonAsync<Product>();

            // Get all
            var getAllResponse = await client.GetAsync("/api/products");
            if (!getAllResponse.IsSuccessStatusCode)
                return Response.Fail(statusCode: ((int)getAllResponse.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));

            // Get by ID
            var getResponse = await client.GetAsync($"/api/products/{product!.Id}");
            if (!getResponse.IsSuccessStatusCode)
                return Response.Fail(statusCode: ((int)getResponse.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));

            // Update
            var updateRequest = FakeDataGenerators.UpdateProductRequestFaker.Generate();
            var updateResponse = await client.PutAsJsonAsync($"/api/products/{product.Id}", updateRequest);
            if (!updateResponse.IsSuccessStatusCode)
                return Response.Fail(statusCode: ((int)updateResponse.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));

            // Delete
            var deleteResponse = await client.DeleteAsync($"/api/products/{product.Id}");
            if (!deleteResponse.IsSuccessStatusCode)
                return Response.Fail(statusCode: ((int)deleteResponse.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));

            return Response.Ok(statusCode: "200");
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.KeepConstant(copies: 1, during: TimeSpan.FromSeconds(30))
        );
    }
}
