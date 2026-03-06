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
            .GeneratedTrees.First(t => t.FilePath.EndsWith("ModulesJsonResolver.g.cs", StringComparison.Ordinal))
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
            .GeneratedTrees.First(t => t.FilePath.EndsWith("ModulesJsonResolver.g.cs", StringComparison.Ordinal))
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
            .GeneratedTrees.First(t => t.FilePath.EndsWith("ModulesJsonResolver.g.cs", StringComparison.Ordinal))
            .GetText()
            .ToString();

        resolver.Should().Contain("Create_TestApp_DtoA");
        resolver.Should().Contain("Create_TestApp_DtoB");
    }
}
