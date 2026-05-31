using System.Text.Json;
using System.Text.RegularExpressions;
using FluentValidation;
using SimpleModule.Core.FormRequests;

namespace SimpleModule.Settings.FormRequests;

[FormRequest]
public sealed partial class UpdateMySettingFormRequest : FormRequest<UpdateMySettingFormRequest>
{
    public string Key { get; set; } = "";
    public JsonElement Value { get; set; }

    public override void Prepare()
    {
        Key = Key.Trim();
    }

    protected override void ConfigureRules(RuleConfigurator<UpdateMySettingFormRequest> rules)
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
                "Setting key must be alphanumeric segments separated by dots (e.g. 'app.theme')."
            )
            .When(x => !string.IsNullOrEmpty(x.Key));
    }

    [GeneratedRegex(@"^[a-zA-Z][a-zA-Z0-9_]*(\.[a-zA-Z][a-zA-Z0-9_]*)*$")]
    private static partial Regex SettingKeyPattern();
}
