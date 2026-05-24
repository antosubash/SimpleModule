using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.Settings.Endpoints.Settings;

public class GetResolvedSettingEndpoint : IEndpoint
{
    public const string Route = SettingsConstants.Routes.Api.GetResolvedSetting;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                async Task<IResult> (
                    string key,
                    ISettingsContracts settings,
                    ClaimsPrincipal principal
                ) =>
                {
                    var userId =
                        principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
                    var value = await settings.ResolveUserSettingElementAsync(key, userId);
                    return TypedResults.Ok(new { key, value });
                }
            )
            .RequireAuthorization();
}
