using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SimpleModule.Core.Caching;
using SimpleModule.Core.FeatureFlags;
using SimpleModule.Database;
using SimpleModule.FeatureFlags;
using SimpleModule.FeatureFlags.Contracts;
using SimpleModule.FeatureFlags.Entities;

namespace FeatureFlags.Tests.Unit;

public sealed class FeatureFlagServiceTests : IDisposable
{
    private readonly FeatureFlagsDbContext _db;
    private readonly FeatureFlagService _sut;
    private readonly IFeatureFlagRegistry _registry;
    private readonly MemoryCache _cache;
    private readonly MemoryCacheStore _cacheStore;
    private readonly List<MemoryCache> _freshCaches = [];
    private readonly List<MemoryCacheStore> _freshCacheStores = [];

    public FeatureFlagServiceTests()
    {
        var options = new DbContextOptionsBuilder<FeatureFlagsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var dbOptions = Options.Create(
            new DatabaseOptions { DefaultConnection = "Data Source=:memory:" }
        );
        _db = new FeatureFlagsDbContext(options, dbOptions);
        _db.Database.EnsureCreated();

        var builder = new FeatureFlagRegistryBuilder();
        builder.AddDefinition(
            new FeatureFlagDefinition
            {
                Name = "Test.FeatureA",
                Description = "Test feature A",
                DefaultEnabled = true,
            }
        );
        builder.AddDefinition(
            new FeatureFlagDefinition
            {
                Name = "Test.FeatureB",
                Description = "Test feature B",
                DefaultEnabled = false,
            }
        );
        _registry = builder.Build();

        _cache = new MemoryCache(Options.Create(new MemoryCacheOptions()));
        _cacheStore = new MemoryCacheStore(_cache);
        _sut = new FeatureFlagService(
            _db,
            _registry,
            _cacheStore,
            NullLogger<FeatureFlagService>.Instance,
            new ServiceCollection().BuildServiceProvider()
        );
    }

    public void Dispose()
    {
        foreach (var s in _freshCacheStores)
        {
            s.Dispose();
        }

        foreach (var c in _freshCaches)
        {
            c.Dispose();
        }

        _cacheStore.Dispose();
        _cache.Dispose();
        _db.Dispose();
    }

    private FeatureFlagService CreateFreshService()
    {
        // Create a new cache to bypass cached results
        var freshCache = new MemoryCache(Options.Create(new MemoryCacheOptions()));
        _freshCaches.Add(freshCache);
        var freshStore = new MemoryCacheStore(freshCache);
        _freshCacheStores.Add(freshStore);
        return new FeatureFlagService(
            _db,
            _registry,
            freshStore,
            NullLogger<FeatureFlagService>.Instance,
            new ServiceCollection().BuildServiceProvider()
        );
    }

    [Fact]
    public async Task IsEnabledAsync_NoDbRow_ReturnsRegistryDefault()
    {
        var result = await _sut.IsEnabledAsync("Test.FeatureA");
        result.Should().BeTrue();

        var result2 = await _sut.IsEnabledAsync("Test.FeatureB");
        result2.Should().BeFalse();
    }

    [Fact]
    public async Task IsEnabledAsync_WithDbRow_ReturnsDbState()
    {
        _db.FeatureFlags.Add(new FeatureFlagEntity { Name = "Test.FeatureA", IsEnabled = false });
        await _db.SaveChangesAsync();

        // Use a fresh service to bypass cache
        var sut = CreateFreshService();

        var result = await sut.IsEnabledAsync("Test.FeatureA");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsEnabledAsync_WithUserOverride_ReturnsOverride()
    {
        _db.FeatureFlags.Add(new FeatureFlagEntity { Name = "Test.FeatureB", IsEnabled = false });
        _db.FeatureFlagOverrides.Add(
            new FeatureFlagOverrideEntity
            {
                FlagName = "Test.FeatureB",
                OverrideType = OverrideType.User,
                OverrideValue = "user-1",
                IsEnabled = true,
            }
        );
        await _db.SaveChangesAsync();

        var sut = CreateFreshService();

        var result = await sut.IsEnabledAsync("Test.FeatureB", "user-1");
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsEnabledAsync_WithRoleOverride_ReturnsOverride()
    {
        _db.FeatureFlags.Add(new FeatureFlagEntity { Name = "Test.FeatureB", IsEnabled = false });
        _db.FeatureFlagOverrides.Add(
            new FeatureFlagOverrideEntity
            {
                FlagName = "Test.FeatureB",
                OverrideType = OverrideType.Role,
                OverrideValue = "Beta",
                IsEnabled = true,
            }
        );
        await _db.SaveChangesAsync();

        var sut = CreateFreshService();

        var result = await sut.IsEnabledAsync("Test.FeatureB", null, ["Beta"]);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsEnabledAsync_UserOverrideTakesPrecedenceOverRole()
    {
        _db.FeatureFlagOverrides.Add(
            new FeatureFlagOverrideEntity
            {
                FlagName = "Test.FeatureA",
                OverrideType = OverrideType.Role,
                OverrideValue = "Beta",
                IsEnabled = true,
            }
        );
        _db.FeatureFlagOverrides.Add(
            new FeatureFlagOverrideEntity
            {
                FlagName = "Test.FeatureA",
                OverrideType = OverrideType.User,
                OverrideValue = "user-1",
                IsEnabled = false,
            }
        );
        await _db.SaveChangesAsync();

        var sut = CreateFreshService();

        var result = await sut.IsEnabledAsync("Test.FeatureA", "user-1", ["Beta"]);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetAllFlagsAsync_ReturnsMergedList()
    {
        var flags = await _sut.GetAllFlagsAsync();
        flags.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateFlagAsync_CreatesAndUpdates()
    {
        var result = await _sut.UpdateFlagAsync(
            "Test.FeatureA",
            new UpdateFeatureFlagRequest { IsEnabled = false }
        );
        result.IsEnabled.Should().BeFalse();

        var updated = await _sut.UpdateFlagAsync(
            "Test.FeatureA",
            new UpdateFeatureFlagRequest { IsEnabled = true }
        );
        updated.IsEnabled.Should().BeTrue();
    }
}
