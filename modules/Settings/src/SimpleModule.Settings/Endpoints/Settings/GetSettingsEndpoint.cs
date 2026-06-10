using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Settings;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.Settings.Endpoints.Settings;

public class GetSettingsEndpoint : IEndpoint
{
    public const string Route = SettingsConstants.Routes.Api.GetSettings;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                async (ISettingsContracts settings, SettingScope? scope, string? group) =>
                {
                    SettingsFilter? filter = null;
                    if (scope is not null || group is not null)
                    {
                        filter = new SettingsFilter { Scope = scope, Group = group };
                    }

                    var results = await settings.GetSettingValuesAsync(filter);

                    // This admin list serves global (System/Application) configuration.
                    // User-scoped values are per-user and must not be enumerable here —
                    // they are read/written through the /me endpoints, which bind the
                    // target user to the caller's claims.
                    results = results.Where(r => r.Scope != SettingScope.User);
                    return TypedResults.Ok(results);
                }
            )
            .RequirePermission(SettingsPermissions.View);
}
