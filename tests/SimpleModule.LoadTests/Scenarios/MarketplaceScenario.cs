using NBomber.Contracts;
using NBomber.CSharp;

namespace SimpleModule.LoadTests.Scenarios;

public static class MarketplaceScenario
{
    public static ScenarioProps Create(HttpClient client, LoadProfile? profile = null)
    {
        return Scenario
            .Create(
                "marketplace_read",
                async context =>
                {
                    // Search packages (anonymous, no auth required)
                    var searchResponse = await client.GetAsync("/api/marketplace?q=auth&take=10");
                    if (!searchResponse.IsSuccessStatusCode)
                        return Response.Fail(
                            statusCode: ((int)searchResponse.StatusCode).ToString(
                                System.Globalization.CultureInfo.InvariantCulture
                            )
                        );

                    // Browse all packages
                    var browseResponse = await client.GetAsync("/api/marketplace");
                    if (!browseResponse.IsSuccessStatusCode)
                        return Response.Fail(
                            statusCode: ((int)browseResponse.StatusCode).ToString(
                                System.Globalization.CultureInfo.InvariantCulture
                            )
                        );

                    return Response.Ok(statusCode: "200");
                }
            )
            .WithoutWarmUp()
            .WithLoadSimulations((profile ?? LoadProfile.Individual).ToSimulations());
    }
}
