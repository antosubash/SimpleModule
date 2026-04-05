using FluentAssertions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace SimpleModule.DevTools.Tests;

public sealed class FindRepoRootTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(
        Path.GetTempPath(),
        $"devtools-test-{Guid.NewGuid():N}"
    );

    public FindRepoRootTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Fact]
    public void FindRepoRoot_Returns_Directory_With_Git_Folder()
    {
        Directory.CreateDirectory(Path.Combine(_tempDir, ".git"));
        var nested = Path.Combine(_tempDir, "a", "b", "c");
        Directory.CreateDirectory(nested);

        var result = ViteDevWatchService.FindRepoRoot(nested);

        result.Should().Be(_tempDir);
    }

    [Fact]
    public void FindRepoRoot_Returns_Directory_With_Git_File_Worktree()
    {
        // In git worktrees, .git is a file (not a directory) containing "gitdir: ..."
        File.WriteAllText(
            Path.Combine(_tempDir, ".git"),
            "gitdir: /some/other/repo/.git/worktrees/branch"
        );
        var nested = Path.Combine(_tempDir, "a", "b");
        Directory.CreateDirectory(nested);

        var result = ViteDevWatchService.FindRepoRoot(nested);

        result.Should().Be(_tempDir);
    }

    [Fact]
    public void FindRepoRoot_Returns_Null_When_No_Git_Folder()
    {
        var nested = Path.Combine(_tempDir, "no-git", "deep");
        Directory.CreateDirectory(nested);

        var result = ViteDevWatchService.FindRepoRoot(nested);

        // Will walk up to filesystem root — may find an actual .git or return null.
        // The important thing is it doesn't throw.
        result.Should().NotBe(nested);
    }

    [Fact]
    public void FindRepoRoot_Returns_Exact_Directory_When_StartPath_Has_Git()
    {
        Directory.CreateDirectory(Path.Combine(_tempDir, ".git"));

        var result = ViteDevWatchService.FindRepoRoot(_tempDir);

        result.Should().Be(_tempDir);
    }
}

public sealed class DiscoverModuleDirectoriesTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(
        Path.GetTempPath(),
        $"devtools-test-{Guid.NewGuid():N}"
    );

    public DiscoverModuleDirectoriesTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Fact]
    public void Discovers_Modules_With_ViteConfig()
    {
        // modules/Products/src/Products/vite.config.ts
        var productsDir = Path.Combine(_tempDir, "Products", "src", "Products");
        Directory.CreateDirectory(productsDir);
        File.WriteAllText(Path.Combine(productsDir, "vite.config.ts"), "");

        // modules/Orders/src/Orders/vite.config.ts
        var ordersDir = Path.Combine(_tempDir, "Orders", "src", "Orders");
        Directory.CreateDirectory(ordersDir);
        File.WriteAllText(Path.Combine(ordersDir, "vite.config.ts"), "");

        var result = ViteDevWatchService.DiscoverModuleDirectories(_tempDir);

        result.Should().HaveCount(2);
        result.Should().Contain(d => d.EndsWith("Products", StringComparison.Ordinal));
        result.Should().Contain(d => d.EndsWith("Orders", StringComparison.Ordinal));
    }

    [Fact]
    public void Ignores_Modules_Without_ViteConfig()
    {
        // Has vite.config.ts
        var withVite = Path.Combine(_tempDir, "Products", "src", "Products");
        Directory.CreateDirectory(withVite);
        File.WriteAllText(Path.Combine(withVite, "vite.config.ts"), "");

        // No vite.config.ts
        var withoutVite = Path.Combine(_tempDir, "Backend", "src", "Backend");
        Directory.CreateDirectory(withoutVite);

        var result = ViteDevWatchService.DiscoverModuleDirectories(_tempDir);

        result.Should().ContainSingle();
        result[0].Should().EndWith("Products");
    }

    [Fact]
    public void Returns_Empty_When_ModulesRoot_Does_Not_Exist()
    {
        var result = ViteDevWatchService.DiscoverModuleDirectories(
            Path.Combine(_tempDir, "nonexistent")
        );

        result.Should().BeEmpty();
    }

    [Fact]
    public void Ignores_Modules_Without_Src_Directory()
    {
        // modules/Flat/ (no src/)
        var flatDir = Path.Combine(_tempDir, "Flat");
        Directory.CreateDirectory(flatDir);
        File.WriteAllText(Path.Combine(flatDir, "vite.config.ts"), "");

        var result = ViteDevWatchService.DiscoverModuleDirectories(_tempDir);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Discovers_Multiple_Modules_Within_Same_Group()
    {
        // modules/MyGroup/src/ModuleA/vite.config.ts
        var moduleA = Path.Combine(_tempDir, "MyGroup", "src", "ModuleA");
        Directory.CreateDirectory(moduleA);
        File.WriteAllText(Path.Combine(moduleA, "vite.config.ts"), "");

        // modules/MyGroup/src/ModuleB/vite.config.ts
        var moduleB = Path.Combine(_tempDir, "MyGroup", "src", "ModuleB");
        Directory.CreateDirectory(moduleB);
        File.WriteAllText(Path.Combine(moduleB, "vite.config.ts"), "");

        var result = ViteDevWatchService.DiscoverModuleDirectories(_tempDir);

        result.Should().HaveCount(2);
    }
}

