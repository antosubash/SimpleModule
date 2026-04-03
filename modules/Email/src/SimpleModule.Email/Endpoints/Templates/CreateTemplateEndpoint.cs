using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Email.Contracts;

namespace SimpleModule.Email.Endpoints.Templates;

public class CreateTemplateEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                "/templates",
                (CreateEmailTemplateRequest request, IEmailContracts emailContracts) =>
                    CrudEndpoints.Create(
                        () => emailContracts.CreateTemplateAsync(request),
                        t => $"/api/email/templates/{t.Id.Value}"
                    )
            )
            .RequirePermission(EmailPermissions.ManageTemplates);
}
