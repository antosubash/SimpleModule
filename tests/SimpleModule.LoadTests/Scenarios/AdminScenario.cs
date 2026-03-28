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
            if (!roleResponse.IsSuccessStatusCode)
                return Response.Fail(statusCode: ((int)roleResponse.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));

            // Extract the role ID from the followed redirect URL (/admin/roles/{id}/edit)
            var requestUri = roleResponse.RequestMessage?.RequestUri?.AbsolutePath ?? string.Empty;
            var segments = requestUri.Split('/', StringSplitOptions.RemoveEmptyEntries);
            // segments: ["admin", "roles", "{id}", "edit"]
            if (segments.Length >= 3)
            {
                var roleId = segments[2];

                // Delete the role
                var deleteResponse = await client.DeleteAsync($"/admin/roles/{roleId}");
                if (!deleteResponse.IsSuccessStatusCode)
                    return Response.Fail(statusCode: ((int)deleteResponse.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            return Response.Ok(statusCode: "200");
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.RampingConstant(copies: 50, during: TimeSpan.FromSeconds(15)),
            Simulation.KeepConstant(copies: 50, during: TimeSpan.FromMinutes(1))
        );
    }
}
