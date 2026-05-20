using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SimpleModule.Core.RateLimiting;
using SimpleModule.Database;
using SimpleModule.RateLimiting.Contracts;

namespace SimpleModule.RateLimiting.Tests;

public sealed class RateLimitRuleCacheTests : IAsyncLifetime, IDisposable
{
    private readonly RateLimitingDbContext _db;
    private readonly RateLimitRuleCache _cache;
    private readonly ServiceProvider _services;

    public RateLimitRuleCacheTests()
    {
        var dbOptions = new DbContextOptionsBuilder<RateLimitingDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        var databaseOptions = Options.Create(
            new DatabaseOptions
            {
                ModuleConnections = new Dictionary<string, string>
                {
                    ["RateLimiting"] = "Data Source=:memory:",
                },
            }
        );
        _db = new RateLimitingDbContext(dbOptions, databaseOptions);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();

        var services = new ServiceCollection();
        services.AddSingleton(_db);
        _services = services.BuildServiceProvider();

        _cache = new RateLimitRuleCache(
            _services.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<RateLimitRuleCache>.Instance
        );
    }

    public ValueTask InitializeAsync() => default;

    public ValueTask DisposeAsync() => default;

    [Fact]
    public async Task FindForPath_ReturnsNull_BeforeRefresh()
    {
        _cache.FindForPath("/api/users").Should().BeNull();
        await Task.CompletedTask;
    }

    [Fact]
    public async Task FindForPath_ReturnsNull_WhenNoRulesEnabled()
    {
        _db.Rules.Add(NewRule("disabled-rule", "/api/users", isEnabled: false));
        await _db.SaveChangesAsync();

        await _cache.RefreshAsync();

        _cache.FindForPath("/api/users").Should().BeNull();
    }

    [Fact]
    public async Task FindForPath_MatchesExactPattern()
    {
        _db.Rules.Add(NewRule("exact", "/api/users"));
        await _db.SaveChangesAsync();

        await _cache.RefreshAsync();

        _cache.FindForPath("/api/users").Should().NotBeNull();
        _cache.FindForPath("/api/users/123").Should().BeNull();
    }

    [Fact]
    public async Task FindForPath_MatchesPrefixWildcard()
    {
        _db.Rules.Add(NewRule("prefix", "/api/users/*"));
        await _db.SaveChangesAsync();

        await _cache.RefreshAsync();

        _cache.FindForPath("/api/users/123").Should().NotBeNull();
        _cache.FindForPath("/api/users/").Should().NotBeNull();
        _cache.FindForPath("/api/orders").Should().BeNull();
    }

    [Fact]
    public async Task FindForPath_MatchesBareWildcard()
    {
        _db.Rules.Add(NewRule("catchall", "*"));
        await _db.SaveChangesAsync();

        await _cache.RefreshAsync();

        _cache.FindForPath("/literally/anything").Should().NotBeNull();
    }

    [Fact]
    public async Task FindForPath_PrefersMoreSpecificRule()
    {
        _db.Rules.AddRange(
            NewRule("catchall", "*", permitLimit: 10),
            NewRule("specific", "/api/users", permitLimit: 99)
        );
        await _db.SaveChangesAsync();

        await _cache.RefreshAsync();

        _cache.FindForPath("/api/users")!.PermitLimit.Should().Be(99);
        _cache.FindForPath("/api/orders")!.PermitLimit.Should().Be(10);
    }

    [Fact]
    public async Task RefreshAsync_PicksUpNewRules()
    {
        await _cache.RefreshAsync();
        _cache.FindForPath("/api/things").Should().BeNull();

        _db.Rules.Add(NewRule("late", "/api/things"));
        await _db.SaveChangesAsync();
        await _cache.RefreshAsync();

        _cache.FindForPath("/api/things").Should().NotBeNull();
    }

    private static RateLimitRule NewRule(
        string name,
        string? pattern,
        bool isEnabled = true,
        int permitLimit = 60
    ) =>
        new()
        {
            PolicyName = name,
            EndpointPattern = pattern,
            IsEnabled = isEnabled,
            PermitLimit = permitLimit,
        };

    public void Dispose()
    {
        _db.Database.CloseConnection();
        _db.Dispose();
        _services.Dispose();
        GC.SuppressFinalize(this);
    }
}
