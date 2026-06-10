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

        // Add System.Security.Claims for ClaimsPrincipal (used by IPolicy<T> implementors)
        references.Add(
            MetadataReference.CreateFromFile(
                typeof(System.Security.Claims.ClaimsPrincipal).Assembly.Location
            )
        );

        return CSharpCompilation.Create(
            "TestAssembly",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );
    }

    /// <summary>
    /// Creates a compilation that includes FluentValidation and the SimpleModule.Core assembly
    /// so that FormRequest&lt;T&gt; and RuleConfigurator&lt;T&gt; are resolvable by the generator.
    /// </summary>
    public static CSharpCompilation CreateCompilationWithFormRequestSupport(params string[] sources)
    {
        var compilation = CreateCompilation(sources);

        var extraRefs = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(FluentValidation.IValidator).Assembly.Location),
            MetadataReference.CreateFromFile(
                typeof(FluentValidation.AbstractValidator<>).Assembly.Location
            ),
        };

        return compilation.AddReferences(extraRefs);
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

    /// <summary>
    /// Compiles one or more referenced assemblies in order (later sources may reference
    /// earlier ones), then a host assembly ("TestAssembly") referencing all of them.
    /// Mirrors the real layout where modules and their *.Contracts assemblies are
    /// separate compilations.
    /// </summary>
    public static CSharpCompilation CreateMultiAssemblyCompilation(
        (string AssemblyName, string Source)[] referencedAssemblies,
        string hostSource
    )
    {
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var baseRefs = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Collections.dll")),
            MetadataReference.CreateFromFile(typeof(SimpleModule.Core.IModule).Assembly.Location),
            MetadataReference.CreateFromFile(
                typeof(System.Security.Claims.ClaimsPrincipal).Assembly.Location
            ),
            MetadataReference.CreateFromFile(
                typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection)
                    .Assembly
                    .Location
            ),
            MetadataReference.CreateFromFile(
                typeof(Microsoft.Extensions.Configuration.IConfiguration).Assembly.Location
            ),
        };

        var builtRefs = new List<MetadataReference>();
        foreach (var (assemblyName, source) in referencedAssemblies)
        {
            var compilation = CSharpCompilation.Create(
                assemblyName,
                [CSharpSyntaxTree.ParseText(source)],
                [.. baseRefs, .. builtRefs],
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );

            using var ms = new MemoryStream();
            var emit = compilation.Emit(ms);
            if (!emit.Success)
            {
                var errors = string.Join(
                    ", ",
                    emit.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)
                );
                throw new InvalidOperationException(
                    $"Referenced assembly {assemblyName} failed to compile: {errors}"
                );
            }
            builtRefs.Add(MetadataReference.CreateFromImage(ms.ToArray()));
        }

        var hostRefs = new List<MetadataReference>([.. baseRefs, .. builtRefs]);
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
                hostRefs.Add(MetadataReference.CreateFromFile(diAbstractions));
        }
        hostRefs.Add(
            MetadataReference.CreateFromFile(
                typeof(Microsoft.AspNetCore.Http.IResult).Assembly.Location
            )
        );

        return CSharpCompilation.Create(
            "TestAssembly",
            [CSharpSyntaxTree.ParseText(hostSource)],
            hostRefs,
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
