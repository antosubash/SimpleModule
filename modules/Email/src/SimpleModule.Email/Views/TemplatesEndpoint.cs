using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Inertia;
using SimpleModule.Email.Contracts;

namespace SimpleModule.Email.Views;

[ViewPage("Email/Templates")]
public class TemplatesEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/templates",
                async (
                    [AsParameters] QueryEmailTemplatesRequest request,
                    IEmailContracts emailContracts
                ) =>
                    Inertia.Render(
                        "Email/Templates",
                        new
                        {
                            result = await emailContracts.QueryTemplatesAsync(request),
                            filters = request,
                        }
                    )
            )
            .RequirePermission(EmailPermissions.ViewTemplates);
    }
}
