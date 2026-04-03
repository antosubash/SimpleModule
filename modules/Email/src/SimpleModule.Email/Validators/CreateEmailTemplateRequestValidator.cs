using System.Text.RegularExpressions;
using SimpleModule.Core.Validation;
using SimpleModule.Email.Contracts;

namespace SimpleModule.Email.Validators;

public static partial class CreateEmailTemplateRequestValidator
{
    public static ValidationResult Validate(CreateEmailTemplateRequest request) =>
        new ValidationBuilder()
            .AddErrorIf(string.IsNullOrWhiteSpace(request.Name), "Name", "Name is required.")
            .AddErrorIf(request.Name?.Length > 200, "Name", "Name must not exceed 200 characters.")
            .AddErrorIf(string.IsNullOrWhiteSpace(request.Slug), "Slug", "Slug is required.")
            .AddErrorIf(request.Slug?.Length > 200, "Slug", "Slug must not exceed 200 characters.")
            .AddErrorIf(
                !string.IsNullOrWhiteSpace(request.Slug) && !SlugPattern().IsMatch(request.Slug),
                "Slug",
                "Slug must be lowercase alphanumeric with hyphens."
            )
            .AddErrorIf(
                string.IsNullOrWhiteSpace(request.Subject),
                "Subject",
                "Subject is required."
            )
            .AddErrorIf(
                request.Subject?.Length > 500,
                "Subject",
                "Subject must not exceed 500 characters."
            )
            .AddErrorIf(string.IsNullOrWhiteSpace(request.Body), "Body", "Body is required.")
            .AddErrorIf(
                !string.IsNullOrWhiteSpace(request.DefaultReplyTo)
                    && !SendEmailRequestValidator.EmailPattern().IsMatch(request.DefaultReplyTo),
                "DefaultReplyTo",
                "Invalid email format."
            )
            .Build();

    [GeneratedRegex(@"^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex SlugPattern();
}
