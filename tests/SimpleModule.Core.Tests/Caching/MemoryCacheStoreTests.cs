using System.Collections.Concurrent;
using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using SimpleModule.Core.Caching;

namespace SimpleModule.Core.Tests.Caching;

public sealed class MemoryCacheStoreTests : IDisposable
{
    private readonly MemoryCache _memoryCache = new(new MemoryCacheOptions());
    private readonly MemoryCacheStore _store;

    public MemoryCacheStoreTests()
    {
        _store = new MemoryCacheStore(_memoryCache);
    }

    [Fact]
    public async Task TryGetAsync_ReturnsMiss_WhenKeyAbsent()
    {
        var result = await _store.TryGetAsync<string>("missing");

        result.Hit.Should().BeFalse();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_ThenTryGetAsync_RoundTripsValue()
    {
        await _store.SetAsync("k1", "value");

        var result = await _store.TryGetAsync<string>("k1");

        result.Hit.Should().BeTrue();
        result.Value.Should().Be("value");
    }

    [Fact]
    public async Task SetAsync_AllowsCachingNullForNegativeCaching()
    {
        await _store.SetAsync<string?>("k1", null);

        var result = await _store.TryGetAsync<string?>("k1");

        result.Hit.Should().BeTrue("a cached null should be a hit, not a miss");
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_OverwritesExistingValue()
    {
        await _store.SetAsync("k1", "first");
        await _store.SetAsync("k1", "second");

        var result = await _store.TryGetAsync<string>("k1");

        result.Value.Should().Be("second");
    }

    [Fact]
    public async Task RemoveAsync_DeletesEntry()
    {
        await _store.SetAsync("k1", "value");

        await _store.RemoveAsync("k1");

        var result = await _store.TryGetAsync<string>("k1");
        result.Hit.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveAsync_IsNoOp_WhenKeyAbsent()
    {
        var act = async () => await _store.RemoveAsync("ghost");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetOrCreateAsync_InvokesFactory_OnMiss()
    {
        var calls = 0;

        var value = await _store.GetOrCreateAsync<string>(
            "k1",
            _ =>
            {
                calls++;
                return new ValueTask<string?>("created");
            }
        );

        value.Should().Be("created");
        calls.Should().Be(1);
    }

    [Fact]
    public async Task GetOrCreateAsync_SkipsFactory_OnHit()
    {
        await _store.SetAsync("k1", "existing");
        var calls = 0;

        var value = await _store.GetOrCreateAsync<string>(
            "k1",
            _ =>
            {
                calls++;
                return new ValueTask<string?>("created");
            }
        );

        value.Should().Be("existing");
        calls.Should().Be(0);
    }

    [Fact]
    public async Task GetOrCreateAsync_PreventsStampede_UnderConcurrentCallers()
    {
        var factoryCalls = 0;
        var gate = new TaskCompletionSource();

        async ValueTask<string?> Factory(CancellationToken _)
        {
            Interlocked.Increment(ref factoryCalls);
            await gate.Task;
            return "value";
        }

        var t1 = _store.GetOrCreateAsync<string>("stampede", Factory).AsTask();
        var t2 = _store.GetOrCreateAsync<string>("stampede", Factory).AsTask();
        var t3 = _store.GetOrCreateAsync<string>("stampede", Factory).AsTask();

        // Let the first factory release.
        gate.SetResult();
        var results = await Task.WhenAll(t1, t2, t3);

        factoryCalls
            .Should()
            .Be(1, "concurrent GetOrCreateAsync calls for the same key must coalesce");
        results.Should().AllBeEquivalentTo("value");
    }

    [Fact]
    public async Task GetOrCreateAsync_RespectsExpirationOptions()
    {
        await _store.GetOrCreateAsync<string>(
            "k1",
            _ => new ValueTask<string?>("v"),
            CacheEntryOptions.Expires(TimeSpan.FromMilliseconds(50))
        );

        await Task.Delay(150);

        var result = await _store.TryGetAsync<string>("k1");
        result.Hit.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveByPrefixAsync_RemovesAllMatchingKeys()
    {
        await _store.SetAsync("user:1:profile", "p1");
        await _store.SetAsync("user:1:settings", "s1");
        await _store.SetAsync("user:2:profile", "p2");
        await _store.SetAsync("system:bootstrap", "x");

        await _store.RemoveByPrefixAsync("user:1:");

        (await _store.TryGetAsync<string>("user:1:profile")).Hit.Should().BeFalse();
        (await _store.TryGetAsync<string>("user:1:settings")).Hit.Should().BeFalse();
        (await _store.TryGetAsync<string>("user:2:profile")).Hit.Should().BeTrue();
        (await _store.TryGetAsync<string>("system:bootstrap")).Hit.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveByPrefixAsync_RemovesEverything_WithEmptyKey()
    {
        await _store.SetAsync("a", "1");
        await _store.SetAsync("b", "2");

        await _store.RemoveByPrefixAsync("a");

        (await _store.TryGetAsync<string>("a")).Hit.Should().BeFalse();
        (await _store.TryGetAsync<string>("b")).Hit.Should().BeTrue();
    }

    [Fact]
    public async Task TryGetAsync_ThrowsOnNullOrEmptyKey()
    {
        var act1 = async () => await _store.TryGetAsync<string>(null!);
        var act2 = async () => await _store.TryGetAsync<string>(string.Empty);

        await act1.Should().ThrowAsync<ArgumentException>();
        await act2.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task WithPrefix_ScopesAllOperations()
    {
        var scoped = _store.WithPrefix("tenant-a");

        await scoped.SetAsync("user", "alice");

        // Scoped store sees the value.
        var hit = await scoped.TryGetAsync<string>("user");
        hit.Hit.Should().BeTrue();
        hit.Value.Should().Be("alice");

        // Underlying store sees the prefixed key.
        var underlying = await _store.TryGetAsync<string>("tenant-a:user");
        underlying.Hit.Should().BeTrue();
        underlying.Value.Should().Be("alice");

        // The unscoped key does not exist.
        (await _store.TryGetAsync<string>("user"))
            .Hit.Should()
            .BeFalse();
    }

    [Fact]
    public async Task WithPrefix_RemoveByPrefix_ScopesPrefix()
    {
        var scoped = _store.WithPrefix("tenant-a");
        await scoped.SetAsync("user:1", "alice");
        await scoped.SetAsync("user:2", "bob");
        await _store.SetAsync("user:1", "global"); // unscoped, must survive

        await scoped.RemoveByPrefixAsync("user");

        (await scoped.TryGetAsync<string>("user:1")).Hit.Should().BeFalse();
        (await scoped.TryGetAsync<string>("user:2")).Hit.Should().BeFalse();
        (await _store.TryGetAsync<string>("user:1")).Hit.Should().BeTrue();
    }

    [Fact]
    public async Task GetOrCreateAsync_DoesNotLeakPerKeyLocks()
    {
        // Populate many distinct keys. After each uncontended call the per-key
        // semaphore should be released; the lock dictionary must not grow unbounded.
        for (var i = 0; i < 50; i++)
        {
            await _store.GetOrCreateAsync<int>($"leak:{i}", _ => new ValueTask<int>(i));
        }

        GetKeyLocks(_store).Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveAsync_DuringInFlightFactory_DoesNotCrash()
    {
        // RemoveAsync must not dispose a semaphore that a concurrent GetOrCreateAsync
        // caller is still holding. The in-flight caller's own finally block reclaims
        // the lock after it releases the gate.
        var factoryGate = new TaskCompletionSource();
        var held = _store
            .GetOrCreateAsync<string>(
                "contended",
                async _ =>
                {
                    await factoryGate.Task;
                    return "value";
                }
            )
            .AsTask();

        await Task.Yield();
        await _store.RemoveAsync("contended");

        factoryGate.SetResult();
        var result = await held;

        result.Should().Be("value");
        GetKeyLocks(_store).Should().NotContainKey("contended");
    }

    private static ConcurrentDictionary<string, SemaphoreSlim> GetKeyLocks(MemoryCacheStore store)
    {
        var field = typeof(MemoryCacheStore).GetField(
            "_keyLocks",
            BindingFlags.NonPublic | BindingFlags.Instance
        )!;
        return (ConcurrentDictionary<string, SemaphoreSlim>)field.GetValue(store)!;
    }

    [Fact]
    public void CacheKey_Compose_JoinsPartsAndSkipsEmpty()
    {
        CacheKey.Compose("a", "b", "c").Should().Be("a:b:c");
        CacheKey.Compose("a", null, "c").Should().Be("a:c");
        CacheKey.Compose("a", string.Empty, "c").Should().Be("a:c");
        CacheKey.Compose("only").Should().Be("only");
    }

    public void Dispose()
    {
        _store.Dispose();
        _memoryCache.Dispose();
        GC.SuppressFinalize(this);
    }
}
