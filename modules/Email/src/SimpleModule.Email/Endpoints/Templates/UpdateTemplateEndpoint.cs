using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Email.Contracts;

namespace SimpleModule.Email.Endpoints.Templates;

public class UpdateTemplateEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut(
                "/templates/{id}",
                (int id, UpdateEmailTemplateRequest request, IEmailContracts emailContracts) =>
                    CrudEndpoints.Update(() =>
                        emailContracts.UpdateTemplateAsync(EmailTemplateId.From(id), request)
                    )
            )
            .RequirePermission(EmailPermissions.ManageTemplates);
}
