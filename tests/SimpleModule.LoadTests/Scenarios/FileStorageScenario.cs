using NBomber.CSharp;
using NBomber.Contracts;

namespace SimpleModule.LoadTests.Scenarios;

public static class FileStorageScenario
{
    public static ScenarioProps Create(HttpClient client)
    {
        return Scenario.Create("files_ops", async context =>
        {
            // Get all files
            var getAllResponse = await client.GetAsync("/api/files");
            if (!getAllResponse.IsSuccessStatusCode)
                return Response.Fail(statusCode: ((int)getAllResponse.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));

            // List folders
            var foldersResponse = await client.GetAsync("/api/files/folders");
            if (!foldersResponse.IsSuccessStatusCode)
                return Response.Fail(statusCode: ((int)foldersResponse.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));

            return Response.Ok(statusCode: "200");
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.RampingConstant(copies: 50, during: TimeSpan.FromSeconds(5)),
            Simulation.KeepConstant(copies: 50, during: TimeSpan.FromSeconds(20))
        );
    }
}
