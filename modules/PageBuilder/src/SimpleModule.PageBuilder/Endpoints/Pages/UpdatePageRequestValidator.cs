using FluentValidation;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder.Endpoints.Pages;

public sealed class UpdatePageRequestValidator : AbstractValidator<UpdatePageRequest>
{
    public UpdatePageRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("Page title is required.");
        RuleFor(x => x.Slug).NotEmpty().WithMessage("Page slug is required.");
    }
}
