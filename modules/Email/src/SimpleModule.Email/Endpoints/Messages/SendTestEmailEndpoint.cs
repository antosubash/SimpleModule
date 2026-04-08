using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Email.Contracts;

namespace SimpleModule.Email.Endpoints.Messages;

public sealed record SendTestEmailRequest(string To, string? Subject, string? Body);

public class SendTestEmailEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                EmailConstants.Routes.TestSend,
                async (SendTestEmailRequest request, IEmailContracts emailContracts) =>
                {
                    var full = new SendEmailRequest
                    {
                        To = request.To,
                        Subject = request.Subject ?? "Worker test",
                        Body = request.Body ?? "Hello from the SimpleModule worker.",
                        IsHtml = false,
                    };
                    var message = await emailContracts.SendEmailAsync(full);
                    return TypedResults.Ok(new
                    {
                        messageId = message.Id,
                        status = "enqueued",
                    });
                }
            )
            .RequirePermission(EmailPermissions.Send);
}
