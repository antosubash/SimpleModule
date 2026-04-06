using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
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
                async (string key, SettingScope scope, ISettingsContracts settings) =>
                {
                    await settings.DeleteSettingAsync(key, scope);
                    return TypedResults.NoContent();
                }
            )
            .RequireAuthorization();
}
