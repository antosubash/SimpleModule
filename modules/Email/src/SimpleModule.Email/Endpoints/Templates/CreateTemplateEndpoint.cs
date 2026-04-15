using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Core.Validation;
using SimpleModule.Email.Contracts;

namespace SimpleModule.Email.Endpoints.Templates;

public class CreateTemplateEndpoint : IEndpoint
{
    public const string Route = EmailConstants.Routes.CreateTemplate;
    public const string Method = "POST";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                Route,
                async (
                    CreateEmailTemplateRequest request,
                    IValidator<CreateEmailTemplateRequest> validator,
                    IEmailContracts emailContracts
                ) =>
                {
                    var validation = await validator.ValidateAsync(request);
                    if (!validation.IsValid)
                        throw new Core.Exceptions.ValidationException(
                            validation.ToValidationErrors()
                        );

                    return await CrudEndpoints.Create(
                        () => emailContracts.CreateTemplateAsync(request),
                        t => $"/api/email/templates/{t.Id.Value}"
                    );
                }
            )
            .RequirePermission(EmailPermissions.ManageTemplates);
}
