using FluentAssertions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace SimpleModule.DevTools.Tests;

public sealed class FileWatcherIntegrationTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(
        Path.GetTempPath(),
        $"devtools-test-{Guid.NewGuid():N}"
    );

    private readonly LiveReloadServer _liveReload = new(NullLogger<LiveReloadServer>.Instance);

    public FileWatcherIntegrationTests()
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
    public async Task Watches_ClientApp_When_Directory_Exists()
    {
        Directory.CreateDirectory(Path.Combine(_tempDir, ".git"));

        var hostDir = Path.Combine(_tempDir, "template", "SimpleModule.Host");
        var clientAppDir = Path.Combine(hostDir, "ClientApp");
        Directory.CreateDirectory(clientAppDir);

        var env = new FakeHostEnvironment(hostDir);
        using var service = new ViteDevWatchService(
            NullLogger<ViteDevWatchService>.Instance,
            env,
            _liveReload
        );

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await service.StartAsync(cts.Token);
        await Task.Delay(100);

        // Watcher is active — writing a file should not throw
        await File.WriteAllTextAsync(Path.Combine(clientAppDir, "test.ts"), "export {}");
        await Task.Delay(100);

        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Watches_Styles_When_Directory_Exists()
    {
        Directory.CreateDirectory(Path.Combine(_tempDir, ".git"));

        var hostDir = Path.Combine(_tempDir, "template", "SimpleModule.Host");
        var stylesDir = Path.Combine(hostDir, "Styles");
        Directory.CreateDirectory(stylesDir);

        var env = new FakeHostEnvironment(hostDir);
        using var service = new ViteDevWatchService(
            NullLogger<ViteDevWatchService>.Instance,
            env,
            _liveReload
        );

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await service.StartAsync(cts.Token);
        await Task.Delay(100);

        // Watcher is active — writing a file should not throw
        await File.WriteAllTextAsync(Path.Combine(stylesDir, "test.css"), "body {}");
        await Task.Delay(100);

        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Skips_ClientApp_When_Directory_Missing()
    {
        Directory.CreateDirectory(Path.Combine(_tempDir, ".git"));

        var hostDir = Path.Combine(_tempDir, "template", "SimpleModule.Host");
        Directory.CreateDirectory(hostDir);
        // Intentionally do NOT create ClientApp/

        var env = new FakeHostEnvironment(hostDir);
        using var service = new ViteDevWatchService(
            NullLogger<ViteDevWatchService>.Instance,
            env,
            _liveReload
        );

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await service.StartAsync(cts.Token);
        await Task.Delay(100);
        await service.StopAsync(CancellationToken.None);

        // No exception = correctly skipped missing directory
    }
}

/// <summary>
/// Minimal IHostEnvironment implementation for testing.
/// </summary>
internal sealed class FakeHostEnvironment(string contentRootPath) : IHostEnvironment
{
    public string ApplicationName { get; set; } = "TestApp";
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    public string ContentRootPath { get; set; } = contentRootPath;
    public string EnvironmentName { get; set; } = "Development";
}
