using System.Net.Http.Json;
using NBomber.Contracts;
using NBomber.CSharp;
using SimpleModule.FeatureFlags.Contracts;

namespace SimpleModule.LoadTests.Scenarios;

public static class FeatureFlagsScenario
{
    public static ScenarioProps Create(HttpClient client, LoadProfile? profile = null)
    {
        return Scenario
            .Create(
                "featureflags_ops",
                async context =>
                {
                    // Get all feature flags
                    var getAllResponse = await client.GetAsync("/api/feature-flags");
                    if (!getAllResponse.IsSuccessStatusCode)
                        return Response.Fail(
                            statusCode: ((int)getAllResponse.StatusCode).ToString(
                                System.Globalization.CultureInfo.InvariantCulture
                            )
                        );

                    // Check a flag (may not exist, but the endpoint should return 200)
                    var checkResponse = await client.GetAsync(
                        "/api/feature-flags/check/load-test-flag"
                    );
                    if (!checkResponse.IsSuccessStatusCode)
                        return Response.Fail(
                            statusCode: ((int)checkResponse.StatusCode).ToString(
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
