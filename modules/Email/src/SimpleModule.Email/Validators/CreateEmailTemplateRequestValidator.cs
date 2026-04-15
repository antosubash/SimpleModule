using System.Text.RegularExpressions;
using FluentValidation;
using SimpleModule.Email.Contracts;

namespace SimpleModule.Email.Validators;

public sealed partial class CreateEmailTemplateRequestValidator
    : AbstractValidator<CreateEmailTemplateRequest>
{
    public CreateEmailTemplateRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .MaximumLength(200)
            .WithMessage("Name must not exceed 200 characters.");
        RuleFor(x => x.Slug)
            .NotEmpty()
            .WithMessage("Slug is required.")
            .MaximumLength(200)
            .WithMessage("Slug must not exceed 200 characters.")
            .Must(s => string.IsNullOrWhiteSpace(s) || SlugPattern().IsMatch(s))
            .WithMessage("Slug must be lowercase alphanumeric with hyphens.");
        RuleFor(x => x.Subject)
            .NotEmpty()
            .WithMessage("Subject is required.")
            .MaximumLength(500)
            .WithMessage("Subject must not exceed 500 characters.");
        RuleFor(x => x.Body).NotEmpty().WithMessage("Body is required.");
        RuleFor(x => x.DefaultReplyTo)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.DefaultReplyTo))
            .WithMessage("Invalid email format.");
    }

    [GeneratedRegex(@"^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex SlugPattern();
}
