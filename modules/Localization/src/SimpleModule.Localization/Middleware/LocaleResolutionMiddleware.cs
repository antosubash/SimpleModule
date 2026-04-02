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

        var culture = new CultureInfo(locale);
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

        var acceptLanguage = context.Request.Headers.AcceptLanguage.ToString();
        if (!string.IsNullOrEmpty(acceptLanguage))
        {
            var parsed = acceptLanguage.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (parsed.Length > 0)
            {
                var primary = parsed[0].Split(';', StringSplitOptions.RemoveEmptyEntries)[0].Trim();
                if (primary.Length >= 2)
                {
                    return primary[..2];
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
