using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Branding.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Inertia;

namespace SimpleModule.Branding.Pages;

public class ManageEndpoint : IViewEndpoint
{
    // Route is relative to the module's ViewPrefix ("/branding"), so this maps to
    // GET /branding/manage (mirrors Settings' "/settings/manage").
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/manage",
                async (IBrandingContracts branding) =>
                    Inertia.Render(
                        "Branding/Manage",
                        new { branding = await branding.GetEditableAsync() }
                    )
            )
            .RequirePermission(BrandingPermissions.Manage);
}
