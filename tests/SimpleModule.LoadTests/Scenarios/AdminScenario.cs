using System.Net.Http.Json;
using NBomber.CSharp;
using NBomber.Contracts;
using SimpleModule.Tests.Shared.Fakes;

namespace SimpleModule.LoadTests.Scenarios;

public static class AdminScenario
{
    public static ScenarioProps Create(HttpClient client)
    {
        return Scenario.Create("admin_ops", async context =>
        {
            // Create a role (lightweight Identity operation)
            var roleName = $"LT-{Guid.NewGuid():N}"[..20];
            using var roleForm = new FormUrlEncodedContent([
                new KeyValuePair<string, string>("name", roleName),
                new KeyValuePair<string, string>("description", "Load test role"),
            ]);
            var roleResponse = await client.PostAsync("/admin/roles/", roleForm);
            return roleResponse.IsSuccessStatusCode
                ? Response.Ok(statusCode: ((int)roleResponse.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture))
                : Response.Fail(statusCode: ((int)roleResponse.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.RampingConstant(copies: 5, during: TimeSpan.FromSeconds(5)),
            Simulation.KeepConstant(copies: 5, during: TimeSpan.FromSeconds(30))
        );
    }
}
