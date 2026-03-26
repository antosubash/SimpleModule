using FluentAssertions;
using SimpleModule.Cli.Commands.Doctor.Checks;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Tests;

public sealed class ViteConfigCheckTests : IDisposable
{
    private readonly string _tempDir;

    public ViteConfigCheckTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"sm-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true);
    }

    private SolutionContext CreateSolutionWithViteConfig(string moduleName, string? viteConfigContent)
    {
        File.WriteAllText(Path.Combine(_tempDir, "Test.slnx"), "<Solution />");
        var moduleDir = Path.Combine(_tempDir, "src", "modules", moduleName, "src", moduleName);
        Directory.CreateDirectory(moduleDir);
        if (viteConfigContent is not null)
            File.WriteAllText(Path.Combine(moduleDir, "vite.config.ts"), viteConfigContent);
        return SolutionContext.Discover(_tempDir)!;
    }

    [Fact]
    public void Run_Pass_WhenViteConfigCorrect()
    {
        var solution = CreateSolutionWithViteConfig("Products", """
            import { defineConfig } from 'vite'
            export default defineConfig({
              build: { lib: { entry: 'Pages/index.ts' } },
              external: ['react', 'react-dom', '@inertiajs/react'],
            })
            """);
        var results = new ViteConfigCheck().Run(solution).ToList();
        results.Should().ContainSingle(r => r.Name == "Products vite.config.ts" && r.Status == CheckStatus.Pass);
    }

    [Fact]
    public void Run_Warn_WhenViteConfigMissing()
    {
        var solution = CreateSolutionWithViteConfig("Products", viteConfigContent: null);
        var results = new ViteConfigCheck().Run(solution).ToList();
        results.Should().ContainSingle(r => r.Name == "Products vite.config.ts" && r.Status == CheckStatus.Warning);
    }

    [Fact]
    public void Run_Warn_WhenLibModeNotConfigured()
    {
        var solution = CreateSolutionWithViteConfig("Products", """
            import { defineConfig } from 'vite'
            export default defineConfig({})
            """);
        var results = new ViteConfigCheck().Run(solution).ToList();
        results.Should().ContainSingle(r => r.Name == "Products vite.config.ts" && r.Status == CheckStatus.Warning);
    }

    [Fact]
    public void Run_Warn_WhenExternalsIncomplete()
    {
        var solution = CreateSolutionWithViteConfig("Products", """
            build: { lib: { entry: 'Pages/index.ts' } },
            external: ['react'],
            """);
        var results = new ViteConfigCheck().Run(solution).ToList();
        results.Should().ContainSingle(r => r.Name == "Products vite.config.ts" && r.Status == CheckStatus.Warning);
    }
}
