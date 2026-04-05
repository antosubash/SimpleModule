using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Inertia;
using SimpleModule.Email.Contracts;
using SimpleModule.Email.Validators;

namespace SimpleModule.Email.Pages;

public class EditTemplateEndpoint : IViewEndpoint
{
    public const string Route = EmailConstants.Routes.EditTemplatePage;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                Route,
                async (int id, IEmailContracts emailContracts) =>
                {
                    var template = await emailContracts.GetTemplateByIdAsync(
                        EmailTemplateId.From(id)
                    );
                    return template is null
                        ? Results.NotFound()
                        : Inertia.Render("Email/EditTemplate", new { template });
                }
            )
            .RequirePermission(EmailPermissions.ManageTemplates);

        app.MapPost(
                "/templates/{id}",
                async (
                    int id,
                    [AsParameters] UpdateTemplateForm form,
                    IEmailContracts emailContracts
                ) =>
                {
                    var request = new UpdateEmailTemplateRequest
                    {
                        Name = form.Name,
                        Subject = form.Subject,
                        Body = form.Body,
                        IsHtml = form.IsHtml,
                    };
                    var validation = UpdateEmailTemplateRequestValidator.Validate(request);
                    if (!validation.IsValid)
                        throw new Core.Exceptions.ValidationException(validation.Errors);

                    await emailContracts.UpdateTemplateAsync(EmailTemplateId.From(id), request);
                    return Results.Redirect("/email/templates");
                }
            )
            .RequirePermission(EmailPermissions.ManageTemplates);

        app.MapDelete(
                "/templates/{id}",
                async (int id, IEmailContracts emailContracts) =>
                {
                    await emailContracts.DeleteTemplateAsync(EmailTemplateId.From(id));
                    return Results.Redirect("/email/templates");
                }
            )
            .RequirePermission(EmailPermissions.ManageTemplates);
    }

    private sealed record UpdateTemplateForm(
        [FromForm] string Name,
        [FromForm] string Subject,
        [FromForm] string Body,
        [FromForm] bool IsHtml
    );
}
