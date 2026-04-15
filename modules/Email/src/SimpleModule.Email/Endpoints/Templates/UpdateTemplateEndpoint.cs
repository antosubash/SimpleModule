using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Core.Validation;
using SimpleModule.Email.Contracts;

namespace SimpleModule.Email.Endpoints.Templates;

public class UpdateTemplateEndpoint : IEndpoint
{
    public const string Route = EmailConstants.Routes.UpdateTemplate;
    public const string Method = "PUT";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut(
                Route,
                async (
                    int id,
                    UpdateEmailTemplateRequest request,
                    IValidator<UpdateEmailTemplateRequest> validator,
                    IEmailContracts emailContracts
                ) =>
                {
                    var validation = await validator.ValidateAsync(request);
                    if (!validation.IsValid)
                        throw new Core.Exceptions.ValidationException(
                            validation.ToValidationErrors()
                        );

                    return await CrudEndpoints.Update(() =>
                        emailContracts.UpdateTemplateAsync(EmailTemplateId.From(id), request)
                    );
                }
            )
            .RequirePermission(EmailPermissions.ManageTemplates);
}
