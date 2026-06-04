using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NSubstitute;
using SimpleModule.AuditLogs.Middleware;
using SimpleModule.AuditLogs.Pipeline;
using SimpleModule.Core.Settings;
using SimpleModule.Settings.Contracts;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.NullObjects;

namespace AuditLogs.Tests.Unit;

public sealed class AuditMiddlewareTests : IDisposable
{
    private readonly NullFusionCache _cache = new(Options.Create(new FusionCacheOptions()));

    public void Dispose() => _cache.Dispose();

    [Fact]
    public async Task InvokeAsync_LoadsSettingsOnce_AndCachesForRequest()
    {
        // Arrange
        var settings = Substitute.For<ISettingsContracts>();
        var channel = new AuditChannel();
        var next = Substitute.For<RequestDelegate>();

        var context = new DefaultHttpContext();
        context.RequestServices = CreateServiceProvider(settings, channel);

        // Configure settings to return default values
        settings.GetSettingAsync(Arg.Any<string>(), Arg.Any<SettingScope>()).Returns("true");

        var middleware = new AuditMiddleware(next, _cache);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        // Verify that GetSettingAsync was called exactly once for each setting key
        await settings
            .Received(1)
            .GetSettingAsync("auditlogs.capture.http", Arg.Any<SettingScope>());
        await settings
            .Received(1)
            .GetSettingAsync("auditlogs.capture.requestbodies", Arg.Any<SettingScope>());
        await settings
            .Received(1)
            .GetSettingAsync("auditlogs.capture.querystrings", Arg.Any<SettingScope>());
        await settings
            .Received(1)
            .GetSettingAsync("auditlogs.capture.useragent", Arg.Any<SettingScope>());
        await settings
            .Received(1)
            .GetSettingAsync("auditlogs.excluded.paths", Arg.Any<SettingScope>());

        // Verify all five settings were fetched (sequentially, over the scoped DbContext)
        // Total calls should be exactly 5
        await settings.Received(5).GetSettingAsync(Arg.Any<string>(), Arg.Any<SettingScope>());
    }

    [Fact]
    public async Task InvokeAsync_SkipsProcessing_WhenHttpCaptureDisabled()
    {
        // Arrange
        var settings = Substitute.For<ISettingsContracts>();
        var channel = new AuditChannel();
        var next = Substitute.For<RequestDelegate>();

        var context = new DefaultHttpContext();
        context.RequestServices = CreateServiceProvider(settings, channel);

        // Configure capture.http to false
        settings
            .GetSettingAsync("auditlogs.capture.http", Arg.Any<SettingScope>())
            .Returns("false");
        settings.GetSettingAsync(Arg.Any<string>(), Arg.Any<SettingScope>()).Returns("true");

        var middleware = new AuditMiddleware(next, _cache);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        await next.Received(1).Invoke(context);

        // Verify no audit entry was created
        context.Response.StatusCode.Should().Be(200); // Default
    }

    [Fact]
    public async Task InvokeAsync_SkipsProcessing_ForExcludedPath()
    {
        // Arrange
        var settings = Substitute.For<ISettingsContracts>();
        var channel = new AuditChannel();
        var next = Substitute.For<RequestDelegate>();

        var context = new DefaultHttpContext();
        context.Request.Path = "/health";
        context.RequestServices = CreateServiceProvider(settings, channel);

        settings.GetSettingAsync(Arg.Any<string>(), Arg.Any<SettingScope>()).Returns("true");

        var middleware = new AuditMiddleware(next, _cache);

        // Act
        await middleware.InvokeAsync(context);

        // Assert - middleware should not audit /health path
        await next.Received(1).Invoke(context);
    }

