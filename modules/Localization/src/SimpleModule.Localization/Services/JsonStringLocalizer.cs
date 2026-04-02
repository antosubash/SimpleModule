using System.Globalization;
using Microsoft.Extensions.Localization;

namespace SimpleModule.Localization.Services;

public sealed class JsonStringLocalizer(string moduleNamespace, TranslationLoader loader) : IStringLocalizer
{
    private readonly string _moduleNamespace = moduleNamespace;
    private readonly TranslationLoader _loader = loader;

    public LocalizedString this[string name]
    {
        get
        {
            var fullKey = $"{_moduleNamespace}.{name}";
            var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var value = _loader.GetTranslation(fullKey, locale);

            return value is not null
                ? new LocalizedString(name, value, resourceNotFound: false)
                : new LocalizedString(name, name, resourceNotFound: true);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var fullKey = $"{_moduleNamespace}.{name}";
            var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var value = _loader.GetTranslation(fullKey, locale);

            if (value is null)
            {
                return new LocalizedString(name, name, resourceNotFound: true);
            }

            try
            {
                var formatted = string.Format(CultureInfo.CurrentCulture, value, arguments);
                return new LocalizedString(name, formatted, resourceNotFound: false);
            }
            catch (FormatException)
            {
                return new LocalizedString(name, value, resourceNotFound: false);
            }
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var allTranslations = _loader.GetAllTranslations(locale);
        var prefix = $"{_moduleNamespace}.";

        foreach (var kvp in allTranslations)
        {
            if (kvp.Key.StartsWith(prefix, StringComparison.Ordinal))
            {
                var bareKey = kvp.Key[prefix.Length..];
                yield return new LocalizedString(bareKey, kvp.Value, resourceNotFound: false);
            }
        }
    }
}
