using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Endpoints;
using SimpleModule.Email.Contracts;
using SimpleModule.Email.FormRequests;

namespace SimpleModule.Email.Endpoints.Templates;

public class CreateTemplateEndpoint : IEndpoint
{
    public const string Route = EmailConstants.Routes.CreateTemplate;
    public const string Method = "POST";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
            Route,
            async (CreateTemplateFormRequest request, IEmailContracts emailContracts) =>
            {
                var dto = new CreateEmailTemplateRequest
                {
                    Name = request.Name,
                    Slug = request.Slug,
                    Subject = request.Subject,
                    Body = request.Body,
                    IsHtml = request.IsHtml,
                    DefaultReplyTo = request.DefaultReplyTo,
                };

                return await CrudEndpoints.Create(
                    () => emailContracts.CreateTemplateAsync(dto),
                    t => $"/api/email/templates/{t.Id.Value}"
                );
            }
        );
}
