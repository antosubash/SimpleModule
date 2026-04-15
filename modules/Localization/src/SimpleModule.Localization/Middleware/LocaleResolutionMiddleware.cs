using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core.Inertia;
using SimpleModule.Core.Settings;
using SimpleModule.Localization.Contracts;
using SimpleModule.Localization.Services;
using SimpleModule.Settings.Contracts;
using ZiggyCreatures.Caching.Fusion;

namespace SimpleModule.Localization.Middleware;

public sealed class LocaleResolutionMiddleware(
    RequestDelegate next,
    IConfiguration configuration,
    TranslationLoader loader,
    IFusionCache cache
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
            var cachedHit = await cache.TryGetAsync<string>(cacheKey);
            if (cachedHit.HasValue && !string.IsNullOrEmpty(cachedHit.Value))
            {
                return cachedHit.Value;
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
                    await cache.SetAsync(
                        cacheKey,
                        userLocale,
                        options => options.Duration = UserLocaleCacheDuration
                    );
                    return userLocale;
                }
            }
        }

        // No explicit user setting — resolve from Accept-Language header or default
        return await ResolveFromAcceptLanguageAsync(context);
    }

    private async Task<string> ResolveFromAcceptLanguageAsync(HttpContext context)
    {
        var rawHeader = context.Request.Headers.AcceptLanguage.ToString();
        if (string.IsNullOrEmpty(rawHeader))
        {
            return GetDefaultLocale();
        }

        var cacheKey = AcceptLanguageKey(rawHeader);
        var cachedHit = await cache.TryGetAsync<string>(cacheKey);
        if (cachedHit.HasValue && !string.IsNullOrEmpty(cachedHit.Value))
        {
            return cachedHit.Value;
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
                    await cache.SetAsync(
                        cacheKey,
                        tag,
                        options => options.Duration = AcceptLanguageCacheDuration
                    );
                    return tag;
                }

                var twoLetter = tag.Split('-')[0];
                if (supportedLocales.Contains(twoLetter))
                {
                    await cache.SetAsync(
                        cacheKey,
                        twoLetter,
                        options => options.Duration = AcceptLanguageCacheDuration
                    );
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
