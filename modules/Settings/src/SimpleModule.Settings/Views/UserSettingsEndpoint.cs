using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Core.Settings;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.Settings.Views;

public class UserSettingsEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/me",
                async (
                    ISettingsContracts settings,
                    ISettingsDefinitionRegistry registry,
                    ClaimsPrincipal principal
                ) =>
                {
                    var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
                    var definitions = registry.GetDefinitions(SettingScope.User);

                    var userSettings = new List<object>();
                    foreach (var def in definitions)
                    {
                        var resolved = await settings.ResolveUserSettingAsync(
                            def.Key,
                            userId ?? string.Empty
                        );
                        var userValue = await settings.GetSettingAsync(
                            def.Key,
                            SettingScope.User,
                            userId
                        );
                        userSettings.Add(
                            new
                            {
                                definition = def,
                                value = resolved,
                                isOverridden = userValue is not null,
                            }
                        );
                    }

                    return Inertia.Render("Settings/UserSettings", new { settings = userSettings });
                }
            )
            .RequireAuthorization();
    }
}
