using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Inertia;
using SimpleModule.Email.Contracts;

namespace SimpleModule.Email.Pages;

public class HistoryEndpoint : IViewEndpoint
{
    public const string Route = EmailConstants.Routes.History;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                Route,
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
