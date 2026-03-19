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

            namespace TestApp.Views
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
            .Contain(t => t.FilePath.EndsWith("ViewPages_Products.g.cs", StringComparison.Ordinal));
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

            namespace TestApp.Views
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

        var viewPages = GetGeneratedSource(result, "ViewPages_Items.g.cs");

        viewPages.Should().Contain("'Items/Create': () => import('../Views/Create')");
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

            namespace TestApp.Views
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

        var viewPages = GetGeneratedSource(result, "ViewPages_Orders.g.cs");

        viewPages.Should().Contain("export const pages: Record<string, any> = {");
        viewPages.Should().Contain("'Orders/Detail': () => import('../Views/Detail')");
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

            namespace TestApp.Views
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

        var viewPages = GetGeneratedSource(result, "ViewPages_Test.g.cs");

        viewPages.Should().Contain("'Test/Browse': () => import('../Views/Browse')");
        viewPages.Should().Contain("'Test/Create': () => import('../Views/Create')");
        viewPages.Should().Contain("'Test/Edit': () => import('../Views/Edit')");
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

            namespace TestApp.Views
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

        var viewPages = GetGeneratedSource(result, "ViewPages_Test.g.cs");

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

            namespace TestApp.Views
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

        var viewPages = GetGeneratedSource(result, "ViewPages_Test.g.cs");

        viewPages.Should().Contain("'Test/Detail': () => import('../Views/Detail')");
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
