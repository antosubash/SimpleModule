using FluentAssertions;
using SimpleModule.Generator.Tests.Helpers;

namespace SimpleModule.Generator.Tests;

public class ViewPagesEmitterTests
{
    [Fact]
    public void Module_WithNoViews_NoViewPagesFileEmitted()
    {
        var source = """
            using SimpleModule.Core;

            namespace TestApp;

            [Module("Test")]
            public class TestModule : IModule { }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        result.GeneratedTrees.Should().NotContain(t => t.FilePath.Contains("ViewPages_"));
    }

    [Fact]
    public void ModuleName_DerivedFromClassName_StripsModuleSuffix()
    {
        var source = """
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Routing;
            using SimpleModule.Core;

            namespace TestApp
            {
                [Module("Products", ViewPrefix = "/products")]
                public class ProductsModule : IModule { }
            }

            namespace TestApp.Pages
            {
                public class BrowseEndpoint : IViewEndpoint
                {
                    public void Map(IEndpointRouteBuilder app)
                    {
                        app.MapGet("/browse", () => "browse");
                    }
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        result
            .GeneratedTrees.Should()
            .Contain(t => t.FilePath.EndsWith("ViewPages_TestApp_ProductsModule.g.cs", StringComparison.Ordinal));
    }

    [Fact]
    public void LazyImports_UseComponentPath()
    {
        var source = """
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Routing;
            using SimpleModule.Core;

            namespace TestApp
            {
                [Module("Items", ViewPrefix = "/items")]
                public class ItemsModule : IModule { }
            }

            namespace TestApp.Pages
            {
                public class CreateEndpoint : IViewEndpoint
                {
                    public void Map(IEndpointRouteBuilder app)
                    {
                        app.MapGet("/create", () => "create");
                    }
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var viewPages = GetGeneratedSource(result, "ViewPages_TestApp_ItemsModule.g.cs");

        viewPages.Should().Contain("'Items/Create': () => import('./Create')");
    }

    [Fact]
    public void PagesRecord_MapsViewPageNameToComponent()
    {
        var source = """
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Routing;
            using SimpleModule.Core;

            namespace TestApp
            {
                [Module("Orders", ViewPrefix = "/orders")]
                public class OrdersModule : IModule { }
            }

            namespace TestApp.Pages
            {
                public class DetailEndpoint : IViewEndpoint
                {
                    public void Map(IEndpointRouteBuilder app)
                    {
                        app.MapGet("/{id}", () => "detail");
                    }
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var viewPages = GetGeneratedSource(result, "ViewPages_TestApp_OrdersModule.g.cs");

        viewPages.Should().Contain("export const pages: Record<string, any> = {");
        viewPages.Should().Contain("'Orders/Detail': () => import('./Detail')");
    }

    [Fact]
    public void MultipleViews_AllAppearInPagesRecord()
    {
        var source = """
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Routing;
            using SimpleModule.Core;

            namespace TestApp
            {
                [Module("Test", ViewPrefix = "/test")]
                public class TestModule : IModule { }
            }

            namespace TestApp.Pages
            {
                public class BrowseEndpoint : IViewEndpoint
                {
                    public void Map(IEndpointRouteBuilder app)
                    {
                        app.MapGet("/browse", () => "browse");
                    }
                }

                public class CreateEndpoint : IViewEndpoint
                {
                    public void Map(IEndpointRouteBuilder app)
                    {
                        app.MapGet("/create", () => "create");
                    }
                }

                public class EditEndpoint : IViewEndpoint
                {
                    public void Map(IEndpointRouteBuilder app)
                    {
                        app.MapGet("/edit", () => "edit");
                    }
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var viewPages = GetGeneratedSource(result, "ViewPages_TestApp_TestModule.g.cs");

        viewPages.Should().Contain("'Test/Browse': () => import('./Browse')");
        viewPages.Should().Contain("'Test/Create': () => import('./Create')");
        viewPages.Should().Contain("'Test/Edit': () => import('./Edit')");
    }

    [Fact]
    public void Output_WrappedInSimpleModuleTsDirective()
    {
        var source = """
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Routing;
            using SimpleModule.Core;

            namespace TestApp
            {
                [Module("Test", ViewPrefix = "/test")]
                public class TestModule : IModule { }
            }

            namespace TestApp.Pages
            {
                public class IndexEndpoint : IViewEndpoint
                {
                    public void Map(IEndpointRouteBuilder app)
                    {
                        app.MapGet("/", () => "index");
                    }
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var viewPages = GetGeneratedSource(result, "ViewPages_TestApp_TestModule.g.cs");

        viewPages.Should().Contain("#if SIMPLEMODULE_TS");
        viewPages.Should().Contain("/*");
        viewPages.Should().Contain("*/");
        viewPages.Should().Contain("#endif");
    }

    [Fact]
    public void ViewClassName_WithViewSuffix_StripsViewSuffix()
    {
        var source = """
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Routing;
            using SimpleModule.Core;

            namespace TestApp
            {
                [Module("Test", ViewPrefix = "/test")]
                public class TestModule : IModule { }
            }

            namespace TestApp.Pages
            {
                public class DetailView : IViewEndpoint
                {
                    public void Map(IEndpointRouteBuilder app)
                    {
                        app.MapGet("/{id}", () => "detail");
                    }
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var viewPages = GetGeneratedSource(result, "ViewPages_TestApp_TestModule.g.cs");

        viewPages.Should().Contain("'Test/Detail': () => import('./Detail')");
    }

    [Fact]
    public void SameSimpleName_DifferentNamespaces_DoNotCollide()
    {
        // Two module classes with the same simple name ("ProductsModule") in different
        // namespaces must produce distinct ViewPages hint names, or AddSource throws a
        // duplicate-hint-name ArgumentException and the whole generator fails.
        var source = """
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Routing;
            using SimpleModule.Core;

            namespace Alpha
            {
                [Module("AlphaProducts", ViewPrefix = "/alpha")]
                public class ProductsModule : IModule { }
            }

            namespace Alpha.Pages
            {
                public class BrowseEndpoint : IViewEndpoint
                {
                    public void Map(IEndpointRouteBuilder app) => app.MapGet("/browse", () => "a");
                }
            }

            namespace Beta
            {
                [Module("BetaProducts", ViewPrefix = "/beta")]
                public class ProductsModule : IModule { }
            }

            namespace Beta.Pages
            {
                public class BrowseEndpoint : IViewEndpoint
                {
                    public void Map(IEndpointRouteBuilder app) => app.MapGet("/browse", () => "b");
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        // Both ViewPages files are emitted with distinct, namespace-qualified hint names.
        result
            .GeneratedTrees.Should()
            .Contain(t =>
                t.FilePath.EndsWith("ViewPages_Alpha_ProductsModule.g.cs", StringComparison.Ordinal)
            )
            .And.Contain(t =>
                t.FilePath.EndsWith("ViewPages_Beta_ProductsModule.g.cs", StringComparison.Ordinal)
            );
    }

    private static string GetGeneratedSource(
        Microsoft.CodeAnalysis.GeneratorDriverRunResult result,
        string fileName
    )
    {
        return result
            .GeneratedTrees.First(t => t.FilePath.EndsWith(fileName, StringComparison.Ordinal))
            .GetText()
            .ToString();
    }
}
