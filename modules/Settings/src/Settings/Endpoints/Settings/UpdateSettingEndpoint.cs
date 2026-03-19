using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.Settings.Endpoints.Settings;

public class UpdateSettingEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut(
                "/",
                async (UpdateSettingRequest request, ISettingsContracts settings) =>
                {
                    await settings.SetSettingAsync(
                        request.Key,
                        request.Value ?? string.Empty,
                        request.Scope
                    );
                    return Results.NoContent();
                }
            )
            .RequireAuthorization();
}
