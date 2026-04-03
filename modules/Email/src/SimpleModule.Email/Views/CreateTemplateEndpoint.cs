using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Inertia;
using SimpleModule.Email.Contracts;

namespace SimpleModule.Email.Views;

[ViewPage("Email/CreateTemplate")]
public class CreateTemplateEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/templates/create", () => Inertia.Render("Email/CreateTemplate"))
            .RequirePermission(EmailPermissions.ManageTemplates);

        app.MapPost(
                "/templates",
                async ([AsParameters] CreateTemplateForm form, IEmailContracts emailContracts) =>
                {
                    await emailContracts.CreateTemplateAsync(
                        new CreateEmailTemplateRequest
                        {
                            Name = form.Name,
                            Slug = form.Slug,
                            Subject = form.Subject,
                            Body = form.Body,
                            IsHtml = form.IsHtml,
                        }
                    );
                    return Results.Redirect("/email/templates");
                }
            )
            .RequirePermission(EmailPermissions.ManageTemplates);
    }

    private sealed record CreateTemplateForm(
        [FromForm] string Name,
        [FromForm] string Slug,
        [FromForm] string Subject,
        [FromForm] string Body,
        [FromForm] bool IsHtml
    );
}
