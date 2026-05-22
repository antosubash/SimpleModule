using System.Text.Json;
using System.Text.RegularExpressions;
using SimpleModule.Core.Settings;

namespace SimpleModule.Settings;

internal static class SettingValidator
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled
    );

    private static readonly Regex ColorRegex = new(@"^#[0-9a-fA-F]{6}$", RegexOptions.Compiled);

    internal static List<string> Validate(SettingDefinition definition, JsonElement value)
    {
        var errors = new List<string>();
        var isEmpty =
            value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
            || (value.ValueKind == JsonValueKind.String && string.IsNullOrEmpty(value.GetString()));

        if (isEmpty)
        {
            if (definition.Required)
                errors.Add($"Setting '{definition.Key}' is required.");
            return errors;
        }

        ValidateType(definition, value, errors);
        ValidateRange(definition, value, errors);
        ValidatePattern(definition, value, errors);
        ValidateAllowedValues(definition, value, errors);

        return errors;
    }

    private static void ValidateType(
        SettingDefinition definition,
        JsonElement value,
        List<string> errors
    )
    {
        switch (definition.Type)
        {
            case SettingType.Number:
                if (!TryGetNumber(value, out _))
                    errors.Add(
                        $"Setting '{definition.Key}' expects a number but received {value.ValueKind}."
                    );
                break;

            case SettingType.Bool:
                if (value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                    errors.Add(
                        $"Setting '{definition.Key}' expects a boolean but received {value.ValueKind}."
                    );
                break;

            case SettingType.Email:
                if (
                    value.ValueKind != JsonValueKind.String
                    || !EmailRegex.IsMatch(value.GetString()!)
                )
                    errors.Add($"Setting '{definition.Key}' must be a valid email address.");
                break;

            case SettingType.Color:
                if (
                    value.ValueKind != JsonValueKind.String
                    || !ColorRegex.IsMatch(value.GetString()!)
                )
                    errors.Add($"Setting '{definition.Key}' must be a hex color like #3b82f6.");
                break;

            case SettingType.Url:
                if (
                    value.ValueKind != JsonValueKind.String
                    || !Uri.TryCreate(value.GetString(), UriKind.Absolute, out _)
                )
                    errors.Add($"Setting '{definition.Key}' must be a valid absolute URL.");
                break;

            case SettingType.DateTime:
                if (
                    value.ValueKind != JsonValueKind.String
                    || !DateTimeOffset.TryParse(value.GetString(), out _)
                )
                    errors.Add($"Setting '{definition.Key}' must be a valid ISO 8601 timestamp.");
                break;

            case SettingType.Text:
            case SettingType.Json:
            case SettingType.Select:
            case SettingType.Password:
            case SettingType.MultilineText:
                break;
        }
    }

    private static void ValidateRange(
        SettingDefinition definition,
        JsonElement value,
        List<string> errors
    )
    {
        if (definition.Type != SettingType.Number)
            return;
        if (!TryGetNumber(value, out var num))
            return;

        if (definition.Min is { } min && num < min)
            errors.Add($"Setting '{definition.Key}' must be at least {min}.");
        if (definition.Max is { } max && num > max)
            errors.Add($"Setting '{definition.Key}' must be at most {max}.");
    }

    private static void ValidatePattern(
        SettingDefinition definition,
        JsonElement value,
        List<string> errors
    )
    {
        if (string.IsNullOrEmpty(definition.Pattern))
            return;
        if (value.ValueKind != JsonValueKind.String)
            return;

        try
        {
            if (!Regex.IsMatch(value.GetString()!, definition.Pattern))
                errors.Add($"Setting '{definition.Key}' does not match the required pattern.");
        }
        catch (ArgumentException)
        {
            errors.Add($"Setting '{definition.Key}' has an invalid pattern regex configured.");
        }
    }

    private static void ValidateAllowedValues(
        SettingDefinition definition,
        JsonElement value,
        List<string> errors
    )
    {
        if (definition.AllowedValues is null || definition.AllowedValues.Count == 0)
            return;
        if (value.ValueKind != JsonValueKind.String)
            return;

        var str = value.GetString();
        if (!definition.AllowedValues.Contains(str))
            errors.Add(
                $"Setting '{definition.Key}' must be one of: {string.Join(", ", definition.AllowedValues)}."
            );
    }

    internal static bool TryGetNumber(JsonElement value, out double result)
    {
        if (value.ValueKind == JsonValueKind.Number)
        {
            result = value.GetDouble();
            return true;
        }

        result = 0;
        return false;
    }
}
