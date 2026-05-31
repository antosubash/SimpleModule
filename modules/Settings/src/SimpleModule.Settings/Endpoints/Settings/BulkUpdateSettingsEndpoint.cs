using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Settings;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.Settings.Endpoints.Settings;

public class BulkUpdateSettingsEndpoint : IEndpoint
{
    public const string Route = SettingsConstants.Routes.Api.BulkUpdateSettings;
    public const string Method = "PUT";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut(
                Route,
                async Task<IResult> (
                    BulkUpdateSettingsRequest request,
                    ISettingsContracts settings
                ) =>
                {
                    var userScopedKey = request.Updates.FirstOrDefault(u => u.Scope == SettingScope.User)?.Key;
                    if (userScopedKey is not null)
                    {
                        return TypedResults.Problem(
                            detail: "Use /api/settings/me to set user-scoped settings.",
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Invalid scope"
                        );
                    }

                    try
                    {
                        await settings.SetManyAsync(request.Updates);
                        return TypedResults.Ok(new { count = request.Updates.Count });
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
