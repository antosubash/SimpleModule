using System.Net.Http.Json;
using NBomber.CSharp;
using NBomber.Contracts;
using SimpleModule.Tests.Shared.Fakes;

namespace SimpleModule.LoadTests.Scenarios;

public static class UsersScenario
{
    public static ScenarioProps Create(HttpClient client)
    {
        return Scenario.Create("users_crud", async context =>
        {
            // Get current user
            var meResponse = await client.GetAsync("/api/users/me");
            if (!meResponse.IsSuccessStatusCode)
                return Response.Fail(statusCode: ((int)meResponse.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));

            // Get all users
            var getAllResponse = await client.GetAsync("/api/users");
            if (!getAllResponse.IsSuccessStatusCode)
                return Response.Fail(statusCode: ((int)getAllResponse.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));

            // Create user
            var createRequest = FakeDataGenerators.CreateUserRequestFaker.Generate();
            var createResponse = await client.PostAsJsonAsync("/api/users", createRequest);
            if (!createResponse.IsSuccessStatusCode)
                return Response.Fail(statusCode: ((int)createResponse.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));

            return Response.Ok(statusCode: "200");
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.RampingConstant(copies: 50, during: TimeSpan.FromSeconds(15)),
            Simulation.KeepConstant(copies: 50, during: TimeSpan.FromMinutes(1))
        );
    }
}
