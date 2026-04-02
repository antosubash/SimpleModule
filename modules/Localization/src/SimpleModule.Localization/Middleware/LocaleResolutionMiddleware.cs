using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core.Inertia;
using SimpleModule.Core.Settings;
using SimpleModule.Localization.Services;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.Localization.Middleware;

public sealed class LocaleResolutionMiddleware(
    RequestDelegate next,
    IConfiguration configuration,
    TranslationLoader loader)
{
    private readonly RequestDelegate _next = next;
    private readonly IConfiguration _configuration = configuration;
    private readonly TranslationLoader _loader = loader;

    public async Task InvokeAsync(HttpContext context)
    {
        var locale = await ResolveLocaleAsync(context);

        CultureInfo culture;
        try
        {
            culture = new CultureInfo(locale);
        }
        catch (CultureNotFoundException)
        {
            locale = _configuration["Localization:DefaultLocale"] ?? "en";
            culture = new CultureInfo(locale);
        }

        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        var sharedData = context.RequestServices.GetService<InertiaSharedData>();
        if (sharedData is not null)
        {
            sharedData.Set("locale", locale);
            sharedData.Set("translations", _loader.GetAllTranslations(locale));
        }

        await _next(context);
    }

    private async Task<string> ResolveLocaleAsync(HttpContext context)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is not null)
        {
            var settings = context.RequestServices.GetService<ISettingsContracts>();
            if (settings is not null)
            {
                var userLocale = await settings.GetSettingAsync<string>("app.language", SettingScope.User, userId);
                if (!string.IsNullOrEmpty(userLocale))
                {
                    return userLocale;
                }
            }
        }

        var acceptLanguageHeaders = context.Request.GetTypedHeaders().AcceptLanguage;
        if (acceptLanguageHeaders is { Count: > 0 })
        {
            var supportedLocales = _loader.GetSupportedLocales();
            foreach (var lang in acceptLanguageHeaders.OrderByDescending(l => l.Quality ?? 1.0))
            {
                var tag = lang.Value.ToString();

                // Try exact match first (e.g., "en-US")
                if (supportedLocales.Contains(tag))
                {
                    return tag;
                }

                // Try two-letter prefix (e.g., "en-US" → "en")
                var twoLetter = tag.Split('-')[0];
                if (supportedLocales.Contains(twoLetter))
                {
                    return twoLetter;
                }
            }
        }

        var configDefault = _configuration["Localization:DefaultLocale"];
        if (!string.IsNullOrEmpty(configDefault))
        {
            return configDefault;
        }

        return "en";
    }
}
