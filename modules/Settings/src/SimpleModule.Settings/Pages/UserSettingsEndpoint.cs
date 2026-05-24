using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Core.Settings;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.Settings.Pages;

public class UserSettingsEndpoint : IViewEndpoint
{
    public const string Route = SettingsConstants.Routes.Views.UserSettings;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                Route,
                async (
                    ISettingsContracts settings,
                    ISettingsDefinitionRegistry registry,
                    ClaimsPrincipal principal
                ) =>
                {
                    var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
                    var definitions = registry.GetDefinitions(SettingScope.User);

                    var userSettings = new List<UserSettingValueDto>(definitions.Count);
                    foreach (var def in definitions)
                    {
                        var userDto = await settings.GetSettingValueAsync(
                            def.Key,
                            SettingScope.User,
                            userId
                        );
                        var resolvedElement = await settings.ResolveUserSettingElementAsync(
                            def.Key,
                            userId ?? string.Empty
                        );
                        userSettings.Add(
                            new UserSettingValueDto
                            {
                                Key = def.Key,
                                Value = userDto?.Value,
                                ResolvedValue = resolvedElement,
                                IsOverridden = userDto is not null,
                            }
                        );
                    }

                    return Inertia.Render(
                        "Settings/UserSettings",
                        new { definitions, settings = userSettings }
                    );
                }
            )
            .RequireAuthorization();
    }
}
