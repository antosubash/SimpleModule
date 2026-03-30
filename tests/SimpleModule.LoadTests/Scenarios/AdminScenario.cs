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
            var roleName = $"LT-{Guid.NewGuid():N}"[..20];
            using var roleForm = new FormUrlEncodedContent([
                new KeyValuePair<string, string>("name", roleName),
                new KeyValuePair<string, string>("description", "Load test role"),
            ]);
            var roleResponse = await client.PostAsync("/admin/roles/", roleForm);

            // Admin endpoints return 302 redirect on success (Blazor SSR pattern).
            // With AllowAutoRedirect=false, we get the raw 302.
            if (!IsSuccess(roleResponse))
                return Response.Fail(statusCode: ((int)roleResponse.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));

            // Extract role ID from Location header (/admin/roles/{id}/edit)
            var locationPath = roleResponse.Headers.Location?.OriginalString ?? string.Empty;
            var segments = locationPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            // segments: ["admin", "roles", "{id}", "edit"]
            if (segments.Length >= 3)
            {
                var roleId = segments[2];

                // Delete the role (also returns 302 on success)
                var deleteResponse = await client.DeleteAsync($"/admin/roles/{roleId}");
                if (!IsSuccess(deleteResponse))
                    return Response.Fail(statusCode: ((int)deleteResponse.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            return Response.Ok(statusCode: "200");
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.RampingConstant(copies: 50, during: TimeSpan.FromSeconds(5)),
            Simulation.KeepConstant(copies: 50, during: TimeSpan.FromSeconds(20))
        );
    }

    /// <summary>
    /// Admin form endpoints return 302 redirects on success (Blazor SSR pattern).
    /// Treat 2xx and 3xx as success.
    /// </summary>
    private static bool IsSuccess(HttpResponseMessage response)
    {
        var code = (int)response.StatusCode;
        return code is >= 200 and < 400;
    }
}
