using FluentAssertions;
using SimpleModule.Generator.Tests.Helpers;

namespace SimpleModule.Generator.Tests;

public class PermissionAutoDiscoveryTests
{
    [Fact]
    public void SealedPermissionClass_GeneratesAddPermissions()
    {
        var source = """
            using SimpleModule.Core;
            using SimpleModule.Core.Authorization;
            using Microsoft.Extensions.DependencyInjection;

            namespace TestApp
            {
                [Module("Products")]
                public class ProductsModule : IModule
                {
                    public void ConfigureServices(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) { }
                }

                public sealed class ProductPermissions : IModulePermissions
                {
                    public const string View = "Products.View";
                    public const string Edit = "Products.Edit";
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var moduleExt = result
            .GeneratedTrees.First(t =>
                t.FilePath.EndsWith("ModuleExtensions.g.cs", StringComparison.Ordinal)
            )
            .GetText()
            .ToString();

        moduleExt.Should().Contain("AddPermissions<");
        moduleExt.Should().Contain("ProductPermissions");
    }

    [Fact]
    public void NonSealedClass_EmitsSM0032()
    {
        var source = """
            using SimpleModule.Core;
            using SimpleModule.Core.Authorization;

            namespace TestApp
            {
                [Module("Products")]
                public class ProductsModule : IModule { }

                public class ProductPermissions : IModulePermissions
                {
                    public const string View = "Products.View";
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics.Should().Contain(d => d.Id == "SM0032");
        var diag = diagnostics.First(d => d.Id == "SM0032");
        diag.GetMessage(System.Globalization.CultureInfo.InvariantCulture)
            .Should()
            .Contain("ProductPermissions");
    }

    [Fact]
    public void NonConstField_EmitsSM0027()
    {
        var source = """
            using SimpleModule.Core;
            using SimpleModule.Core.Authorization;

            namespace TestApp
            {
                [Module("Products")]
                public class ProductsModule : IModule { }

                public sealed class ProductPermissions : IModulePermissions
                {
                    public const string View = "Products.View";
                    public static string Edit = "Products.Edit";
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics.Should().Contain(d => d.Id == "SM0027");
        var diag = diagnostics.First(d => d.Id == "SM0027");
        var message = diag.GetMessage(System.Globalization.CultureInfo.InvariantCulture);
        message.Should().Contain("ProductPermissions");
        message.Should().Contain("Edit");
    }

    [Fact]
    public void WrongNamingConvention_EmitsSM0031()
    {
        var source = """
            using SimpleModule.Core;
            using SimpleModule.Core.Authorization;

            namespace TestApp
            {
                [Module("Products")]
                public class ProductsModule : IModule { }

                public sealed class ProductPermissions : IModulePermissions
                {
                    public const string View = "ProductsView";
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics.Should().Contain(d => d.Id == "SM0031");
        var diag = diagnostics.First(d => d.Id == "SM0031");
        diag.GetMessage(System.Globalization.CultureInfo.InvariantCulture)
            .Should()
            .Contain("ProductsView");
    }

    [Fact]
    public void DuplicateValues_EmitsSM0033()
    {
        var source = """
            using SimpleModule.Core;
            using SimpleModule.Core.Authorization;

            namespace TestApp
            {
                [Module("Products")]
                public class ProductsModule : IModule { }

                public sealed class ProductPermissionsA : IModulePermissions
                {
                    public const string View = "Products.View";
                }

                public sealed class ProductPermissionsB : IModulePermissions
                {
                    public const string View = "Products.View";
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics.Should().Contain(d => d.Id == "SM0033");
        var diag = diagnostics.First(d => d.Id == "SM0033");
        var message = diag.GetMessage(System.Globalization.CultureInfo.InvariantCulture);
        message.Should().Contain("Products.View");
    }

    [Fact]
    public void WrongPrefix_EmitsSM0034()
    {
        var source = """
            using SimpleModule.Core;
            using SimpleModule.Core.Authorization;

            namespace TestApp
            {
                [Module("Products")]
                public class ProductsModule : IModule { }

                public sealed class ProductPermissions : IModulePermissions
                {
                    public const string View = "Orders.View";
                }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics.Should().Contain(d => d.Id == "SM0034");
        var diag = diagnostics.First(d => d.Id == "SM0034");
        var message = diag.GetMessage(System.Globalization.CultureInfo.InvariantCulture);
        message.Should().Contain("Orders.View");
        message.Should().Contain("Products");
    }
}
