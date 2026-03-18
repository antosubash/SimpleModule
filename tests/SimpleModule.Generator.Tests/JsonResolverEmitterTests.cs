using FluentAssertions;
using SimpleModule.Generator.Tests.Helpers;

namespace SimpleModule.Generator.Tests;

public class JsonResolverEmitterTests
{
    [Fact]
    public void Dto_WithInternalProperty_ExcludedFromResolver()
    {
        var source = """
            using SimpleModule.Core;

            namespace TestApp;

            [Module("Test")]
            public class TestModule : IModule { }

            [Dto]
            public class InternalPropDto
            {
                public int Id { get; set; }
                internal string Secret { get; set; } = "";
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var resolver = GetResolver(result);

        resolver.Should().Contain("prop_Id");
        resolver.Should().NotContain("prop_Secret");
    }

    [Fact]
    public void Resolver_DispatchesByExactTypeComparison()
    {
        var source = """
            using SimpleModule.Core;

            namespace TestApp;

            [Module("Test")]
            public class TestModule : IModule { }

            [Dto]
            public class MyDto
            {
                public int Id { get; set; }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var resolver = GetResolver(result);

        resolver.Should().Contain("if (type == typeof(global::TestApp.MyDto))");
    }

    [Fact]
    public void EachDto_GetsOwnCreateFactoryMethod()
    {
        var source = """
            using SimpleModule.Core;

            namespace TestApp;

            [Module("Test")]
            public class TestModule : IModule { }

            [Dto]
            public class AlphaDto
            {
                public int Id { get; set; }
            }

            [Dto]
            public class BetaDto
            {
                public string Name { get; set; } = "";
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var resolver = GetResolver(result);

        resolver.Should().Contain("private static JsonTypeInfo Create_TestApp_AlphaDto(JsonSerializerOptions options)");
        resolver.Should().Contain("private static JsonTypeInfo Create_TestApp_BetaDto(JsonSerializerOptions options)");
    }

    [Fact]
    public void Property_WithNoSetter_OnlyGetsGetLambda()
    {
        var source = """
            using SimpleModule.Core;

            namespace TestApp;

            [Module("Test")]
            public class TestModule : IModule { }

            [Dto]
            public class ReadOnlyPropDto
            {
                public int Id { get; }
                public string Name { get; set; } = "";
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var resolver = GetResolver(result);

        // Id has Get but no Set
        resolver.Should().Contain("prop_Id.Get =");
        // Count the Set assignments - Id should not have one
        var lines = resolver.Split('\n');
        var idSetLine = lines.Any(l => l.Contains("prop_Id.Set =", StringComparison.Ordinal));
        idSetLine.Should().BeFalse();

        // Name has both Get and Set
        resolver.Should().Contain("prop_Name.Get =");
        resolver.Should().Contain("prop_Name.Set =");
    }

    [Fact]
    public void Resolver_CreateObject_UsesNewInstance()
    {
        var source = """
            using SimpleModule.Core;

            namespace TestApp;

            [Module("Test")]
            public class TestModule : IModule { }

            [Dto]
            public class SimpleDto
            {
                public int Id { get; set; }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var resolver = GetResolver(result);

        resolver.Should().Contain("info.CreateObject = static () => new global::TestApp.SimpleDto();");
    }

    [Fact]
    public void Resolver_PropertyJsonName_IsCamelCased()
    {
        var source = """
            using SimpleModule.Core;

            namespace TestApp;

            [Module("Test")]
            public class TestModule : IModule { }

            [Dto]
            public class CamelDto
            {
                public int ProductId { get; set; }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var resolver = GetResolver(result);

        resolver.Should().Contain("\"productId\"");
        resolver.Should().NotContain("\"ProductId\"");
    }

    [Fact]
    public void Resolver_IncludesPragmaWarningDisable()
    {
        var source = """
            using SimpleModule.Core;

            namespace TestApp;

            [Module("Test")]
            public class TestModule : IModule { }

            [Dto]
            public class SimpleDto
            {
                public int Id { get; set; }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var resolver = GetResolver(result);

        resolver.Should().Contain("#pragma warning disable IL2026");
        resolver.Should().Contain("#pragma warning disable IL3050");
        resolver.Should().Contain("#nullable enable");
    }

    [Fact]
    public void Property_WithPrivateSetter_ExcludesSetLambda()
    {
        var source = """
            using SimpleModule.Core;

            namespace TestApp;

            [Module("Test")]
            public class TestModule : IModule { }

            [Dto]
            public class PrivateSetterDto
            {
                public int Id { get; private set; }
                public string Name { get; set; } = "";
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var resolver = GetResolver(result);

        // Id has Get but no Set (setter is private)
        resolver.Should().Contain("prop_Id.Get =");
        var lines = resolver.Split('\n');
        lines.Any(l => l.Contains("prop_Id.Set =", StringComparison.Ordinal)).Should().BeFalse();

        // Name has both
        resolver.Should().Contain("prop_Name.Set =");
    }

    private static string GetResolver(Microsoft.CodeAnalysis.GeneratorDriverRunResult result) =>
        result
            .GeneratedTrees.First(t =>
                t.FilePath.EndsWith("ModulesJsonResolver.g.cs", StringComparison.Ordinal)
            )
            .GetText()
            .ToString();
}
