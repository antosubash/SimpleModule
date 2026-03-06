using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SimpleModule.Generator;

namespace SimpleModule.Generator.Tests.Helpers;

public static class GeneratorTestHelper
{
    public static CSharpCompilation CreateCompilation(params string[] sources)
    {
        var syntaxTrees = sources.Select(s => CSharpSyntaxTree.ParseText(s)).ToArray();

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(SimpleModule.Core.IModule).Assembly.Location),
        };

        // Add runtime references
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        references.Add(
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Runtime.dll"))
        );
        references.Add(
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Collections.dll"))
        );

        // Add ASP.NET Core references for IServiceCollection, IEndpointRouteBuilder
        var aspNetDir = Path.GetDirectoryName(
            typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection).Assembly.Location
        );
        if (aspNetDir is not null)
        {
            var diAbstractions = Path.Combine(
                aspNetDir,
                "Microsoft.Extensions.DependencyInjection.Abstractions.dll"
            );
            if (File.Exists(diAbstractions))
                references.Add(MetadataReference.CreateFromFile(diAbstractions));
        }

        return CSharpCompilation.Create(
            "TestAssembly",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );
    }

    public static GeneratorDriverRunResult RunGenerator(CSharpCompilation compilation)
    {
        var generator = new ModuleDiscovererGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

        return driver.GetRunResult();
    }
}
