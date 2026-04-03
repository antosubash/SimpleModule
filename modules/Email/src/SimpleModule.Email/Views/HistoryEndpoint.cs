using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Inertia;
using SimpleModule.Email.Contracts;

namespace SimpleModule.Email.Views;

[ViewPage("Email/History")]
public class HistoryEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/history",
                async (
                    [AsParameters] QueryEmailMessagesRequest request,
                    IEmailContracts emailContracts
                ) =>
                    Inertia.Render(
                        "Email/History",
                        new
                        {
                            result = await emailContracts.QueryMessagesAsync(request),
                            filters = request,
                        }
                    )
            )
            .RequirePermission(EmailPermissions.ViewHistory);
    }
}
