using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace SimpleModule.DevTools.Tests;

public sealed class ServiceLifecycleTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(
        Path.GetTempPath(),
        $"devtools-test-{Guid.NewGuid():N}"
    );

    private readonly LiveReloadServer _liveReload = new(NullLogger<LiveReloadServer>.Instance);

    public ServiceLifecycleTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        _liveReload.Dispose();
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_Returns_When_No_Git_Root_Found()
    {
        var env = new FakeHostEnvironment(_tempDir);
        using var service = new ViteDevWatchService(
            NullLogger<ViteDevWatchService>.Instance,
            env,
            _liveReload
        );

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await service.StartAsync(cts.Token);

        // Service should return quickly without hanging — no .git found
        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ExecuteAsync_Discovers_Modules_And_Sets_Up_Watchers()
    {
        // Create repo structure: .git + modules/Products/src/Products/vite.config.ts
        Directory.CreateDirectory(Path.Combine(_tempDir, ".git"));
        var moduleDir = Path.Combine(_tempDir, "modules", "Products", "src", "Products");
        Directory.CreateDirectory(moduleDir);
        await File.WriteAllTextAsync(Path.Combine(moduleDir, "vite.config.ts"), "");

        // Create host content root inside the repo
        var hostDir = Path.Combine(_tempDir, "template", "SimpleModule.Host");
        Directory.CreateDirectory(hostDir);

        var env = new FakeHostEnvironment(hostDir);
        using var service = new ViteDevWatchService(
            NullLogger<ViteDevWatchService>.Instance,
            env,
            _liveReload
        );

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await service.StartAsync(cts.Token);

        // Give ExecuteAsync a moment to set up watchers
        await Task.Delay(100);

        // Service is running (hasn't exited)
        await service.StopAsync(CancellationToken.None);

        // No exception thrown = watchers were set up and torn down correctly
    }

    [Fact]
    public async Task Service_Shuts_Down_Cleanly_On_Cancellation()
    {
        Directory.CreateDirectory(Path.Combine(_tempDir, ".git"));
        var moduleDir = Path.Combine(_tempDir, "modules", "Test", "src", "Test");
        Directory.CreateDirectory(moduleDir);
        await File.WriteAllTextAsync(Path.Combine(moduleDir, "vite.config.ts"), "");

        var hostDir = Path.Combine(_tempDir, "template", "SimpleModule.Host");
        Directory.CreateDirectory(hostDir);

        var env = new FakeHostEnvironment(hostDir);
        using var service = new ViteDevWatchService(
            NullLogger<ViteDevWatchService>.Instance,
            env,
            _liveReload
        );

        using var cts = new CancellationTokenSource();
        await service.StartAsync(cts.Token);
        await Task.Delay(100);

        // Cancel and verify clean shutdown
        await cts.CancelAsync();
        await service.StopAsync(CancellationToken.None);

        // Dispose should not throw
        service.Dispose();
    }

    [Fact]
    public async Task Dispose_Is_Idempotent()
    {
        var env = new FakeHostEnvironment(_tempDir);
        var service = new ViteDevWatchService(
            NullLogger<ViteDevWatchService>.Instance,
            env,
            _liveReload
        );

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        await service.StartAsync(cts.Token);
        await service.StopAsync(CancellationToken.None);

        // Double dispose should not throw
        service.Dispose();
        service.Dispose();
    }
}
