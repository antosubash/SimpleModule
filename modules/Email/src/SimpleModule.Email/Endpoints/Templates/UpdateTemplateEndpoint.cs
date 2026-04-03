using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Email.Contracts;
using SimpleModule.Email.Validators;

namespace SimpleModule.Email.Endpoints.Templates;

public class UpdateTemplateEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut(
                "/templates/{id}",
                (int id, UpdateEmailTemplateRequest request, IEmailContracts emailContracts) =>
                {
                    var validation = UpdateEmailTemplateRequestValidator.Validate(request);
                    if (!validation.IsValid)
                        throw new Core.Exceptions.ValidationException(validation.Errors);

                    return CrudEndpoints.Update(() =>
                        emailContracts.UpdateTemplateAsync(EmailTemplateId.From(id), request)
                    );
                }
            )
            .RequirePermission(EmailPermissions.ManageTemplates);
}
