using FluentValidation;
using SimpleModule.Core.FormRequests;

namespace SimpleModule.Settings.FormRequests;

[FormRequest]
public sealed class UpdateMenuItemFormRequest : FormRequest<UpdateMenuItemFormRequest>
{
    public string Label { get; set; } = "";

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1056:URI-like properties should not be strings"
    )]
    public string? Url { get; set; }
    public string? PageRoute { get; set; }
    public string Icon { get; set; } = "";
    public string? CssClass { get; set; }
    public bool OpenInNewTab { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool IsHomePage { get; set; }

    public override void Prepare()
    {
        Label = Label.Trim();
        Url = Url?.Trim();
        PageRoute = PageRoute?.Trim();
        Icon = Icon.Trim();
        CssClass = CssClass?.Trim();
    }

    protected override void ConfigureRules(RuleConfigurator<UpdateMenuItemFormRequest> rules)
    {
        rules
            .RuleFor(x => x.Label)
            .NotEmpty()
            .WithMessage("Label is required.")
            .MaximumLength(200)
            .WithMessage("Label must not exceed 200 characters.");

        rules
            .RuleFor(x => x.Url)
            .MaximumLength(2000)
            .WithMessage("URL must not exceed 2000 characters.");

        rules
            .RuleFor(x => x.PageRoute)
            .MaximumLength(500)
            .WithMessage("Page route must not exceed 500 characters.");

        rules
            .RuleFor(x => x.Icon)
            .MaximumLength(1000)
            .WithMessage("Icon must not exceed 1000 characters.");
    }
}
