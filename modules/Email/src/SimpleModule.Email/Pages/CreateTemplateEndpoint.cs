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

public class CreateTemplateEndpoint : IViewEndpoint
{
    public const string Route = EmailConstants.Routes.CreateTemplatePage;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(Route, () => Inertia.Render("Email/CreateTemplate"))
            .RequirePermission(EmailPermissions.ManageTemplates);

        app.MapPost(
                "/templates",
                async ([AsParameters] CreateTemplateForm form, IEmailContracts emailContracts) =>
                {
                    var request = new CreateEmailTemplateRequest
                    {
                        Name = form.Name,
                        Slug = form.Slug,
                        Subject = form.Subject,
                        Body = form.Body,
                        IsHtml = form.IsHtml,
                    };
                    var validation = CreateEmailTemplateRequestValidator.Validate(request);
                    if (!validation.IsValid)
                        throw new Core.Exceptions.ValidationException(validation.Errors);

                    await emailContracts.CreateTemplateAsync(request);
                    return Results.Redirect(
                        EmailConstants.ViewPrefix + EmailConstants.Routes.Templates
                    );
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
