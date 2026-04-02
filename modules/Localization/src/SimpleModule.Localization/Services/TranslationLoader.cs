using System.Collections.Concurrent;
using System.Text.Json;

namespace SimpleModule.Localization.Services;

public sealed class TranslationLoader
{
    private readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, string>> _translations = new();

    public void LoadFromDirectory(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(directory, "*.json"))
        {
            var locale = Path.GetFileNameWithoutExtension(file);
            var json = File.ReadAllText(file);
            var translations = FlattenJson(json);
            _translations[locale] = translations;
        }
    }

    internal void InitializeFromDictionary(string locale, IReadOnlyDictionary<string, string> translations)
    {
        _translations[locale] = translations;
    }

    public string? GetTranslation(string key, string locale)
    {
        if (_translations.TryGetValue(locale, out var translations) && translations.TryGetValue(key, out var value))
        {
            return value;
        }

        if (locale != "en" && _translations.TryGetValue("en", out var fallback) && fallback.TryGetValue(key, out var fallbackValue))
        {
            return fallbackValue;
        }

        return null;
    }

    public IReadOnlyDictionary<string, string> GetAllTranslations(string locale)
    {
        if (_translations.TryGetValue(locale, out var translations))
        {
            return translations;
        }

        return new Dictionary<string, string>();
    }

    public IReadOnlyList<string> GetSupportedLocales()
    {
        return _translations.Keys.OrderBy(k => k).ToList();
    }

    internal static IReadOnlyDictionary<string, string> FlattenJson(string json)
    {
        var result = new Dictionary<string, string>();
        using var document = JsonDocument.Parse(json);
        FlattenElement(document.RootElement, "", result);
        return result;
    }

    private static void FlattenElement(JsonElement element, string prefix, Dictionary<string, string> result)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";
                    FlattenElement(property.Value, key, result);
                }
                break;
            case JsonValueKind.String:
                result[prefix] = element.GetString()!;
                break;
            default:
                result[prefix] = element.ToString();
                break;
        }
    }
}
