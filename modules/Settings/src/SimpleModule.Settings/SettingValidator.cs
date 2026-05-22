using System.Text.Json;
using SimpleModule.Core.Settings;

namespace SimpleModule.Settings;

internal static class SettingValidator
{
    internal static List<string> Validate(SettingDefinition definition, JsonElement value)
    {
        var errors = new List<string>();

        if (value.ValueKind == JsonValueKind.Null || value.ValueKind == JsonValueKind.Undefined)
        {
            return errors;
        }

        switch (definition.Type)
        {
            case SettingType.Number:
                if (!TryGetNumber(value, out _))
                    errors.Add(
                        $"Setting '{definition.Key}' expects a number but received {value.ValueKind}."
                    );
                break;

            case SettingType.Bool:
                if (value.ValueKind != JsonValueKind.True && value.ValueKind != JsonValueKind.False)
                    errors.Add(
                        $"Setting '{definition.Key}' expects a boolean but received {value.ValueKind}."
                    );
                break;

            case SettingType.Text:
            case SettingType.Json:
                break;
        }

        return errors;
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
