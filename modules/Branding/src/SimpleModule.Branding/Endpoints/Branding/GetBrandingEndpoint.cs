using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Branding.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;

namespace SimpleModule.Branding.Endpoints.Branding;

public class GetBrandingEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/api/branding",
                async (IBrandingContracts branding) =>
                    TypedResults.Ok(await branding.GetEditableAsync())
            )
            .RequirePermission(BrandingPermissions.Manage);
}
