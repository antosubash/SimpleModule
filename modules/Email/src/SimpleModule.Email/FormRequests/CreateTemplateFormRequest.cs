using System.Security.Claims;
using System.Text.RegularExpressions;
using FluentValidation;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Extensions;
using SimpleModule.Core.FormRequests;

namespace SimpleModule.Email.FormRequests;

[FormRequest]
public sealed partial class CreateTemplateFormRequest : FormRequest<CreateTemplateFormRequest>
{
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Body { get; set; } = "";
    public bool IsHtml { get; set; } = true;
    public string? DefaultReplyTo { get; set; }

    public override bool Authorize(ClaimsPrincipal user) =>
        user.HasPermission(EmailPermissions.ManageTemplates);

    public override void Prepare()
    {
        Name = Name.Trim();
#pragma warning disable CA1308 // Slugs are conventionally lowercase
        Slug = Slug.Trim().ToLowerInvariant();
#pragma warning restore CA1308
        Subject = Subject.Trim();
    }

    protected override void ConfigureRules(RuleConfigurator<CreateTemplateFormRequest> rules)
    {
        rules
            .RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .MaximumLength(200)
            .WithMessage("Name must not exceed 200 characters.");
        rules
            .RuleFor(x => x.Slug)
            .NotEmpty()
            .WithMessage("Slug is required.")
            .MaximumLength(200)
            .WithMessage("Slug must not exceed 200 characters.")
            .Must(s => string.IsNullOrWhiteSpace(s) || SlugPattern().IsMatch(s))
            .WithMessage("Slug must be lowercase alphanumeric with hyphens.");
        rules
            .RuleFor(x => x.Subject)
            .NotEmpty()
            .WithMessage("Subject is required.")
            .MaximumLength(500)
            .WithMessage("Subject must not exceed 500 characters.");
        rules.RuleFor(x => x.Body).NotEmpty().WithMessage("Body is required.");
        rules
            .RuleFor(x => x.DefaultReplyTo)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.DefaultReplyTo))
            .WithMessage("Invalid email format.");
    }

    [GeneratedRegex(@"^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex SlugPattern();
}
