namespace SimpleModule.Localization.Contracts;

public interface ILocalizationContracts
{
    string? GetTranslation(string key, string locale);
    IReadOnlyDictionary<string, string> GetAllTranslations(string locale);
    IReadOnlyList<string> GetSupportedLocales();
}
