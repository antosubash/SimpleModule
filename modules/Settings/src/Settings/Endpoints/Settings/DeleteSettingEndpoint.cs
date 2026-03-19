using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Settings;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.Settings.Endpoints.Settings;

public class DeleteSettingEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(
                "/{key}",
                async (string key, SettingScope scope, ISettingsContracts settings) =>
                {
                    await settings.DeleteSettingAsync(key, scope);
                    return Results.NoContent();
                }
            )
            .RequireAuthorization();
}
