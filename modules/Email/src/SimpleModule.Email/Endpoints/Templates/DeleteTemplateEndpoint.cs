using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Email.Contracts;

namespace SimpleModule.Email.Endpoints.Templates;

public class DeleteTemplateEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(
                "/templates/{id}",
                (int id, IEmailContracts emailContracts) =>
                    CrudEndpoints.Delete(() =>
                        emailContracts.DeleteTemplateAsync(EmailTemplateId.From(id))
                    )
            )
            .RequirePermission(EmailPermissions.ManageTemplates);
}
