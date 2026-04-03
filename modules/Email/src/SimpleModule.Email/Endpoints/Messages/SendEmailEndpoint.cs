using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Email.Contracts;

namespace SimpleModule.Email.Endpoints.Messages;

public class SendEmailEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                "/messages/send",
                async (SendEmailRequest request, IEmailContracts emailContracts) =>
                {
                    var message = await emailContracts.SendEmailAsync(request);
                    return TypedResults.Ok(message);
                }
            )
            .RequirePermission(EmailPermissions.Send);
}
