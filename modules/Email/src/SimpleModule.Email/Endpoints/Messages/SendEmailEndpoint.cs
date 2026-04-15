using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Validation;
using SimpleModule.Email.Contracts;

namespace SimpleModule.Email.Endpoints.Messages;

public class SendEmailEndpoint : IEndpoint
{
    public const string Route = EmailConstants.Routes.SendEmail;
    public const string Method = "POST";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                Route,
                async (
                    SendEmailRequest request,
                    IValidator<SendEmailRequest> validator,
                    IEmailContracts emailContracts
                ) =>
                {
                    var validation = await validator.ValidateAsync(request);
                    if (!validation.IsValid)
                        throw new Core.Exceptions.ValidationException(
                            validation.ToValidationErrors()
                        );

                    var message = await emailContracts.SendEmailAsync(request);
                    return TypedResults.Ok(message);
                }
            )
            .RequirePermission(EmailPermissions.Send);
}
