using FluentValidation;
using SimpleModule.Email.Contracts;

namespace SimpleModule.Email.Validators;

public sealed class UpdateEmailTemplateRequestValidator
    : AbstractValidator<UpdateEmailTemplateRequest>
{
    public UpdateEmailTemplateRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .MaximumLength(200)
            .WithMessage("Name must not exceed 200 characters.");
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
}
