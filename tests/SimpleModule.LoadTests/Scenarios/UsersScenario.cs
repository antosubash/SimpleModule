using System.Net.Http.Json;
using NBomber.Contracts;
using NBomber.CSharp;
using SimpleModule.Users.Contracts;

namespace SimpleModule.LoadTests.Scenarios;

public static class UsersScenario
{
    public static ScenarioProps Create(HttpClient client, LoadProfile? profile = null)
    {
        return Scenario
            .Create(
                "users_crud",
                async context =>
                {
                    // Get current user (requires NameIdentifier to match a real Identity user)
                    var meResponse = await client.GetAsync("/api/users/me");
                    if (!meResponse.IsSuccessStatusCode)
                        return Response.Fail(
                            statusCode: ((int)meResponse.StatusCode).ToString(
                                System.Globalization.CultureInfo.InvariantCulture
                            )
                        );

                    // Get user by ID using the ID from /me
                    var currentUser = await meResponse.Content.ReadFromJsonAsync<UserDto>();
                    var getByIdResponse = await client.GetAsync($"/api/users/{currentUser!.Id}");
                    if (!getByIdResponse.IsSuccessStatusCode)
                        return Response.Fail(
                            statusCode: ((int)getByIdResponse.StatusCode).ToString(
                                System.Globalization.CultureInfo.InvariantCulture
                            )
                        );

                    // Get all users
                    var getAllResponse = await client.GetAsync("/api/users");
                    if (!getAllResponse.IsSuccessStatusCode)
                        return Response.Fail(
                            statusCode: ((int)getAllResponse.StatusCode).ToString(
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
