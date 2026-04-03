using SimpleModule.Core.Validation;
using SimpleModule.Email.Contracts;

namespace SimpleModule.Email.Validators;

public static class UpdateEmailTemplateRequestValidator
{
    public static ValidationResult Validate(UpdateEmailTemplateRequest request) =>
        new ValidationBuilder()
            .AddErrorIf(string.IsNullOrWhiteSpace(request.Name), "Name", "Name is required.")
            .AddErrorIf(request.Name?.Length > 200, "Name", "Name must not exceed 200 characters.")
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
}
