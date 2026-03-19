using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Core.Settings;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.Settings.Views;

public class AdminSettingsEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/",
                async (ISettingsContracts settings, ISettingsDefinitionRegistry registry) =>
                {
                    var definitions = registry.GetDefinitions();
                    var storedSettings = await settings.GetSettingsAsync();
                    return Inertia.Render(
                        "Settings/AdminSettings",
                        new { definitions, settings = storedSettings }
                    );
                }
            )
            .RequireAuthorization();
    }
}
