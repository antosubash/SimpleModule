using System.Net.Http.Json;
using NBomber.CSharp;
using NBomber.Contracts;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.LoadTests.Scenarios;

public static class PageBuilderScenario
{
    public static ScenarioProps Create(HttpClient client)
    {
        return Scenario.Create("pagebuilder_crud", async context =>
        {
            try
            {
                // Create a page
                var createRequest = new CreatePageRequest
                {
                    Title = $"Load Test Page {context.ScenarioInfo.InstanceNumber}",
                    Slug = $"lt-{Guid.NewGuid():N}",
                };
                var createResponse = await client.PostAsJsonAsync("/api/pagebuilder", createRequest);
                if (!createResponse.IsSuccessStatusCode)
                    return Response.Fail(statusCode: ((int)createResponse.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));

                var page = await createResponse.Content.ReadFromJsonAsync<Page>();

                // Get by ID
                var getByIdResponse = await client.GetAsync($"/api/pagebuilder/{page!.Id}");
                if (!getByIdResponse.IsSuccessStatusCode)
                    return Response.Fail(statusCode: ((int)getByIdResponse.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));

                // Update title
                var updateRequest = new UpdatePageRequest
                {
                    Title = $"Updated {createRequest.Title}",
                    Slug = page.Slug,
                };
                var updateResponse = await client.PutAsJsonAsync($"/api/pagebuilder/{page.Id}", updateRequest);
                if (!updateResponse.IsSuccessStatusCode)
                    return Response.Fail(statusCode: ((int)updateResponse.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));

                // Publish
                var publishResponse = await client.PostAsync($"/api/pagebuilder/{page.Id}/publish", null);
                if (!publishResponse.IsSuccessStatusCode)
                    return Response.Fail(statusCode: ((int)publishResponse.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));

                // Unpublish
                var unpublishResponse = await client.PostAsync($"/api/pagebuilder/{page.Id}/unpublish", null);
                if (!unpublishResponse.IsSuccessStatusCode)
                    return Response.Fail(statusCode: ((int)unpublishResponse.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));

                // Delete
                var deleteResponse = await client.DeleteAsync($"/api/pagebuilder/{page.Id}");
                if (!deleteResponse.IsSuccessStatusCode)
                    return Response.Fail(statusCode: ((int)deleteResponse.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));

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

                return Response.Ok(statusCode: "200");
            }
#pragma warning disable CA1031 // Catch scenario exceptions for NBomber reporting
            catch (Exception ex)
#pragma warning restore CA1031
            {
                return Response.Fail(message: ex.Message);
            }
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.RampingConstant(copies: 50, during: TimeSpan.FromSeconds(5)),
            Simulation.KeepConstant(copies: 50, during: TimeSpan.FromSeconds(20))
        );
    }
}