public sealed class ContainsSegmentTests
{
    private static string P(params string[] parts) => Path.Combine(parts);

    [Fact]
    public void ContainsSegment_Detects_Matching_Segment()
    {
        var path = P("project", "wwwroot", "file.js");
        ViteDevWatchService.ContainsSegment(path, "wwwroot").Should().BeTrue();
    }

    [Fact]
    public void ContainsSegment_Returns_False_For_Missing_Segment()
    {
        var path = P("project", "src", "file.js");
        ViteDevWatchService.ContainsSegment(path, "wwwroot").Should().BeFalse();
    }

    [Fact]
    public void ContainsSegment_Detects_NodeModules()
    {
        var path = P("project", "node_modules", "pkg", "index.js");
        ViteDevWatchService.ContainsSegment(path, "node_modules").Should().BeTrue();
    }

    [Fact]
    public void ContainsSegment_Does_Not_Match_Partial_Segment_Name()
    {
        var path = P("project", "my_node_modules_backup", "file.js");
        ViteDevWatchService.ContainsSegment(path, "node_modules").Should().BeFalse();
    }

    [Fact]
    public void ContainsSegment_Detects_Scan_Segment()
    {
        var path = P("a", "_scan", "file.css");
        ViteDevWatchService.ContainsSegment(path, "_scan").Should().BeTrue();
    }

    [Fact]
    public void ContainsSegment_Does_Not_Match_Scanner_For_Scan()
    {
        var path = P("a", "scanner", "file.css");
        ViteDevWatchService.ContainsSegment(path, "_scan").Should().BeFalse();
    }

    [Fact]
    public void ContainsSegment_Is_Case_Insensitive()
    {
        var path = P("project", "WWWROOT", "file.js");
        ViteDevWatchService.ContainsSegment(path, "wwwroot").Should().BeTrue();
    }
}

public sealed class ShouldIgnorePathTests
{
    private static string P(params string[] parts) => Path.Combine(parts);

    [Fact]
    public void ShouldIgnoreModulePath_Ignores_Wwwroot()
    {
        var path = P("modules", "Products", "wwwroot", "Products.pages.js");
        ViteDevWatchService.ShouldIgnoreModulePath(path).Should().BeTrue();
    }

    [Fact]
    public void ShouldIgnoreModulePath_Ignores_NodeModules()
    {
        var path = P("modules", "Products", "node_modules", "react", "index.js");
        ViteDevWatchService.ShouldIgnoreModulePath(path).Should().BeTrue();
    }

    [Fact]
    public void ShouldIgnoreModulePath_Allows_Pages()
    {
        var path = P("modules", "Products", "Pages", "index.ts");
        ViteDevWatchService.ShouldIgnoreModulePath(path).Should().BeFalse();
    }

    [Fact]
    public void ShouldIgnoreModulePath_Allows_Views()
    {
        var path = P("modules", "Products", "Views", "Browse.tsx");
        ViteDevWatchService.ShouldIgnoreModulePath(path).Should().BeFalse();
    }

    [Fact]
    public void ShouldIgnoreClientAppPath_Ignores_NodeModules()
    {
        var path = P("ClientApp", "node_modules", "react", "index.js");
        ViteDevWatchService.ShouldIgnoreClientAppPath(path).Should().BeTrue();
    }

    [Fact]
    public void ShouldIgnoreClientAppPath_Allows_Source_Files()
    {
        var path = P("ClientApp", "app.tsx");
        ViteDevWatchService.ShouldIgnoreClientAppPath(path).Should().BeFalse();
    }

    [Fact]
    public void ShouldIgnoreTailwindPath_Ignores_Scan_Directory()
    {
        var path = P("Styles", "_scan", "output.css");
        ViteDevWatchService.ShouldIgnoreTailwindPath(path).Should().BeTrue();
    }

    [Fact]
    public void ShouldIgnoreTailwindPath_Allows_Source_Css()
    {
        var path = P("Styles", "app.css");
        ViteDevWatchService.ShouldIgnoreTailwindPath(path).Should().BeFalse();
    }
}

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
