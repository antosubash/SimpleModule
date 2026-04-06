using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Inertia;
using SimpleModule.Email.Contracts;

namespace SimpleModule.Email.Pages;

public class TemplatesEndpoint : IViewEndpoint
{
    public const string Route = EmailConstants.Routes.Templates;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                Route,
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
