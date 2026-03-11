using FluentAssertions;
using SimpleModule.Generator.Tests.Helpers;

namespace SimpleModule.Generator.Tests;

public class DtoJsonResolverGenerationTests
{
    [Fact]
    public void DtoWithProperties_ResolverContainsGettersAndSetters()
    {
        var source = """
            using SimpleModule.Core;
            using Microsoft.Extensions.DependencyInjection;

            namespace TestApp;

            [Module("Test")]
            public class TestModule : IModule
            {
                public void ConfigureServices(IServiceCollection services) { }
            }

            [Dto]
            public class ItemDto
            {
                public int Id { get; set; }
                public string Title { get; set; } = "";
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var resolver = result
            .GeneratedTrees.First(t =>
                t.FilePath.EndsWith("ModulesJsonResolver.g.cs", StringComparison.Ordinal)
            )
            .GetText()
            .ToString();

        resolver.Should().Contain("prop_Id");
        resolver.Should().Contain("prop_Title");
        resolver.Should().Contain(".Get =");
        resolver.Should().Contain(".Set =");
    }

    [Fact]
    public void DtoWithReadOnlyProperty_ResolverHasGetterButNoSetter()
    {
        var source = """
            using SimpleModule.Core;
            using Microsoft.Extensions.DependencyInjection;

            namespace TestApp;

            [Module("Test")]
            public class TestModule : IModule
            {
                public void ConfigureServices(IServiceCollection services) { }
            }

            [Dto]
            public class ReadOnlyDto
            {
                public int Id { get; }
                public string Name { get; set; } = "";
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var resolver = result
            .GeneratedTrees.First(t =>
                t.FilePath.EndsWith("ModulesJsonResolver.g.cs", StringComparison.Ordinal)
            )
            .GetText()
            .ToString();

        // Id has getter but no setter
        resolver.Should().Contain("prop_Id");
        resolver.Should().Contain("prop_Id.Get =");
        // Name has both
        resolver.Should().Contain("prop_Name.Set =");
    }

    [Fact]
    public void MultipleDtos_ResolverHandlesAllTypes()
    {
        var source = """
            using SimpleModule.Core;
            using Microsoft.Extensions.DependencyInjection;

            namespace TestApp;

            [Module("Test")]
            public class TestModule : IModule
            {
                public void ConfigureServices(IServiceCollection services) { }
            }

            [Dto]
            public class DtoA
            {
                public int Id { get; set; }
            }

            [Dto]
            public class DtoB
            {
                public string Value { get; set; } = "";
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var resolver = result
            .GeneratedTrees.First(t =>
                t.FilePath.EndsWith("ModulesJsonResolver.g.cs", StringComparison.Ordinal)
            )
            .GetText()
            .ToString();

        resolver.Should().Contain("Create_TestApp_DtoA");
        resolver.Should().Contain("Create_TestApp_DtoB");
    }

    [Fact]
    public void DtoPropertyNames_AreCamelCasedInJson()
    {
        var source = """
            using SimpleModule.Core;
            using Microsoft.Extensions.DependencyInjection;

            namespace TestApp;

            [Module("Test")]
            public class TestModule : IModule
            {
                public void ConfigureServices(IServiceCollection services) { }
            }

            [Dto]
            public class CamelCaseDto
            {
                public int ProductId { get; set; }
                public string FirstName { get; set; } = "";
                public decimal TotalAmount { get; set; }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var resolver = GetResolver(result);

        // PascalCase properties should become camelCase JSON names
        resolver.Should().Contain("\"productId\"");
        resolver.Should().Contain("\"firstName\"");
        resolver.Should().Contain("\"totalAmount\"");
        // Should NOT contain PascalCase as JSON names
        resolver.Should().NotContain("\"ProductId\"");
        resolver.Should().NotContain("\"FirstName\"");
    }

    [Fact]
    public void DtoWithStaticProperties_IgnoresStaticMembers()
    {
        var source = """
            using SimpleModule.Core;
            using Microsoft.Extensions.DependencyInjection;

            namespace TestApp;

            [Module("Test")]
            public class TestModule : IModule
            {
                public void ConfigureServices(IServiceCollection services) { }
            }

            [Dto]
            public class WithStaticDto
            {
                public static int Counter { get; set; }
                public int Id { get; set; }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var resolver = GetResolver(result);

        resolver.Should().Contain("prop_Id");
        resolver.Should().NotContain("prop_Counter");
    }

    [Fact]
    public void DtoWithNoPublicProperties_GeneratesEmptyResolver()
    {
        var source = """
            using SimpleModule.Core;
            using Microsoft.Extensions.DependencyInjection;

            namespace TestApp;

            [Module("Test")]
            public class TestModule : IModule
            {
                public void ConfigureServices(IServiceCollection services) { }
            }

            [Dto]
            public class EmptyDto
            {
                private int _id;
                internal string Name { get; set; } = "";
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var resolver = GetResolver(result);

        resolver.Should().Contain("Create_TestApp_EmptyDto");
        resolver.Should().Contain("info.CreateObject");
        // No properties should be added
        resolver.Should().NotContain("prop_");
    }

    [Fact]
    public void DtoInNestedNamespace_UsesSafeNameWithUnderscores()
    {
        var source = """
            using SimpleModule.Core;
            using Microsoft.Extensions.DependencyInjection;

            namespace MyCompany.App.Models;

            [Module("Test")]
            public class TestModule : IModule
            {
                public void ConfigureServices(IServiceCollection services) { }
            }

            [Dto]
            public class OrderDto
            {
                public int Id { get; set; }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var resolver = GetResolver(result);

        resolver.Should().Contain("Create_MyCompany_App_Models_OrderDto");
        resolver.Should().Contain("global::MyCompany.App.Models.OrderDto");
    }

    [Fact]
    public void DtoWithNullableProperty_IncludesNullableType()
    {
        var source = """
            using SimpleModule.Core;
            using Microsoft.Extensions.DependencyInjection;

            namespace TestApp;

            [Module("Test")]
            public class TestModule : IModule
            {
                public void ConfigureServices(IServiceCollection services) { }
            }

            [Dto]
            public class NullableDto
            {
                public int? OptionalId { get; set; }
                public string? OptionalName { get; set; }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var resolver = GetResolver(result);

        resolver.Should().Contain("prop_OptionalId");
        resolver.Should().Contain("prop_OptionalName");
    }

    [Fact]
    public void DtoWithoutModules_DoesNotGenerateResolver()
    {
        var source = """
            using SimpleModule.Core;

            namespace TestApp;

            [Dto]
            public class OrphanDto
            {
                public int Id { get; set; }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        // Without any modules, nothing should be generated
        result.GeneratedTrees.Should().BeEmpty();
    }

    [Fact]
    public void Resolver_ImplementsIJsonTypeInfoResolver()
    {
        var source = """
            using SimpleModule.Core;
            using Microsoft.Extensions.DependencyInjection;

            namespace TestApp;

            [Module("Test")]
            public class TestModule : IModule
            {
                public void ConfigureServices(IServiceCollection services) { }
            }

            [Dto]
            public class SimpleDto
            {
                public int Id { get; set; }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var resolver = GetResolver(result);

        resolver
            .Should()
            .Contain("public sealed class ModulesJsonResolver : IJsonTypeInfoResolver");
        resolver.Should().Contain("public static readonly ModulesJsonResolver Instance = new()");
        resolver
            .Should()
            .Contain("public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)");
    }

    [Fact]
    public void Resolver_ReturnsNullForUnknownType()
    {
        var source = """
            using SimpleModule.Core;
            using Microsoft.Extensions.DependencyInjection;

            namespace TestApp;

            [Module("Test")]
            public class TestModule : IModule
            {
                public void ConfigureServices(IServiceCollection services) { }
            }

            [Dto]
            public class KnownDto
            {
                public int Id { get; set; }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var resolver = GetResolver(result);

        // The method should have a return null at the end for unknown types
        resolver.Should().Contain("return null;");
    }

    private static string GetResolver(Microsoft.CodeAnalysis.GeneratorDriverRunResult result) =>
        result
            .GeneratedTrees.First(t =>
                t.FilePath.EndsWith("ModulesJsonResolver.g.cs", StringComparison.Ordinal)
            )
            .GetText()
            .ToString();
}
