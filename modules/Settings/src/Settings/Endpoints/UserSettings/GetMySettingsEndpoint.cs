using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Settings;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.Settings.Endpoints.UserSettings;

public class GetMySettingsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/me",
                async (
                    ISettingsContracts settings,
                    ISettingsDefinitionRegistry registry,
                    ClaimsPrincipal principal
                ) =>
                {
                    var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (string.IsNullOrEmpty(userId))
                        return Results.Unauthorized();

                    var definitions = registry.GetDefinitions(SettingScope.User);
                    var results = new List<object>();

                    foreach (var def in definitions)
                    {
                        var resolved = await settings.ResolveUserSettingAsync(def.Key, userId);
                        var userValue = await settings.GetSettingAsync(
                            def.Key,
                            SettingScope.User,
                            userId
                        );
                        results.Add(
                            new
                            {
                                definition = def,
                                value = resolved,
                                isOverridden = userValue is not null,
                            }
                        );
                    }

                    return Results.Ok(results);
                }
            )
            .RequireAuthorization();
}