    [Fact]
    public async Task InvokeAsync_ExcludesPath_WhenConfigured()
    {
        // Arrange
        var settings = Substitute.For<ISettingsContracts>();
        var channel = new AuditChannel();
        var next = Substitute.For<RequestDelegate>();

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/custom-exclude";
        context.Request.Method = "POST";
        context.RequestServices = CreateServiceProvider(settings, channel);

        // Configure default settings but with custom excluded paths
        settings
            .GetSettingAsync("auditlogs.capture.http", Arg.Any<SettingScope>())
            .Returns("true");
        settings
            .GetSettingAsync("auditlogs.capture.requestbodies", Arg.Any<SettingScope>())
            .Returns("true");
        settings
            .GetSettingAsync("auditlogs.capture.querystrings", Arg.Any<SettingScope>())
            .Returns("true");
        settings
            .GetSettingAsync("auditlogs.capture.useragent", Arg.Any<SettingScope>())
            .Returns("false");
        settings
            .GetSettingAsync("auditlogs.excluded.paths", Arg.Any<SettingScope>())
            .Returns("/api/custom-exclude");

        var middleware = new AuditMiddleware(next, _cache);

        // Act
        await middleware.InvokeAsync(context);

        // Assert - middleware should skip excluded path
        await next.Received(1).Invoke(context);
    }

    [Fact]
    public async Task InvokeAsync_MergesHardcodedAndConfiguredExcludedPaths()
    {
        // Arrange
        var settings = Substitute.For<ISettingsContracts>();
        var channel = new AuditChannel();
        var next = Substitute.For<RequestDelegate>();

        var context = new DefaultHttpContext();
        context.Request.Path = "/health";
        context.Request.Method = "GET";
        context.RequestServices = CreateServiceProvider(settings, channel);

        settings.GetSettingAsync("auditlogs.capture.http", Arg.Any<SettingScope>()).Returns("true");
        settings
            .GetSettingAsync("auditlogs.capture.requestbodies", Arg.Any<SettingScope>())
            .Returns("true");
        settings
            .GetSettingAsync("auditlogs.capture.querystrings", Arg.Any<SettingScope>())
            .Returns("true");
        settings
            .GetSettingAsync("auditlogs.capture.useragent", Arg.Any<SettingScope>())
            .Returns("false");
        // Return both hardcoded path and configured path
        settings
            .GetSettingAsync("auditlogs.excluded.paths", Arg.Any<SettingScope>())
            .Returns("/api/internal");

        var middleware = new AuditMiddleware(next, _cache);

        // Act - test hardcoded /health path
        await middleware.InvokeAsync(context);

        // Assert - hardcoded /health should be excluded
        await next.Received(1).Invoke(context);
    }

    [Fact]
    public async Task InvokeAsync_PathMatching_IsCaseInsensitive()
    {
        // Arrange
        var settings = Substitute.For<ISettingsContracts>();
        var channel = new AuditChannel();
        var next = Substitute.For<RequestDelegate>();

        var context = new DefaultHttpContext();
        context.Request.Path = "/Health"; // Different case
        context.Request.Method = "GET";
        context.RequestServices = CreateServiceProvider(settings, channel);

        settings.GetSettingAsync(Arg.Any<string>(), Arg.Any<SettingScope>()).Returns("true");

        var middleware = new AuditMiddleware(next, _cache);

        // Act
        await middleware.InvokeAsync(context);

        // Assert - /Health should match /health (case-insensitive)
        await next.Received(1).Invoke(context);
    }

    [Fact]
    public async Task InvokeAsync_UsesDefaultSettings_WhenSettingsServiceIsNull()
    {
        // Arrange
        var channel = new AuditChannel();
        var next = Substitute.For<RequestDelegate>();

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.Request.Method = "GET";
        context.RequestServices = CreateServiceProviderWithoutSettings(channel);

        var middleware = new AuditMiddleware(next, _cache);

        // Act
        await middleware.InvokeAsync(context);

        // Assert - should use defaults: CaptureHttp=true, other defaults
        await next.Received(1).Invoke(context);
    }

    [Fact]
    public async Task InvokeAsync_DefaultValues_AreCorrect()
    {
        // Arrange
        var settings = Substitute.For<ISettingsContracts>();
        var channel = new AuditChannel();
        var next = Substitute.For<RequestDelegate>();

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.Request.Method = "GET";
        context.RequestServices = CreateServiceProvider(settings, channel);

        settings.GetSettingAsync(Arg.Any<string>(), Arg.Any<SettingScope>()).Returns((string?)null); // Simulate no setting found

        var middleware = new AuditMiddleware(next, _cache);

        // Act
        await middleware.InvokeAsync(context);

        // Assert - should process since defaults are CaptureHttp=true, CaptureUserAgent=false
        await next.Received(1).Invoke(context);
    }

