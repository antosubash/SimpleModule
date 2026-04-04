using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using SimpleModule.Core;
using SimpleModule.Localization.Contracts;

namespace SimpleModule.Localization.Services;

public sealed class TranslationLoader
{
    private static readonly Regex EmbeddedLocalePattern = new(
        @"\.Locales\.([^.]+)\.json$",
        RegexOptions.Compiled
    );

    private readonly ConcurrentDictionary<
        string,
        ConcurrentDictionary<string, string>
    > _mutableTranslations = new();

    private FrozenDictionary<string, FrozenDictionary<string, string>>? _frozenTranslations;
    private IReadOnlyList<string>? _supportedLocales;
    private HashSet<string>? _supportedLocalesSet;

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

                var localeDictionary = _mutableTranslations.GetOrAdd(
                    locale,
                    _ => new ConcurrentDictionary<string, string>()
                );
                var prefix = moduleName;
                foreach (var kvp in flatTranslations)
                {
                    localeDictionary[$"{prefix}.{kvp.Key}"] = kvp.Value;
                }
            }
        }

        Freeze();
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

            var localeDictionary = _mutableTranslations.GetOrAdd(
                locale,
                _ => new ConcurrentDictionary<string, string>()
            );
            foreach (var kvp in flatTranslations)
            {
                localeDictionary[kvp.Key] = kvp.Value;
            }
        }

        Freeze();
    }

    internal void InitializeFromDictionary(
        string locale,
        IReadOnlyDictionary<string, string> translations
    )
    {
        var localeDictionary = _mutableTranslations.GetOrAdd(
            locale,
            _ => new ConcurrentDictionary<string, string>()
        );
        foreach (var kvp in translations)
        {
            localeDictionary[kvp.Key] = kvp.Value;
        }

        Freeze();
    }

    public string? GetTranslation(string key, string locale)
    {
        var frozen = _frozenTranslations;
        if (frozen is not null)
        {
            if (
                frozen.TryGetValue(locale, out var translations)
                && translations.TryGetValue(key, out var value)
            )
            {
                return value;
            }

            if (
                locale != LocalizationConstants.DefaultLocale
                && frozen.TryGetValue(LocalizationConstants.DefaultLocale, out var fallback)
                && fallback.TryGetValue(key, out var fallbackValue)
            )
            {
                return fallbackValue;
            }

            return null;
        }

        if (
            _mutableTranslations.TryGetValue(locale, out var mutableTranslations)
            && mutableTranslations.TryGetValue(key, out var mutableValue)
        )
        {
            return mutableValue;
        }

        if (
            locale != LocalizationConstants.DefaultLocale
            && _mutableTranslations.TryGetValue(
                LocalizationConstants.DefaultLocale,
                out var mutableFallback
            )
            && mutableFallback.TryGetValue(key, out var mutableFallbackValue)
        )
        {
            return mutableFallbackValue;
        }

        return null;
    }

    public IReadOnlyDictionary<string, string> GetAllTranslations(string locale)
    {
        var frozen = _frozenTranslations;
        if (frozen is not null)
        {
            return frozen.TryGetValue(locale, out var translations)
                ? translations
                : FrozenDictionary<string, string>.Empty;
        }

        if (_mutableTranslations.TryGetValue(locale, out var mutableTranslations))
        {
            return new Dictionary<string, string>(mutableTranslations);
        }

        return new Dictionary<string, string>();
    }

    public IReadOnlyList<string> GetSupportedLocales()
    {
        return _supportedLocales ?? _mutableTranslations.Keys.OrderBy(k => k).ToList();
    }

    public IReadOnlySet<string> SupportedLocalesSet =>
        _supportedLocalesSet ?? new HashSet<string>(_mutableTranslations.Keys);

    private void Freeze()
    {
        var builder = new Dictionary<string, FrozenDictionary<string, string>>();
        foreach (var kvp in _mutableTranslations)
        {
            builder[kvp.Key] = kvp.Value.ToFrozenDictionary();
        }

        _frozenTranslations = builder.ToFrozenDictionary();

        var sortedLocales = _mutableTranslations.Keys.OrderBy(k => k).ToList().AsReadOnly();
        _supportedLocales = sortedLocales;
        _supportedLocalesSet = new HashSet<string>(sortedLocales, StringComparer.Ordinal);
    }

    internal static IReadOnlyDictionary<string, string> FlattenJson(string json)
    {
        var result = new Dictionary<string, string>();
        using var document = JsonDocument.Parse(json);
        FlattenElement(document.RootElement, "", result);
        return result;
    }

    private static void FlattenElement(
        JsonElement element,
        string prefix,
        Dictionary<string, string> result
    )
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var key = string.IsNullOrEmpty(prefix)
                        ? property.Name
                        : $"{prefix}.{property.Name}";
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

    internal static string? GetModuleName(Assembly assembly)
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
