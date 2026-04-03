using System.Text.RegularExpressions;
using SimpleModule.Core.Validation;
using SimpleModule.Email.Contracts;

namespace SimpleModule.Email.Validators;

public static partial class SendEmailRequestValidator
{
    public static ValidationResult Validate(SendEmailRequest request) =>
        new ValidationBuilder()
            .AddErrorIf(string.IsNullOrWhiteSpace(request.To), "To", "Recipient is required.")
            .AddErrorIf(
                !string.IsNullOrWhiteSpace(request.To) && !EmailPattern().IsMatch(request.To),
                "To",
                "Invalid email format."
            )
            .AddErrorIf(
                !string.IsNullOrWhiteSpace(request.ReplyTo)
                    && !EmailPattern().IsMatch(request.ReplyTo),
                "ReplyTo",
                "Invalid email format."
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
            .Build();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    internal static partial Regex EmailPattern();
}
