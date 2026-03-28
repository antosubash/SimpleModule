using NBomber.CSharp;
using NBomber.Contracts;

namespace SimpleModule.LoadTests.Scenarios;

public static class UsersScenario
{
    public static ScenarioProps Create(HttpClient client)
    {
        return Scenario.Create("users_crud", async context =>
        {
            // Get current user (requires NameIdentifier to match a real Identity user)
            var meResponse = await client.GetAsync("/api/users/me");
            if (!meResponse.IsSuccessStatusCode)
                return Response.Fail(statusCode: ((int)meResponse.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));

            // Get all users
            var getAllResponse = await client.GetAsync("/api/users");
            if (!getAllResponse.IsSuccessStatusCode)
                return Response.Fail(statusCode: ((int)getAllResponse.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));

            return Response.Ok(statusCode: "200");
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.RampingConstant(copies: 50, during: TimeSpan.FromSeconds(15)),
            Simulation.KeepConstant(copies: 50, during: TimeSpan.FromMinutes(1))
        );
    }
}
