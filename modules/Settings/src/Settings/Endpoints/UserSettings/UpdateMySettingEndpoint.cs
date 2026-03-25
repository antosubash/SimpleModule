using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Settings;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.Settings.Endpoints.UserSettings;

public class UpdateMySettingEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut(
                "/me",
                async (
                    UpdateSettingRequest request,
                    ISettingsContracts settings,
                    ClaimsPrincipal principal
                ) =>
                {
                    var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (string.IsNullOrEmpty(userId))
                        return Results.Unauthorized();

                    await settings.SetSettingAsync(
                        request.Key,
                        request.Value ?? string.Empty,
                        SettingScope.User,
                        userId
                    );
                    return TypedResults.NoContent();
                }
            )
            .RequireAuthorization();
}
