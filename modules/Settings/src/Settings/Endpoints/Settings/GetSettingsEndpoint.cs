using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Settings;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.Settings.Endpoints.Settings;

public class GetSettingsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/",
                async (ISettingsContracts settings, SettingScope? scope) =>
                {
                    var filter = scope is not null ? new SettingsFilter { Scope = scope } : null;
                    var results = await settings.GetSettingsAsync(filter);
                    return TypedResults.Ok(results);
                }
            )
            .RequireAuthorization();
}
