using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Email.Contracts;
using SimpleModule.Email.Validators;

namespace SimpleModule.Email.Endpoints.Templates;

public class CreateTemplateEndpoint : IEndpoint
{
    public const string Route = EmailConstants.Routes.CreateTemplate;
    public const string Method = "POST";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                Route,
                (CreateEmailTemplateRequest request, IEmailContracts emailContracts) =>
                {
                    var validation = CreateEmailTemplateRequestValidator.Validate(request);
                    if (!validation.IsValid)
                        throw new Core.Exceptions.ValidationException(validation.Errors);

                    return CrudEndpoints.Create(
                        () => emailContracts.CreateTemplateAsync(request),
                        t => $"/api/email/templates/{t.Id.Value}"
                    );
                }
            )
            .RequirePermission(EmailPermissions.ManageTemplates);
}
