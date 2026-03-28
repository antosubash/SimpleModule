using NBomber.CSharp;
using NBomber.Contracts;

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

            // Get stats
            var statsResponse = await client.GetAsync("/api/audit-logs/stats");
            if (!statsResponse.IsSuccessStatusCode)
                return Response.Fail(statusCode: ((int)statsResponse.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));

            return Response.Ok(statusCode: "200");
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.RampingConstant(copies: 50, during: TimeSpan.FromSeconds(15)),
            Simulation.KeepConstant(copies: 50, during: TimeSpan.FromMinutes(1))
        );
    }
}
