using FluentValidation;
using SimpleModule.Email.Contracts;

namespace SimpleModule.Email.Validators;

public sealed class SendEmailRequestValidator : AbstractValidator<SendEmailRequest>
{
    public SendEmailRequestValidator()
    {
        RuleFor(x => x.To).NotEmpty().WithMessage("Recipient is required.");
        RuleFor(x => x.To)
            .EmailAddress()
            .WithMessage("Invalid email format.")
            .When(x => !string.IsNullOrWhiteSpace(x.To));
        RuleFor(x => x.ReplyTo)
            .EmailAddress()
            .WithMessage("Invalid email format.")
            .When(x => !string.IsNullOrWhiteSpace(x.ReplyTo));
        RuleFor(x => x.Subject)
            .NotEmpty()
            .WithMessage("Subject is required.")
            .MaximumLength(500)
            .WithMessage("Subject must not exceed 500 characters.");
        RuleFor(x => x.Body).NotEmpty().WithMessage("Body is required.");
    }
}
