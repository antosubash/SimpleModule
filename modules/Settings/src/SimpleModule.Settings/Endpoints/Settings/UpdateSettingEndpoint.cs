using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Settings;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.Settings.Endpoints.Settings;

public class UpdateSettingEndpoint : IEndpoint
{
    public const string Route = SettingsConstants.Routes.Api.UpdateSetting;
    public const string Method = "PUT";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut(
                Route,
                async Task<IResult> (UpdateSettingRequest request, ISettingsContracts settings) =>
                {
                    if (request.Scope == SettingScope.User)
                    {
                        return TypedResults.Problem(
                            detail: "Use /api/settings/me to set user-scoped settings.",
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Invalid scope"
                        );
                    }

                    try
                    {
                        await settings.SetSettingAsync(request.Key, request.Value, request.Scope);
                        return TypedResults.NoContent();
                    }
                    catch (SettingValidationException ex)
                    {
                        return TypedResults.ValidationProblem(
                            new Dictionary<string, string[]> { [ex.Key] = ex.Errors.ToArray() }
                        );
                    }
                }
            )
            .RequirePermission(SettingsPermissions.Update);
}
