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
    IMemoryCache cache)
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task InvokeAsync(HttpContext context)
    {
        var locale = await ResolveLocaleAsync(context);

        var culture = GetOrCreateCulture(locale);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        var sharedData = context.RequestServices.GetService<InertiaSharedData>();
        if (sharedData is not null)
        {
            sharedData.Set(LocalizationConstants.LocaleSharedDataKey, locale);
            sharedData.Set(LocalizationConstants.TranslationsSharedDataKey, loader.GetAllTranslations(locale));
        }

        await next(context);
    }

    private async Task<string> ResolveLocaleAsync(HttpContext context)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is not null)
        {
            var cacheKey = $"locale:user:{userId}";
            if (cache.TryGetValue(cacheKey, out string? cachedLocale) && cachedLocale is not null)
            {
                return cachedLocale;
            }

            // Resolve and cache the result — even if the user has no explicit setting,
            // cache the resolved fallback so we don't hit the DB again for this user.
            var resolved = await ResolveAuthenticatedUserLocaleAsync(context, userId);
            cache.Set(cacheKey, resolved, CacheDuration);
            return resolved;
        }

        return ResolveAnonymousLocale(context);
    }

    private async Task<string> ResolveAuthenticatedUserLocaleAsync(HttpContext context, string userId)
    {
        var settings = context.RequestServices.GetService<ISettingsContracts>();
        if (settings is not null)
        {
            var userLocale = await settings.GetSettingAsync<string>(
                LocalizationConstants.UserLanguageSetting, SettingScope.User, userId);
            if (!string.IsNullOrEmpty(userLocale))
            {
                return userLocale;
            }
        }

        // User has no explicit setting — fall through to Accept-Language / default
        return ResolveAnonymousLocale(context);
    }

    private string ResolveAnonymousLocale(HttpContext context)
    {
        var acceptLanguageHeaders = context.Request.GetTypedHeaders().AcceptLanguage;
        if (acceptLanguageHeaders is { Count: > 0 })
        {
            var supportedLocales = loader.SupportedLocalesSet;
            foreach (var lang in acceptLanguageHeaders.OrderByDescending(l => l.Quality ?? 1.0))
            {
                var tag = lang.Value.ToString();

                if (supportedLocales.Contains(tag))
                {
                    return tag;
                }

                var twoLetter = tag.Split('-')[0];
                if (supportedLocales.Contains(twoLetter))
                {
                    return twoLetter;
                }
            }
        }

        var configDefault = configuration["Localization:DefaultLocale"];
        if (!string.IsNullOrEmpty(configDefault))
        {
            return configDefault;
        }

        return LocalizationConstants.DefaultLocale;
    }

    private CultureInfo GetOrCreateCulture(string locale)
    {
        var cultureKey = $"culture:{locale}";
        if (cache.TryGetValue(cultureKey, out CultureInfo? cached) && cached is not null)
        {
            return cached;
        }

        CultureInfo culture;
        try
        {
            culture = new CultureInfo(locale);
        }
        catch (CultureNotFoundException)
        {
            culture = new CultureInfo(
                configuration["Localization:DefaultLocale"] ?? LocalizationConstants.DefaultLocale);
        }

        cache.Set(cultureKey, culture, TimeSpan.FromHours(1));
        return culture;
    }
}