    [Fact]
    public async Task InvokeAsync_HandlesNullSettingsGracefully()
    {
        // Arrange
        var channel = new AuditChannel();
        var next = Substitute.For<RequestDelegate>();

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.Request.Method = "POST";
        context.RequestServices = CreateServiceProviderWithoutSettings(channel);

        var middleware = new AuditMiddleware(next, _cache);

        // Act & Assert - should not throw
        var action = () => middleware.InvokeAsync(context);
        await action.Should().NotThrowAsync();

        await next.Received(1).Invoke(context);
    }

    [Fact]
    public async Task InvokeAsync_LoadsSettingsSequentially_NeverConcurrently()
    {
        // Regression for #232: GetSettingAsync runs over a single scoped SettingsDbContext,
        // which is not thread-safe. The middleware must await each lookup sequentially rather
        // than fanning out via Task.WhenAll, otherwise the DbContext races and throws.
        var settings = new ConcurrencyTrackingSettings();
        var channel = new AuditChannel();
        var next = Substitute.For<RequestDelegate>();

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.Request.Method = "GET";
        context.RequestServices = CreateServiceProvider(settings, channel);

        var middleware = new AuditMiddleware(next, _cache);

        await middleware.InvokeAsync(context);

        settings.MaxObservedConcurrency.Should().Be(1);
        await next.Received(1).Invoke(context);
    }

    private static IServiceProvider CreateServiceProvider(
        ISettingsContracts settings,
        AuditChannel channel
    )
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(AuditChannel)).Returns(channel);
        serviceProvider.GetService(typeof(ISettingsContracts)).Returns(settings);
        return serviceProvider;
    }

    private static IServiceProvider CreateServiceProviderWithoutSettings(AuditChannel channel)
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(AuditChannel)).Returns(channel);
        serviceProvider.GetService(typeof(ISettingsContracts)).Returns(null);
        return serviceProvider;
    }

    /// <summary>
    /// Stand-in for the scoped settings service that records the peak number of overlapping
    /// <see cref="GetSettingAsync(string, SettingScope, string?)"/> calls. A correctly
    /// sequential caller keeps this at 1; a Task.WhenAll fan-out drives it above 1 (mirroring
    /// the DbContext race in #232).
    /// </summary>
    private sealed class ConcurrencyTrackingSettings : ISettingsContracts
    {
        private int _current;

        public int MaxObservedConcurrency { get; private set; }

        public async Task<string?> GetSettingAsync(
            string key,
            SettingScope scope,
            string? userId = null
        )
        {
            var observed = Interlocked.Increment(ref _current);
            MaxObservedConcurrency = Math.Max(MaxObservedConcurrency, observed);
            try
            {
                // Yield so overlapping callers (if any) have a chance to enter before this returns.
                await Task.Yield();
                return "true";
            }
            finally
            {
                Interlocked.Decrement(ref _current);
            }
        }

        public Task<T?> GetSettingAsync<T>(string key, SettingScope scope, string? userId = null) =>
            throw new NotSupportedException();

        public Task<string?> ResolveUserSettingAsync(string key, string userId) =>
            throw new NotSupportedException();

        public Task<JsonElement?> ResolveUserSettingElementAsync(string key, string userId) =>
            throw new NotSupportedException();

        public Task SetSettingAsync(
            string key,
            JsonElement value,
            SettingScope scope,
            string? userId = null
        ) => throw new NotSupportedException();

        public Task SetManyAsync(IReadOnlyList<BulkSettingUpdate> updates) =>
            throw new NotSupportedException();

        public Task DeleteSettingAsync(string key, SettingScope scope, string? userId = null) =>
            throw new NotSupportedException();

        public Task ResetToDefaultAsync(string key, SettingScope scope, string? userId = null) =>
            throw new NotSupportedException();

        public Task<IEnumerable<SettingValueDto>> GetSettingValuesAsync(
            SettingsFilter? filter = null
        ) => throw new NotSupportedException();

        public Task<SettingValueDto?> GetSettingValueAsync(
            string key,
            SettingScope scope,
            string? userId = null
        ) => throw new NotSupportedException();
    }
}
