using System.Globalization;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core.Inertia;
using SimpleModule.Core.Settings;
using SimpleModule.Localization.Middleware;
using SimpleModule.Localization.Services;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.Localization.Tests.Unit;

public sealed class LocaleResolutionMiddlewareTests
{
    private readonly TranslationLoader _loader;

    public LocaleResolutionMiddlewareTests()
    {
        _loader = new TranslationLoader();
        _loader.InitializeFromDictionary("en", new Dictionary<string, string>
        {
            ["common.save"] = "Save",
        });
        _loader.InitializeFromDictionary("es", new Dictionary<string, string>
        {
            ["common.save"] = "Guardar",
        });
    }

    [Fact]
    public async Task Invoke_AuthenticatedUserWithLanguageSetting_UsesUserLocale()
    {
        var settings = new FakeSettingsContracts("es");
        var context = CreateHttpContext(settings, userId: "user-1");
        string? capturedLocale = null;

        var middleware = new LocaleResolutionMiddleware(
            _ =>
            {
                capturedLocale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                return Task.CompletedTask;
            },
            CreateConfiguration(null),
            _loader
        );

        await middleware.InvokeAsync(context);

        capturedLocale.Should().Be("es");

        var sharedData = context.RequestServices.GetRequiredService<InertiaSharedData>();
        sharedData.Get<string>("locale").Should().Be("es");
        sharedData.Get<IReadOnlyDictionary<string, string>>("translations").Should().NotBeNull();
    }

    [Fact]
    public async Task Invoke_AnonymousWithAcceptLanguageHeader_UsesHeaderLocale()
    {
        var settings = new FakeSettingsContracts(null);
        var context = CreateHttpContext(settings);
        context.Request.Headers.AcceptLanguage = "es-ES,es;q=0.9,en;q=0.8";
        string? capturedLocale = null;

        var middleware = new LocaleResolutionMiddleware(
            _ =>
            {
                capturedLocale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                return Task.CompletedTask;
            },
            CreateConfiguration(null),
            _loader
        );

        await middleware.InvokeAsync(context);

        capturedLocale.Should().Be("es");
    }

    [Fact]
    public async Task Invoke_NoHeaderNoSetting_UsesConfigDefault()
    {
        var settings = new FakeSettingsContracts(null);
        var context = CreateHttpContext(settings);
        string? capturedLocale = null;

        var middleware = new LocaleResolutionMiddleware(
            _ =>
            {
                capturedLocale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                return Task.CompletedTask;
            },
            CreateConfiguration("es"),
            _loader
        );

        await middleware.InvokeAsync(context);

        capturedLocale.Should().Be("es");
    }

    [Fact]
    public async Task Invoke_NoHeaderNoSettingNoConfig_FallsBackToEn()
    {
        var settings = new FakeSettingsContracts(null);
        var context = CreateHttpContext(settings);
        string? capturedLocale = null;

        var middleware = new LocaleResolutionMiddleware(
            _ =>
            {
                capturedLocale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                return Task.CompletedTask;
            },
            CreateConfiguration(null),
            _loader
        );

        await middleware.InvokeAsync(context);

        capturedLocale.Should().Be("en");
    }

    private static DefaultHttpContext CreateHttpContext(
        FakeSettingsContracts settings,
        string? userId = null)
    {
        var services = new ServiceCollection();
        services.AddScoped<InertiaSharedData>();
        services.AddSingleton<ISettingsContracts>(settings);

        var context = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
        };

        if (userId is not null)
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId),
            ], "TestAuth"));
        }

        return context;
    }

    private static IConfiguration CreateConfiguration(string? defaultLocale)
    {
        var configData = new Dictionary<string, string?>();
        if (defaultLocale is not null)
        {
            configData["Localization:DefaultLocale"] = defaultLocale;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }

    private sealed class FakeSettingsContracts(string? language) : ISettingsContracts
    {
        public Task<string?> GetSettingAsync(string key, SettingScope scope, string? userId = null)
        {
            return Task.FromResult(language);
        }

        public Task<T?> GetSettingAsync<T>(string key, SettingScope scope, string? userId = null)
        {
            if (typeof(T) == typeof(string))
            {
                return Task.FromResult((T?)(object?)language);
            }

            return Task.FromResult(default(T));
        }

        public Task<string?> ResolveUserSettingAsync(string key, string userId)
        {
            return Task.FromResult(language);
        }

        public Task SetSettingAsync(string key, string value, SettingScope scope, string? userId = null)
        {
            return Task.CompletedTask;
        }

        public Task DeleteSettingAsync(string key, SettingScope scope, string? userId = null)
        {
            return Task.CompletedTask;
        }

        public Task<IEnumerable<Setting>> GetSettingsAsync(SettingsFilter? filter = null)
        {
            return Task.FromResult<IEnumerable<Setting>>([]);
        }
    }
}
