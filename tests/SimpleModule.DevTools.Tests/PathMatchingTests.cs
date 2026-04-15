using FluentAssertions;

namespace SimpleModule.DevTools.Tests;

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
