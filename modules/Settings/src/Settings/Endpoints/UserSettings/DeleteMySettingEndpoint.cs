using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Settings;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.Settings.Endpoints.UserSettings;

public class DeleteMySettingEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(
                "/me/{**key}",
                async (string key, ISettingsContracts settings, ClaimsPrincipal principal) =>
                {
                    var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (string.IsNullOrEmpty(userId))
                        return Results.Unauthorized();

                    await settings.DeleteSettingAsync(key, SettingScope.User, userId);
                    return Results.NoContent();
                }
            )
            .RequireAuthorization();
}
