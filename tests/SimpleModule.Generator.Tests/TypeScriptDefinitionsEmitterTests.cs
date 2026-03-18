using FluentAssertions;
using SimpleModule.Generator.Tests.Helpers;

namespace SimpleModule.Generator.Tests;

public class TypeScriptDefinitionsEmitterTests
{
    [Fact]
    public void Dto_WithStringIntBool_MapsToCorrectTsTypes()
    {
        var source = """
            using SimpleModule.Core;

            namespace TestApp.Contracts;

            [Module("TestApp")]
            public class TestAppModule : IModule { }

            [Dto]
            public class ItemDto
            {
                public string Name { get; set; } = "";
                public int Count { get; set; }
                public bool IsActive { get; set; }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var tsSource = GetGeneratedSource(result, "DtoTypeScript_Contracts.g.cs");

        tsSource.Should().Contain("name: string;");
        tsSource.Should().Contain("count: number;");
        tsSource.Should().Contain("isActive: boolean;");
    }

    [Fact]
    public void Dto_WithDateTimeGuidDateOnly_MapsToString()
    {
        var source = """
            using System;
            using SimpleModule.Core;

            namespace TestApp.Contracts;

            [Module("TestApp")]
            public class TestAppModule : IModule { }

            [Dto]
            public class DatesDto
            {
                public DateTime CreatedAt { get; set; }
                public Guid Id { get; set; }
                public DateOnly BirthDate { get; set; }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var tsSource = GetGeneratedSource(result, "DtoTypeScript_Contracts.g.cs");

        tsSource.Should().Contain("createdAt: string;");
        tsSource.Should().Contain("id: string;");
        tsSource.Should().Contain("birthDate: string;");
    }

    [Fact]
    public void Dto_WithListAndIEnumerable_MapsToArray()
    {
        var source = """
            using System.Collections.Generic;
            using SimpleModule.Core;

            namespace TestApp.Contracts;

            [Module("TestApp")]
            public class TestAppModule : IModule { }

            [Dto]
            public class CollectionDto
            {
                public List<string> Tags { get; set; } = new();
                public IEnumerable<int> Scores { get; set; } = new List<int>();
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var tsSource = GetGeneratedSource(result, "DtoTypeScript_Contracts.g.cs");

        tsSource.Should().Contain("tags: string[];");
        tsSource.Should().Contain("scores: number[];");
    }

    [Fact]
    public void Dto_WithNullableShorthandValueType_MapsToAny()
    {
        // Note: Nullable<T> via shorthand (int?) produces FQNs with nested global:: prefixes
        // that the TypeMappingHelpers does not currently resolve. This test documents that behavior.
        var source = """
            using SimpleModule.Core;

            namespace TestApp.Contracts;

            [Module("TestApp")]
            public class TestAppModule : IModule { }

            [Dto]
            public class NullableDto
            {
                public int? OptionalCount { get; set; }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var tsSource = GetGeneratedSource(result, "DtoTypeScript_Contracts.g.cs");

        // Current behavior: nullable value types via shorthand (int?) map to `any`
        // because Roslyn represents them with nested global:: prefixes that
        // MapCSharpTypeToTypeScript doesn't resolve.
        tsSource.Should().Contain("optionalCount: any;");
    }

    [Fact]
    public void Dto_ReferencingAnotherDto_UsesDtoInterfaceName()
    {
        var source = """
            using SimpleModule.Core;

            namespace TestApp.Contracts;

            [Module("TestApp")]
            public class TestAppModule : IModule { }

            [Dto]
            public class CategoryDto
            {
                public int Id { get; set; }
                public string Name { get; set; } = "";
            }

            [Dto]
            public class ProductDto
            {
                public int Id { get; set; }
                public CategoryDto Category { get; set; } = new();
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var tsSource = GetGeneratedSource(result, "DtoTypeScript_Contracts.g.cs");

        tsSource.Should().Contain("category: CategoryDto;");
        tsSource.Should().NotContain("category: any;");
    }

    [Fact]
    public void PropertyNames_AreCamelCased()
    {
        var source = """
            using SimpleModule.Core;

            namespace TestApp.Contracts;

            [Module("TestApp")]
            public class TestAppModule : IModule { }

            [Dto]
            public class CamelDto
            {
                public string FirstName { get; set; } = "";
                public int ProductId { get; set; }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var tsSource = GetGeneratedSource(result, "DtoTypeScript_Contracts.g.cs");

        tsSource.Should().Contain("firstName: string;");
        tsSource.Should().Contain("productId: number;");
        tsSource.Should().NotContain("FirstName:");
        tsSource.Should().NotContain("ProductId:");
    }

    [Fact]
    public void File_UsesSimpleModuleTsWrapper()
    {
        var source = """
            using SimpleModule.Core;

            namespace TestApp.Contracts;

            [Module("TestApp")]
            public class TestAppModule : IModule { }

            [Dto]
            public class SimpleDto
            {
                public int Id { get; set; }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var tsSource = GetGeneratedSource(result, "DtoTypeScript_Contracts.g.cs");

        tsSource.Should().Contain("#if SIMPLEMODULE_TS");
        tsSource.Should().Contain("/*");
        tsSource.Should().Contain("*/");
        tsSource.Should().Contain("#endif");
    }

    [Fact]
    public void File_IncludesModuleComment()
    {
        var source = """
            using SimpleModule.Core;

            namespace TestApp.Contracts;

            [Module("TestApp")]
            public class TestAppModule : IModule { }

            [Dto]
            public class SimpleDto
            {
                public int Id { get; set; }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var tsSource = GetGeneratedSource(result, "DtoTypeScript_Contracts.g.cs");

        tsSource.Should().Contain("// @module Contracts");
    }

    [Fact]
    public void NoDtoTypes_NoTypeScriptFileEmitted()
    {
        var source = """
            using SimpleModule.Core;

            namespace TestApp;

            [Module("Test")]
            public class TestModule : IModule { }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        result.GeneratedTrees.Should().NotContain(t => t.FilePath.Contains("DtoTypeScript_"));
    }

    [Fact]
    public void Dto_WithUnknownType_MapsToAny()
    {
        var source = """
            using SimpleModule.Core;

            namespace TestApp.Contracts;

            [Module("TestApp")]
            public class TestAppModule : IModule { }

            public class NonDtoClass
            {
                public int Value { get; set; }
            }

            [Dto]
            public class ContainerDto
            {
                public NonDtoClass Child { get; set; } = new();
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var tsSource = GetGeneratedSource(result, "DtoTypeScript_Contracts.g.cs");

        tsSource.Should().Contain("child: any;");
    }

    [Fact]
    public void Dto_GeneratesExportInterface()
    {
        var source = """
            using SimpleModule.Core;

            namespace TestApp.Contracts;

            [Module("TestApp")]
            public class TestAppModule : IModule { }

            [Dto]
            public class MyDto
            {
                public int Id { get; set; }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        var tsSource = GetGeneratedSource(result, "DtoTypeScript_Contracts.g.cs");

        tsSource.Should().Contain("export interface MyDto {");
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
