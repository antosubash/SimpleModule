using System.Net.Http.Json;
using NBomber.CSharp;
using NBomber.Contracts;
using SimpleModule.AuditLogs.Contracts;
using SimpleModule.Core;

namespace SimpleModule.LoadTests.Scenarios;

public static class AuditLogsScenario
{
    public static ScenarioProps Create(HttpClient client)
    {
        return Scenario.Create("auditlogs_read", async context =>
        {
            // Get all audit logs
            var getAllResponse = await client.GetAsync("/api/audit-logs");
            if (!getAllResponse.IsSuccessStatusCode)
                return Response.Fail(statusCode: ((int)getAllResponse.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));

            // Get by ID if any entries exist
            var pagedResult = await getAllResponse.Content.ReadFromJsonAsync<PagedResult<AuditEntry>>();
            if (pagedResult?.Items.Count > 0)
            {
                var firstId = pagedResult.Items[0].Id;
                var getByIdResponse = await client.GetAsync($"/api/audit-logs/{firstId}");
                if (!getByIdResponse.IsSuccessStatusCode)
                    return Response.Fail(statusCode: ((int)getByIdResponse.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));
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
