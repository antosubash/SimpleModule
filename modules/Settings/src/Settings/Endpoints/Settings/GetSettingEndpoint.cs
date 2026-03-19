using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Settings;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.Settings.Endpoints.Settings;

public class GetSettingEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/{key}",
                async (string key, SettingScope scope, ISettingsContracts settings) =>
                {
                    var value = await settings.GetSettingAsync(key, scope);
                    return value is not null
                        ? Results.Ok(
                            new
                            {
                                key,
                                value,
                                scope,
                            }
                        )
                        : Results.NotFound();
                }
            )
            .RequireAuthorization();
}
