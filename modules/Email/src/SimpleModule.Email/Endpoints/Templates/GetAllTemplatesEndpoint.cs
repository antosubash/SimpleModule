using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Email.Contracts;

namespace SimpleModule.Email.Endpoints.Templates;

public class GetAllTemplatesEndpoint : IEndpoint
{
    public const string Route = EmailConstants.Routes.GetAllTemplates;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                (
                    [AsParameters] QueryEmailTemplatesRequest request,
                    IEmailContracts emailContracts
                ) => emailContracts.QueryTemplatesAsync(request)
            )
            .RequirePermission(EmailPermissions.ViewTemplates);
}
