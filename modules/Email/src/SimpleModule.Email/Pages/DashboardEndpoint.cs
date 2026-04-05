using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Inertia;
using SimpleModule.Email.Contracts;

namespace SimpleModule.Email.Pages;

public class DashboardEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/dashboard",
                async (IEmailContracts emailContracts) =>
                    Inertia.Render(
                        "Email/Dashboard",
                        new { stats = await emailContracts.GetEmailStatsAsync() }
                    )
            )
            .RequirePermission(EmailPermissions.ViewHistory);
    }
}
