using Microsoft.AspNetCore.Builder;
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
                async (IEmailContracts emailContracts) =>
                    Inertia.Render(
                        "Email/Templates",
                        new
                        {
                            templates = await emailContracts.QueryTemplatesAsync(
                                new QueryEmailTemplatesRequest()
                            ),
                        }
                    )
            )
            .RequirePermission(EmailPermissions.ViewTemplates);
    }
}
