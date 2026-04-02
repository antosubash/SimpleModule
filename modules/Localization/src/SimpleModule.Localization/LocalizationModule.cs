using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using SimpleModule.Core;
using SimpleModule.Localization.Contracts;
using SimpleModule.Localization.Services;

namespace SimpleModule.Localization;

[Module(LocalizationConstants.ModuleName, RoutePrefix = LocalizationConstants.RoutePrefix)]
public sealed class LocalizationModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<TranslationLoader>();
        services.AddLocalization();
        services.AddSingleton<IStringLocalizerFactory, JsonStringLocalizerFactory>();
        services.AddScoped<LocalizationService>();
        services.AddScoped<ILocalizationContracts>(sp => sp.GetRequiredService<LocalizationService>());
    }

    public void ConfigureMiddleware(IApplicationBuilder app)
    {
        app.UseMiddleware<Middleware.LocaleResolutionMiddleware>();
    }
}
