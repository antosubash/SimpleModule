using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using SimpleModule.Core;

namespace SimpleModule.Localization.Services;

public sealed class TranslationLoader
{
    private static readonly Regex EmbeddedLocalePattern = new(@"\.Locales\.([^.]+)\.json$", RegexOptions.Compiled);

    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _translations = new();

    public void Initialize(Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            var moduleName = GetModuleName(assembly);
            if (moduleName is null)
            {
                continue;
            }

            var resourceNames = assembly.GetManifestResourceNames();
            foreach (var resourceName in resourceNames)
            {
                var match = EmbeddedLocalePattern.Match(resourceName);
                if (!match.Success)
                {
                    continue;
                }

                var locale = match.Groups[1].Value;
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream is null)
                {
                    continue;
                }

                using var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();
                var flatTranslations = FlattenJson(json);

                var localeDictionary = _translations.GetOrAdd(locale, _ => new ConcurrentDictionary<string, string>());
                var prefix = moduleName;
                foreach (var kvp in flatTranslations)
                {
                    localeDictionary[$"{prefix}.{kvp.Key}"] = kvp.Value;
                }
            }
        }
    }

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
            var flatTranslations = FlattenJson(json);

            var localeDictionary = _translations.GetOrAdd(locale, _ => new ConcurrentDictionary<string, string>());
            foreach (var kvp in flatTranslations)
            {
                localeDictionary[kvp.Key] = kvp.Value;
            }
        }
    }

    internal void InitializeFromDictionary(string locale, IReadOnlyDictionary<string, string> translations)
    {
        var localeDictionary = _translations.GetOrAdd(locale, _ => new ConcurrentDictionary<string, string>());
        foreach (var kvp in translations)
        {
            localeDictionary[kvp.Key] = kvp.Value;
        }
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
            return new Dictionary<string, string>(translations);
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

    private static string? GetModuleName(Assembly assembly)
    {
        try
        {
            foreach (var type in assembly.GetTypes())
            {
                var attr = type.GetCustomAttribute<ModuleAttribute>();
                if (attr is not null)
                {
                    return attr.Name;
                }
            }
        }
        catch (ReflectionTypeLoadException ex)
        {
            foreach (var type in ex.Types)
            {
                if (type is null)
                {
                    continue;
                }

                var attr = type.GetCustomAttribute<ModuleAttribute>();
                if (attr is not null)
                {
                    return attr.Name;
                }
            }
        }

        return null;
    }
}
