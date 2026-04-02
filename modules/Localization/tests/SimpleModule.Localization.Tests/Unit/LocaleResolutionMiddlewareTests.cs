using System.Globalization;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core.Inertia;
using SimpleModule.Core.Settings;
using SimpleModule.Localization.Middleware;
using SimpleModule.Localization.Services;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.Localization.Tests.Unit;

public sealed class LocaleResolutionMiddlewareTests : IDisposable
{
    private readonly TranslationLoader _loader;
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());

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

        var middleware = CreateMiddleware(
            CaptureLocale(v => capturedLocale = v),
            CreateConfiguration(null),
            _cache);

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

        var middleware = CreateMiddleware(
            CaptureLocale(v => capturedLocale = v),
            CreateConfiguration(null),
            _cache);

        await middleware.InvokeAsync(context);

        capturedLocale.Should().Be("es");
    }

    [Fact]
    public async Task Invoke_NoHeaderNoSetting_UsesConfigDefault()
    {
        var settings = new FakeSettingsContracts(null);
        var context = CreateHttpContext(settings);
        string? capturedLocale = null;

        var middleware = CreateMiddleware(
            CaptureLocale(v => capturedLocale = v),
            CreateConfiguration("es"),
            _cache);

        await middleware.InvokeAsync(context);

        capturedLocale.Should().Be("es");
    }

    [Fact]
    public async Task Invoke_NoHeaderNoSettingNoConfig_FallsBackToEn()
    {
        var settings = new FakeSettingsContracts(null);
        var context = CreateHttpContext(settings);
        string? capturedLocale = null;

        var middleware = CreateMiddleware(
            CaptureLocale(v => capturedLocale = v),
            CreateConfiguration(null),
            _cache);

        await middleware.InvokeAsync(context);

        capturedLocale.Should().Be("en");
    }

    [Fact]
    public async Task Invoke_CachesExplicitUserSetting()
    {
        var callCount = 0;
        var settings = new FakeSettingsContracts("es", onGet: () => callCount++);
        using var localCache = new MemoryCache(new MemoryCacheOptions());

        var middleware = CreateMiddleware(
            _ => Task.CompletedTask,
            CreateConfiguration(null),
            localCache);

        var context1 = CreateHttpContext(settings, userId: "user-1");
        await middleware.InvokeAsync(context1);
        callCount.Should().Be(1);

        // Second request uses cache — no DB call
        var context2 = CreateHttpContext(settings, userId: "user-1");
        await middleware.InvokeAsync(context2);
        callCount.Should().Be(1, "explicit user setting should be served from cache");
    }

    [Fact]
    public async Task Invoke_DoesNotCacheFallbackPerUser()
    {
        // When a user has no explicit setting, the resolved locale comes from
        // Accept-Language or default — this should NOT be cached per-user to
        // avoid cross-browser cache pollution.
        var callCount = 0;
        var settings = new FakeSettingsContracts(null, onGet: () => callCount++);
        using var localCache = new MemoryCache(new MemoryCacheOptions());

        var middleware = CreateMiddleware(
            _ => Task.CompletedTask,
            CreateConfiguration(null),
            localCache);

        var context1 = CreateHttpContext(settings, userId: "user-2");
        await middleware.InvokeAsync(context1);
        callCount.Should().Be(1);

        // Second request — should hit DB again since no explicit setting was cached
        var context2 = CreateHttpContext(settings, userId: "user-2");
        await middleware.InvokeAsync(context2);
        callCount.Should().Be(2, "fallback should not be cached per-user");
    }

    private LocaleResolutionMiddleware CreateMiddleware(
        RequestDelegate next,
        IConfiguration config,
        IMemoryCache cache)
    {
        return new LocaleResolutionMiddleware(next, config, _loader, cache);
    }

    private static RequestDelegate CaptureLocale(Action<string> setter)
    {
        return _ =>
        {
            setter(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
            return Task.CompletedTask;
        };
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

    private sealed class FakeSettingsContracts(string? language, Action? onGet = null) : ISettingsContracts
    {
        public Task<string?> GetSettingAsync(string key, SettingScope scope, string? userId = null)
        {
            onGet?.Invoke();
            return Task.FromResult(language);
        }

        public Task<T?> GetSettingAsync<T>(string key, SettingScope scope, string? userId = null)
        {
            onGet?.Invoke();
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

    public void Dispose() => _cache.Dispose();
}
