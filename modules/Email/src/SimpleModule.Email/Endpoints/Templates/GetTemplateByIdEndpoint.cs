using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Email.Contracts;

namespace SimpleModule.Email.Endpoints.Templates;

public class GetTemplateByIdEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/templates/{id}",
                (int id, IEmailContracts emailContracts) =>
                    CrudEndpoints.GetById(() =>
                        emailContracts.GetTemplateByIdAsync(EmailTemplateId.From(id))
                    )
            )
            .RequirePermission(EmailPermissions.ViewTemplates);
}
