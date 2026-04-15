using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Inertia;
using SimpleModule.Core.Validation;
using SimpleModule.Email.Contracts;

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
                async (
                    [AsParameters] CreateTemplateForm form,
                    IValidator<CreateEmailTemplateRequest> validator,
                    IEmailContracts emailContracts
                ) =>
                {
                    var request = new CreateEmailTemplateRequest
                    {
                        Name = form.Name,
                        Slug = form.Slug,
                        Subject = form.Subject,
                        Body = form.Body,
                        IsHtml = form.IsHtml,
                    };
                    var validation = await validator.ValidateAsync(request);
                    if (!validation.IsValid)
                        throw new Core.Exceptions.ValidationException(
                            validation.ToValidationErrors()
                        );

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
