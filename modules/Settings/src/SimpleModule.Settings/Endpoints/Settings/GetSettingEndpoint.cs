using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Settings;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.Settings.Endpoints.Settings;

public class GetSettingEndpoint : IEndpoint
{
    public const string Route = SettingsConstants.Routes.Api.GetSetting;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                async Task<IResult> (
                    string key,
                    SettingScope? scope,
                    ISettingsContracts settings
                ) =>
                {
                    if (scope is null)
                    {
                        return TypedResults.Problem(
                            detail: "Query parameter 'scope' is required.",
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Missing required parameter"
                        );
                    }

                    var dto = await settings.GetSettingValueAsync(key, scope.Value);
                    return dto is not null ? TypedResults.Ok(dto) : TypedResults.NotFound();
                }
            )
            .RequireAuthorization();
}
