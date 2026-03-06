using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace SimpleModule.Generator;

[Generator]
public class ModuleDiscovererGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var compilationProvider = context.CompilationProvider;

        context.RegisterSourceOutput(
            compilationProvider,
            static (spc, compilation) =>
            {
                var moduleAttributeSymbol = compilation.GetTypeByMetadataName(
                    "SimpleModule.Core.ModuleAttribute"
                );
                if (moduleAttributeSymbol is null)
                    return;

                var jsonContextSymbol = compilation.GetTypeByMetadataName(
                    "System.Text.Json.Serialization.JsonSerializerContext"
                );

                var modules = new List<string>();
                var jsonContexts = new List<string>();
                var moduleAssemblies = new HashSet<IAssemblySymbol>(SymbolEqualityComparer.Default);

                // First pass: find all module types and track which assemblies contain them
                foreach (var reference in compilation.References)
                {
                    if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assemblySymbol)
                        continue;

                    var assemblyModules = new List<string>();
                    FindModuleTypes(assemblySymbol.GlobalNamespace, moduleAttributeSymbol, assemblyModules);
                    if (assemblyModules.Count > 0)
                    {
                        modules.AddRange(assemblyModules);
                        moduleAssemblies.Add(assemblySymbol);
                    }
                }

                // Check current assembly too
                var currentModules = new List<string>();
                FindModuleTypes(compilation.Assembly.GlobalNamespace, moduleAttributeSymbol, currentModules);
                if (currentModules.Count > 0)
                {
                    modules.AddRange(currentModules);
                    moduleAssemblies.Add(compilation.Assembly);
                }

                // Second pass: find JsonSerializerContext types only in module assemblies
                foreach (var assembly in moduleAssemblies)
                {
                    if (jsonContextSymbol is not null)
                        FindJsonContexts(assembly.GlobalNamespace, jsonContextSymbol, jsonContexts);
                }

                if (modules.Count == 0)
                    return;

                GenerateModuleExtensions(spc, modules, jsonContexts);
                GenerateEndpointExtensions(spc, modules);
            }
        );
    }

    private static void FindModuleTypes(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol moduleAttributeSymbol,
        List<string> modules
    )
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            if (member is INamespaceSymbol childNamespace)
            {
                FindModuleTypes(childNamespace, moduleAttributeSymbol, modules);
            }
            else if (member is INamedTypeSymbol typeSymbol)
            {
                foreach (var attr in typeSymbol.GetAttributes())
                {
                    if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, moduleAttributeSymbol))
                    {
                        modules.Add(typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                        break;
                    }
                }
            }
        }
    }

    private static void FindJsonContexts(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol jsonContextSymbol,
        List<string> jsonContexts
    )
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            if (member is INamespaceSymbol childNamespace)
            {
                FindJsonContexts(childNamespace, jsonContextSymbol, jsonContexts);
            }
            else if (member is INamedTypeSymbol typeSymbol
                && typeSymbol.DeclaredAccessibility == Accessibility.Public
                && !typeSymbol.IsAbstract
                && InheritsFrom(typeSymbol, jsonContextSymbol))
            {
                jsonContexts.Add(typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            }
        }
    }

    private static bool InheritsFrom(INamedTypeSymbol type, INamedTypeSymbol baseType)
    {
        var current = type.BaseType;
        while (current is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
                return true;
            current = current.BaseType;
        }
        return false;
    }

    private static void GenerateModuleExtensions(
        SourceProductionContext context,
        List<string> modules,
        List<string> jsonContexts
    )
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("using Microsoft.AspNetCore.Http.Json;");
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine();
        sb.AppendLine("namespace SimpleModule.Core;");
        sb.AppendLine();
        sb.AppendLine("public static class ModuleExtensions");
        sb.AppendLine("{");
        sb.AppendLine("    public static IServiceCollection AddModules(this IServiceCollection services)");
        sb.AppendLine("    {");

        foreach (var module in modules)
        {
            sb.AppendLine($"        new {module}().ConfigureServices(services);");
        }

        if (jsonContexts.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("        services.ConfigureHttpJsonOptions(options =>");
            sb.AppendLine("        {");
            sb.AppendLine("            var chain = options.SerializerOptions.TypeInfoResolverChain;");

            foreach (var ctx in jsonContexts)
            {
                sb.AppendLine($"            chain.Add({ctx}.Default);");
            }

            sb.AppendLine("        });");
        }

        sb.AppendLine();
        sb.AppendLine("        return services;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource("ModuleExtensions.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static void GenerateEndpointExtensions(
        SourceProductionContext context,
        List<string> modules
    )
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("using Microsoft.AspNetCore.Builder;");
        sb.AppendLine();
        sb.AppendLine("namespace SimpleModule.Core;");
        sb.AppendLine();
        sb.AppendLine("public static class EndpointExtensions");
        sb.AppendLine("{");
        sb.AppendLine("    public static WebApplication MapModuleEndpoints(this WebApplication app)");
        sb.AppendLine("    {");

        foreach (var module in modules)
        {
            sb.AppendLine($"        new {module}().ConfigureEndpoints(app);");
        }

        sb.AppendLine("        return app;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource("EndpointExtensions.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }
}
