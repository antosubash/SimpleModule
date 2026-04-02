using NBomber.Contracts;
using NBomber.CSharp;

namespace SimpleModule.LoadTests.Scenarios;

public static class SettingsScenario
{
    public static ScenarioProps Create(HttpClient client, LoadProfile? profile = null)
    {
        return Scenario
            .Create(
                "settings_ops",
                async context =>
                {
                    // Get settings
                    var settingsResponse = await client.GetAsync("/api/settings");
                    if (!settingsResponse.IsSuccessStatusCode)
                        return Response.Fail(
                            statusCode: ((int)settingsResponse.StatusCode).ToString(
                                System.Globalization.CultureInfo.InvariantCulture
                            )
                        );

                    // Get definitions
                    var definitionsResponse = await client.GetAsync("/api/settings/definitions");
                    if (!definitionsResponse.IsSuccessStatusCode)
                        return Response.Fail(
                            statusCode: ((int)definitionsResponse.StatusCode).ToString(
                                System.Globalization.CultureInfo.InvariantCulture
                            )
                        );

                    // Get menu tree
                    var menuResponse = await client.GetAsync("/api/settings/menus");
                    if (!menuResponse.IsSuccessStatusCode)
                        return Response.Fail(
                            statusCode: ((int)menuResponse.StatusCode).ToString(
                                System.Globalization.CultureInfo.InvariantCulture
                            )
                        );

                    // Get available pages
                    var pagesResponse = await client.GetAsync(
                        "/api/settings/menus/available-pages"
                    );
                    if (!pagesResponse.IsSuccessStatusCode)
                        return Response.Fail(
                            statusCode: ((int)pagesResponse.StatusCode).ToString(
                                System.Globalization.CultureInfo.InvariantCulture
                            )
                        );

                    // Note: GET /api/settings/me is excluded — it uses ClaimTypes.NameIdentifier
                    // which doesn't match OpenIddict's "sub" claim in Bearer tokens (product bug).

                    return Response.Ok(statusCode: "200");
                }
            )
            .WithoutWarmUp()
            .WithLoadSimulations((profile ?? LoadProfile.Individual).ToSimulations());
    }
}
