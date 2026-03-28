using NBomber.CSharp;
using NBomber.Contracts;

namespace SimpleModule.LoadTests.Scenarios;

public static class PageBuilderScenario
{
    public static ScenarioProps Create(HttpClient client)
    {
        return Scenario.Create("pagebuilder_crud", async context =>
        {
            // Get all pages
            var getAllResponse = await client.GetAsync("/api/pagebuilder");
            if (!getAllResponse.IsSuccessStatusCode)
                return Response.Fail(statusCode: ((int)getAllResponse.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));

            // Get tags
            var tagsResponse = await client.GetAsync("/api/pagebuilder/tags");
            if (!tagsResponse.IsSuccessStatusCode)
                return Response.Fail(statusCode: ((int)tagsResponse.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));

            // Get templates
            var templatesResponse = await client.GetAsync("/api/pagebuilder/templates");
            if (!templatesResponse.IsSuccessStatusCode)
                return Response.Fail(statusCode: ((int)templatesResponse.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));

            // Get trash
            var trashResponse = await client.GetAsync("/api/pagebuilder/trash");
            if (!trashResponse.IsSuccessStatusCode)
                return Response.Fail(statusCode: ((int)trashResponse.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));

            return Response.Ok(statusCode: "200");
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.RampingConstant(copies: 5, during: TimeSpan.FromSeconds(5)),
            Simulation.KeepConstant(copies: 5, during: TimeSpan.FromSeconds(30))
        );
    }
}
