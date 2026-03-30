using System.Net.Http.Json;
using NBomber.CSharp;
using NBomber.Contracts;
using SimpleModule.Core.Settings;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.LoadTests.Scenarios;

public static class SettingsScenario
{
    public static ScenarioProps Create(HttpClient client)
    {
        return Scenario.Create("settings_ops", async context =>
        {
            try
            {
            // Get settings — also log auth header to diagnose 401s
            var settingsResponse = await client.GetAsync("/api/settings");
            if (!settingsResponse.IsSuccessStatusCode)
                return Response.Fail(
                    statusCode: ((int)settingsResponse.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture),
                    message: $"Auth: {client.DefaultRequestHeaders.Authorization?.Scheme ?? "none"}");

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
            }
#pragma warning disable CA1031
            catch (Exception ex)
#pragma warning restore CA1031
            {
                return Response.Fail(message: ex.GetType().Name + ": " + ex.Message);
            }
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.RampingConstant(copies: 50, during: TimeSpan.FromSeconds(5)),
            Simulation.KeepConstant(copies: 50, during: TimeSpan.FromSeconds(20))
        );
    }
}
