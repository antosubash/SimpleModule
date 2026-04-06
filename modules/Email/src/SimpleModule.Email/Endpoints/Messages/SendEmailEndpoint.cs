using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Email.Contracts;
using SimpleModule.Email.Validators;

namespace SimpleModule.Email.Endpoints.Messages;

public class SendEmailEndpoint : IEndpoint
{
    public const string Route = EmailConstants.Routes.SendEmail;
    public const string Method = "POST";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                Route,
                async (SendEmailRequest request, IEmailContracts emailContracts) =>
                {
                    var validation = SendEmailRequestValidator.Validate(request);
                    if (!validation.IsValid)
                        throw new Core.Exceptions.ValidationException(validation.Errors);

                    var message = await emailContracts.SendEmailAsync(request);
                    return TypedResults.Ok(message);
                }
            )
            .RequirePermission(EmailPermissions.Send);
}
