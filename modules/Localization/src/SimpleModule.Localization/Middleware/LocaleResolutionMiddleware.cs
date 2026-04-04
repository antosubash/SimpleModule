using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core.Inertia;
using SimpleModule.Core.Settings;
using SimpleModule.Localization.Contracts;
using SimpleModule.Localization.Services;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.Localization.Middleware;

public sealed class LocaleResolutionMiddleware(
    RequestDelegate next,
    IConfiguration configuration,
    TranslationLoader loader,
    IMemoryCache cache
)
{
    private static readonly TimeSpan UserLocaleCacheDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan AcceptLanguageCacheDuration = TimeSpan.FromMinutes(30);

    public async Task InvokeAsync(HttpContext context)
    {
        var locale = await ResolveLocaleAsync(context);

        CultureInfo culture;
        try
        {
            culture = CultureInfo.GetCultureInfo(locale);
        }
        catch (CultureNotFoundException)
        {
            locale = GetDefaultLocale();
            culture = CultureInfo.GetCultureInfo(locale);
        }

        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        var sharedData = context.RequestServices.GetService<InertiaSharedData>();
        if (sharedData is not null)
        {
            sharedData.Set(LocalizationConstants.LocaleSharedDataKey, locale);
            sharedData.Set(
                LocalizationConstants.TranslationsSharedDataKey,
                loader.GetAllTranslations(locale)
            );
        }

        await next(context);
    }

    private async Task<string> ResolveLocaleAsync(HttpContext context)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is not null)
        {
            // Only cache the user's explicit DB setting — not Accept-Language fallbacks.
            // This avoids cross-browser cache pollution where one browser's Accept-Language
            // leaks to another browser for the same user.
            var cacheKey = UserLocaleKey(userId);
            if (cache.TryGetValue(cacheKey, out string? cachedLocale) && cachedLocale is not null)
            {
                return cachedLocale;
            }

            var settings = context.RequestServices.GetService<ISettingsContracts>();
            if (settings is not null)
            {
                var userLocale = await settings.GetSettingAsync<string>(
                    LocalizationConstants.UserLanguageSetting,
                    SettingScope.User,
                    userId
                );
                if (!string.IsNullOrEmpty(userLocale))
                {
                    cache.Set(cacheKey, userLocale, UserLocaleCacheDuration);
                    return userLocale;
                }
            }
        }

        // No explicit user setting — resolve from Accept-Language header or default
        return ResolveFromAcceptLanguage(context);
    }

    private string ResolveFromAcceptLanguage(HttpContext context)
    {
        var rawHeader = context.Request.Headers.AcceptLanguage.ToString();
        if (string.IsNullOrEmpty(rawHeader))
        {
            return GetDefaultLocale();
        }

        var cacheKey = AcceptLanguageKey(rawHeader);
        if (cache.TryGetValue(cacheKey, out string? cached) && cached is not null)
        {
            return cached;
        }

        var acceptLanguageHeaders = context.Request.GetTypedHeaders().AcceptLanguage;
        if (acceptLanguageHeaders is { Count: > 0 })
        {
            var supportedLocales = loader.SupportedLocalesSet;
            foreach (var lang in acceptLanguageHeaders.OrderByDescending(l => l.Quality ?? 1.0))
            {
                var tag = lang.Value.ToString();

                if (supportedLocales.Contains(tag))
                {
                    cache.Set(cacheKey, tag, AcceptLanguageCacheDuration);
                    return tag;
                }

                var twoLetter = tag.Split('-')[0];
                if (supportedLocales.Contains(twoLetter))
                {
                    cache.Set(cacheKey, twoLetter, AcceptLanguageCacheDuration);
                    return twoLetter;
                }
            }
        }

        return GetDefaultLocale();
    }

    private string GetDefaultLocale()
    {
        return configuration["Localization:DefaultLocale"] ?? LocalizationConstants.DefaultLocale;
    }

    private static string UserLocaleKey(string userId) => string.Concat("locale:user:", userId);

    private static string AcceptLanguageKey(string headerValue) =>
        string.Concat("locale:accept:", headerValue);
}
