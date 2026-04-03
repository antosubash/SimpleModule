using Microsoft.AspNetCore.Builder;
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
                async (IEmailContracts emailContracts) =>
                    Inertia.Render(
                        "Email/History",
                        new
                        {
                            messages = await emailContracts.QueryMessagesAsync(
                                new QueryEmailMessagesRequest()
                            ),
                        }
                    )
            )
            .RequirePermission(EmailPermissions.ViewHistory);
    }
}
