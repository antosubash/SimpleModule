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
    public const string Route = SettingsConstants.Routes.Api.GetMySettings;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                async (
                    ISettingsContracts settings,
                    ISettingsDefinitionRegistry registry,
                    ClaimsPrincipal principal
                ) =>
                {
                    var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (string.IsNullOrEmpty(userId))
                        return Results.Unauthorized();

                    var defs = registry.GetDefinitions(SettingScope.User);
                    var results = new List<UserSettingValueDto>(defs.Count);

                    foreach (var def in defs)
                    {
                        var userDto = await settings.GetSettingValueAsync(
                            def.Key,
                            SettingScope.User,
                            userId
                        );
                        var resolvedElement = await settings.ResolveUserSettingElementAsync(
                            def.Key,
                            userId
                        );
                        results.Add(
                            new UserSettingValueDto
                            {
                                Key = def.Key,
                                Value = userDto?.Value,
                                ResolvedValue = resolvedElement,
                                IsOverridden = userDto is not null,
                            }
                        );
                    }

                    return TypedResults.Ok(results);
                }
            )
            .RequireAuthorization();
}
