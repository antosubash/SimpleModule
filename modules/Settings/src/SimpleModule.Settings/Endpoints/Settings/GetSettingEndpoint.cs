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
                async Task<IResult> (string key, SettingScope scope, ISettingsContracts settings) =>
                {
                    var value = await settings.GetSettingAsync(key, scope);
                    return value is not null
                        ? TypedResults.Ok(
                            new
                            {
                                key,
                                value,
                                scope,
                            }
                        )
                        : TypedResults.NotFound();
                }
            )
            .RequireAuthorization();
}
