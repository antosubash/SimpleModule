using FluentAssertions;

namespace SimpleModule.DevTools.Tests;

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
