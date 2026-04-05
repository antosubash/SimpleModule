using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Settings;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.Settings.Endpoints.Settings;

public class GetDefinitionsEndpoint : IEndpoint
{
    public const string Route = SettingsConstants.Routes.Api.GetDefinitions;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                (ISettingsDefinitionRegistry registry, SettingScope? scope) =>
                    TypedResults.Ok(registry.GetDefinitions(scope))
            )
            .RequireAuthorization();
}
