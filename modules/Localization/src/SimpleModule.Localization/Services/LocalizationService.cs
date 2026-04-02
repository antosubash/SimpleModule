using SimpleModule.Localization.Contracts;

namespace SimpleModule.Localization.Services;

public sealed class LocalizationService(TranslationLoader loader) : ILocalizationContracts
{
    private readonly TranslationLoader _loader = loader;

    public string? GetTranslation(string key, string locale)
    {
        return _loader.GetTranslation(key, locale);
    }

    public IReadOnlyDictionary<string, string> GetAllTranslations(string locale)
    {
        return _loader.GetAllTranslations(locale);
    }

    public IReadOnlyList<string> GetSupportedLocales()
    {
        return _loader.GetSupportedLocales();
    }
}
