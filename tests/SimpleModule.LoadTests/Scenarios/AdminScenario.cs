using NBomber.CSharp;
using NBomber.Contracts;

namespace SimpleModule.LoadTests.Scenarios;

public static class AdminScenario
{
    public static ScenarioProps Create(HttpClient client)
    {
        return Scenario.Create("admin_ops", async context =>
        {
            // Create a role
            var roleName = $"LoadTestRole-{Guid.NewGuid():N}"[..30];
            var roleForm = new FormUrlEncodedContent([
                new KeyValuePair<string, string>("name", roleName),
                new KeyValuePair<string, string>("description", "Load test role"),
            ]);
            var roleResponse = await client.PostAsync("/admin/roles/", roleForm);
            if (!roleResponse.IsSuccessStatusCode)
                return Response.Fail(statusCode: ((int)roleResponse.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));

            // Create a user
            var email = $"lt-{Guid.NewGuid():N}"[..20] + "@test.dev";
            var userForm = new FormUrlEncodedContent([
                new KeyValuePair<string, string>("email", email),
                new KeyValuePair<string, string>("displayName", "Load Test User"),
                new KeyValuePair<string, string>("password", "LoadTest123!"),
                new KeyValuePair<string, string>("emailConfirmed", "true"),
            ]);
            var userResponse = await client.PostAsync("/admin/users/", userForm);
            if (!userResponse.IsSuccessStatusCode)
                return Response.Fail(statusCode: ((int)userResponse.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));

            return Response.Ok(statusCode: "200");
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.RampingConstant(copies: 10, during: TimeSpan.FromSeconds(10)),
            Simulation.KeepConstant(copies: 10, during: TimeSpan.FromMinutes(1))
        );
    }
}
