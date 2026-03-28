using NBomber.CSharp;
using NBomber.Contracts;

namespace SimpleModule.LoadTests.Scenarios;

public static class SettingsScenario
{
    public static ScenarioProps Create(HttpClient client)
    {
        return Scenario.Create("settings_ops", async context =>
        {
            // Get settings
            var settingsResponse = await client.GetAsync("/api/settings");
            if (!settingsResponse.IsSuccessStatusCode)
                return Response.Fail(statusCode: ((int)settingsResponse.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));

            // Get definitions
            var definitionsResponse = await client.GetAsync("/api/settings/definitions");
            if (!definitionsResponse.IsSuccessStatusCode)
                return Response.Fail(statusCode: ((int)definitionsResponse.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));

            // Get menu tree
            var menuResponse = await client.GetAsync("/api/settings/menus");
            if (!menuResponse.IsSuccessStatusCode)
                return Response.Fail(statusCode: ((int)menuResponse.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));

            // Get available pages
            var pagesResponse = await client.GetAsync("/api/settings/menus/available-pages");
            if (!pagesResponse.IsSuccessStatusCode)
                return Response.Fail(statusCode: ((int)pagesResponse.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));

            // Get my settings
            var mySettingsResponse = await client.GetAsync("/api/settings/me");
            if (!mySettingsResponse.IsSuccessStatusCode)
                return Response.Fail(statusCode: ((int)mySettingsResponse.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));

            return Response.Ok(statusCode: "200");
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.RampingConstant(copies: 5, during: TimeSpan.FromSeconds(5)),
            Simulation.KeepConstant(copies: 5, during: TimeSpan.FromSeconds(30))
        );
    }
}
