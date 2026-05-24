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
            .WithMessage("Setting key must not exceed 256 characters.")
            .Must(k => string.IsNullOrEmpty(k) || SettingKeyPattern().IsMatch(k))
            .WithMessage(
                "Setting key must be lowercase alphanumeric with dots and underscores (e.g. 'app.theme')."
            );

        rules.RuleFor(x => x.Scope).IsInEnum().WithMessage("Invalid setting scope.");
    }

    [GeneratedRegex(@"^[a-z][a-z0-9_.]*$")]
    private static partial Regex SettingKeyPattern();
}
