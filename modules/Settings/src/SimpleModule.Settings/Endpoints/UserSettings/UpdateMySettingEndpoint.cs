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
    public const string Route = SettingsConstants.Routes.Api.UpdateMySetting;
    public const string Method = "PUT";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut(
                Route,
                async Task<IResult> (
                    UpdateSettingRequest request,
                    ISettingsContracts settings,
                    ClaimsPrincipal principal
                ) =>
                {
                    var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (string.IsNullOrEmpty(userId))
                        return Results.Unauthorized();

                    try
                    {
                        await settings.SetSettingAsync(
                            request.Key,
                            request.Value,
                            SettingScope.User,
                            userId
                        );
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
            .RequireAuthorization();
}
