using System.Text.Json;
using System.Text.RegularExpressions;
using FluentValidation;
using SimpleModule.Core.FormRequests;
using SimpleModule.Core.Settings;

namespace SimpleModule.Settings.FormRequests;

[FormRequest]
public sealed partial class UpdateSettingFormRequest : FormRequest<UpdateSettingFormRequest>
{
    public string Key { get; set; } = "";
    public JsonElement Value { get; set; }
    public SettingScope Scope { get; set; }

    public override void Prepare()
    {
        Key = Key.Trim();
    }

    protected override void ConfigureRules(RuleConfigurator<UpdateSettingFormRequest> rules)
    {
        rules
            .RuleFor(x => x.Key)
            .NotEmpty()
            .WithMessage("Setting key is required.")
            .MaximumLength(256)
            .WithMessage("Setting key must not exceed 256 characters.");

        rules
            .RuleFor(x => x.Key)
            .Must(k => SettingKeyPattern().IsMatch(k))
            .WithMessage(
                "Setting key must be alphanumeric segments separated by dots (e.g. 'app.theme', 'email.defaultFromAddress')."
            )
            .When(x => !string.IsNullOrEmpty(x.Key));

        rules.RuleFor(x => x.Scope).IsInEnum().WithMessage("Invalid setting scope.");
    }

    [GeneratedRegex(@"^[a-zA-Z][a-zA-Z0-9_]*(\.[a-zA-Z][a-zA-Z0-9_]*)*$")]
    private static partial Regex SettingKeyPattern();
}
