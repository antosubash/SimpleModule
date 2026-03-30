using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Tenants.Contracts;

namespace SimpleModule.Tenants.Views;

[ViewPage("Tenants/Browse")]
public class BrowseEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/browse",
                async (ITenantContracts contracts) =>
                {
                    var tenants = (await contracts.GetAllTenantsAsync())
                        .Select(t => new
                        {
                            t.Id,
                            t.Name,
                            t.Slug,
                            t.Status,
                            HostCount = t.Hosts.Count,
                        });
                    return Inertia.Render("Tenants/Browse", new { tenants });
                }
            )
            .AllowAnonymous();
    }
}
