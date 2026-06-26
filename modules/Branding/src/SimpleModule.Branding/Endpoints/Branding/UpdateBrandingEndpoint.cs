using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Branding.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;

namespace SimpleModule.Branding.Endpoints.Branding;

public class UpdateBrandingEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut(
                "/api/branding",
                async (BrandingEditModel model, IBrandingContracts branding) =>
                {
                    await branding.UpdateAsync(model);
                    return TypedResults.Ok();
                }
            )
            .RequirePermission(BrandingPermissions.Manage)
            .DisableAntiforgery();
}
