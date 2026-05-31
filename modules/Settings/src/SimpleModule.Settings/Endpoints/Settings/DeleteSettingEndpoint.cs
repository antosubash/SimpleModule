using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Settings;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.Settings.Endpoints.Settings;

public class DeleteSettingEndpoint : IEndpoint
{
    public const string Route = SettingsConstants.Routes.Api.DeleteSetting;
    public const string Method = "DELETE";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(
                Route,
                async Task<IResult> (string key, SettingScope? scope, ISettingsContracts settings) =>
                {
                    if (scope is null)
                    {
                        return TypedResults.Problem(
                            detail: "Query parameter 'scope' is required.",
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Missing required parameter"
                        );
                    }

                    await settings.ResetToDefaultAsync(key, scope.Value);
                    return TypedResults.NoContent();
                }
            )
            .RequirePermission(SettingsPermissions.Update);
}
