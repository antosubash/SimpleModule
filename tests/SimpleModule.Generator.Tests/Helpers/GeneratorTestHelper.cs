using System.Collections.Immutable;
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

        // Add generic collections reference (Dictionary<,> may not be type-forwarded from System.Runtime on all platforms)
        references.Add(
            MetadataReference.CreateFromFile(
                typeof(System.Collections.Generic.Dictionary<,>).Assembly.Location
            )
        );

        // Add ASP.NET Core references for IServiceCollection, IEndpointRouteBuilder, IConfiguration
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

        references.Add(
            MetadataReference.CreateFromFile(
                typeof(Microsoft.Extensions.Configuration.IConfiguration).Assembly.Location
            )
        );

        var configAbstractionsPath = Path.Combine(
            Path.GetDirectoryName(
                typeof(Microsoft.Extensions.Configuration.IConfiguration).Assembly.Location
            )!,
            "Microsoft.Extensions.Configuration.Abstractions.dll"
        );
        if (File.Exists(configAbstractionsPath))
            references.Add(MetadataReference.CreateFromFile(configAbstractionsPath));

        // Add ASP.NET Core HTTP abstractions (for IResult)
        references.Add(
            MetadataReference.CreateFromFile(
                typeof(Microsoft.AspNetCore.Http.IResult).Assembly.Location
            )
        );

        // Add System.Threading.Tasks for Task<T>
        var tasksPath = Path.Combine(runtimeDir, "System.Threading.Tasks.dll");
        if (File.Exists(tasksPath))
            references.Add(MetadataReference.CreateFromFile(tasksPath));

        return CSharpCompilation.Create(
            "TestAssembly",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );
    }

    public static CSharpCompilation CreateCompilationWithAssemblyName(
        string assemblyName,
        params string[] sources
    )
    {
        var compilation = CreateCompilation(sources);
        return compilation.WithAssemblyName(assemblyName);
    }

    public static CSharpCompilation CreateCompilationWithEfCore(params string[] sources)
    {
        var compilation = CreateCompilation(sources);

        var efCoreReferences = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(
                typeof(Microsoft.EntityFrameworkCore.DbContext).Assembly.Location
            ),
            MetadataReference.CreateFromFile(
                typeof(Microsoft.EntityFrameworkCore.DbSet<>).Assembly.Location
            ),
            MetadataReference.CreateFromFile(
                typeof(Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext<,,>)
                    .Assembly
                    .Location
            ),
            MetadataReference.CreateFromFile(
                typeof(Microsoft.AspNetCore.Identity.IdentityUser).Assembly.Location
            ),
            MetadataReference.CreateFromFile(
                typeof(Microsoft.Extensions.Options.IOptions<>).Assembly.Location
            ),
            MetadataReference.CreateFromFile(
                typeof(SimpleModule.Database.DatabaseOptions).Assembly.Location
            ),
        };

        // Add EF Core abstractions assembly if separate
        var efCoreDir = Path.GetDirectoryName(
            typeof(Microsoft.EntityFrameworkCore.DbContext).Assembly.Location
        )!;
        var efAbstractions = Path.Combine(
            efCoreDir,
            "Microsoft.EntityFrameworkCore.Abstractions.dll"
        );
        if (File.Exists(efAbstractions))
            efCoreReferences.Add(MetadataReference.CreateFromFile(efAbstractions));

        // Add Microsoft.Extensions.Identity.Stores for IdentityUser<TKey>
        var identityStoresPath = Path.Combine(
            efCoreDir,
            "Microsoft.Extensions.Identity.Stores.dll"
        );
        if (File.Exists(identityStoresPath))
            efCoreReferences.Add(MetadataReference.CreateFromFile(identityStoresPath));

        return compilation.AddReferences(efCoreReferences);
    }

    public static CSharpCompilation CreateEfCoreCompilationWithAssemblyName(
        string assemblyName,
        params string[] sources
    )
    {
        return CreateCompilationWithEfCore(sources).WithAssemblyName(assemblyName);
    }

    public static GeneratorDriverRunResult RunGenerator(CSharpCompilation compilation)
    {
        var generator = new ModuleDiscovererGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

        return driver.GetRunResult();
    }

    public static (
        GeneratorDriverRunResult Result,
        ImmutableArray<Diagnostic> Diagnostics
    ) RunGeneratorWithDiagnostics(CSharpCompilation compilation)
    {
        var generator = new ModuleDiscovererGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        return (driver.GetRunResult(), diagnostics);
    }
}
