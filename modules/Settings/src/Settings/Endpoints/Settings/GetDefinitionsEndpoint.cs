using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Settings;

namespace SimpleModule.Settings.Endpoints.Settings;

public class GetDefinitionsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/definitions",
                (ISettingsDefinitionRegistry registry, SettingScope? scope) =>
                    Results.Ok(registry.GetDefinitions(scope))
            )
            .RequireAuthorization();
}
