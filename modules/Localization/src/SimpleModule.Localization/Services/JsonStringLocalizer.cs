using System.Globalization;
using Microsoft.Extensions.Localization;

namespace SimpleModule.Localization.Services;

public sealed class JsonStringLocalizer(string moduleNamespace, TranslationLoader loader)
    : IStringLocalizer
{
    public LocalizedString this[string name]
    {
        get
        {
            var (value, notFound) = Resolve(name);
            return new LocalizedString(name, value ?? name, resourceNotFound: notFound);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var (value, notFound) = Resolve(name);

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
        var allTranslations = loader.GetAllTranslations(locale);
        var prefix = $"{moduleNamespace}.";

        foreach (var kvp in allTranslations)
        {
            if (kvp.Key.StartsWith(prefix, StringComparison.Ordinal))
            {
                var bareKey = kvp.Key[prefix.Length..];
                yield return new LocalizedString(bareKey, kvp.Value, resourceNotFound: false);
            }
        }
    }

    private (string? value, bool notFound) Resolve(string name)
    {
        var fullKey = $"{moduleNamespace}.{name}";
        var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var value = loader.GetTranslation(fullKey, locale);
        return (value, value is null);
    }
}
