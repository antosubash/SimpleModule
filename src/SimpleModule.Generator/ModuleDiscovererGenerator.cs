using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace SimpleModule.Generator;

[Generator]
public class ModuleDiscovererGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Extract an equatable data model from the compilation so the incremental
        // pipeline can cache results and skip re-generation when nothing changes.
        var dataProvider = context.CompilationProvider.Select(
            static (compilation, _) => ExtractDiscoveryData(compilation)
        );

        context.RegisterSourceOutput(
            dataProvider,
            static (spc, data) =>
            {
                if (data.Modules.Length == 0)
                    return;

                GenerateModuleExtensions(spc, data.Modules, data.DtoTypes.Length > 0);
                GenerateEndpointExtensions(spc, data.Modules);
                GenerateRazorComponentExtensions(spc, data.Modules);

                if (data.DtoTypes.Length > 0)
                    GenerateJsonResolver(spc, data.DtoTypes);
            }
        );
    }

    private static DiscoveryData ExtractDiscoveryData(Compilation compilation)
    {
        var moduleAttributeSymbol = compilation.GetTypeByMetadataName(
            "SimpleModule.Core.ModuleAttribute"
        );
        if (moduleAttributeSymbol is null)
            return DiscoveryData.Empty;

        var dtoAttributeSymbol = compilation.GetTypeByMetadataName(
            "SimpleModule.Core.DtoAttribute"
        );

        var modules = new List<ModuleInfo>();

        foreach (var reference in compilation.References)
        {
            if (
                compilation.GetAssemblyOrModuleSymbol(reference)
                is not IAssemblySymbol assemblySymbol
            )
                continue;

            FindModuleTypes(assemblySymbol.GlobalNamespace, moduleAttributeSymbol, modules);
        }

        FindModuleTypes(
            compilation.Assembly.GlobalNamespace,
            moduleAttributeSymbol,
            modules
        );

        if (modules.Count == 0)
            return DiscoveryData.Empty;

        var dtoTypes = new List<DtoTypeInfo>();
        if (dtoAttributeSymbol is not null)
        {
            foreach (var reference in compilation.References)
            {
                if (
                    compilation.GetAssemblyOrModuleSymbol(reference)
                    is not IAssemblySymbol assemblySymbol
                )
                    continue;

                FindDtoTypes(assemblySymbol.GlobalNamespace, dtoAttributeSymbol, dtoTypes);
            }

            FindDtoTypes(
                compilation.Assembly.GlobalNamespace,
                dtoAttributeSymbol,
                dtoTypes
            );
        }

        var componentBaseSymbol = compilation.GetTypeByMetadataName(
            "Microsoft.AspNetCore.Components.ComponentBase"
        );
        if (componentBaseSymbol is not null)
        {
            var assembliesWithComponents =
                new HashSet<IAssemblySymbol>(SymbolEqualityComparer.Default);
            foreach (var reference in compilation.References)
            {
                if (
                    compilation.GetAssemblyOrModuleSymbol(reference)
                    is not IAssemblySymbol asm
                )
                    continue;

                if (HasComponentBaseDescendant(asm.GlobalNamespace, componentBaseSymbol))
                    assembliesWithComponents.Add(asm);
            }

            foreach (var module in modules)
            {
                var metadataName = module.FullyQualifiedName.Replace("global::", "");
                var typeSymbol = compilation.GetTypeByMetadataName(metadataName);
                if (
                    typeSymbol is not null
                    && assembliesWithComponents.Contains(typeSymbol.ContainingAssembly)
                )
                    module.HasRazorComponents = true;
            }
        }

        return new DiscoveryData(
            modules
                .Select(m => new ModuleInfoRecord(
                    m.FullyQualifiedName,
                    m.HasConfigureServices,
                    m.HasConfigureEndpoints,
                    m.HasRazorComponents
                ))
                .ToImmutableArray(),
            dtoTypes
                .Select(d => new DtoTypeInfoRecord(
                    d.FullyQualifiedName,
                    d.SafeName,
                    d.Properties
                        .Select(p => new DtoPropertyInfoRecord(p.Name, p.TypeFqn, p.HasSetter))
                        .ToImmutableArray()
                ))
                .ToImmutableArray()
        );
    }

    private static void FindModuleTypes(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol moduleAttributeSymbol,
        List<ModuleInfo> modules
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
                    if (
                        SymbolEqualityComparer.Default.Equals(
                            attr.AttributeClass,
                            moduleAttributeSymbol
                        )
                    )
                    {
                        modules.Add(
                            new ModuleInfo
                            {
                                FullyQualifiedName = typeSymbol.ToDisplayString(
                                    SymbolDisplayFormat.FullyQualifiedFormat
                                ),
                                HasConfigureServices = DeclaresMethod(
                                    typeSymbol,
                                    "ConfigureServices"
                                ),
                                HasConfigureEndpoints = DeclaresMethod(
                                    typeSymbol,
                                    "ConfigureEndpoints"
                                ),
                            }
                        );
                        break;
                    }
                }
            }
        }
    }

    private static bool DeclaresMethod(INamedTypeSymbol typeSymbol, string methodName)
    {
        foreach (var member in typeSymbol.GetMembers(methodName))
        {
            if (member is IMethodSymbol)
                return true;
        }
        return false;
    }

    private static void FindDtoTypes(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol dtoAttributeSymbol,
        List<DtoTypeInfo> dtoTypes
    )
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            if (member is INamespaceSymbol childNamespace)
            {
                FindDtoTypes(childNamespace, dtoAttributeSymbol, dtoTypes);
            }
            else if (member is INamedTypeSymbol typeSymbol)
            {
                foreach (var attr in typeSymbol.GetAttributes())
                {
                    if (
                        SymbolEqualityComparer.Default.Equals(
                            attr.AttributeClass,
                            dtoAttributeSymbol
                        )
                    )
                    {
                        var fqn = typeSymbol.ToDisplayString(
                            SymbolDisplayFormat.FullyQualifiedFormat
                        );
                        var safeName = fqn.Replace("global::", "").Replace(".", "_");

                        var properties = new List<DtoPropertyInfo>();
                        foreach (var m in typeSymbol.GetMembers())
                        {
                            if (
                                m is IPropertySymbol prop
                                && prop.DeclaredAccessibility == Accessibility.Public
                                && !prop.IsStatic
                                && !prop.IsIndexer
                                && prop.GetMethod is not null
                            )
                            {
                                properties.Add(
                                    new DtoPropertyInfo
                                    {
                                        Name = prop.Name,
                                        TypeFqn = prop.Type.ToDisplayString(
                                            SymbolDisplayFormat.FullyQualifiedFormat
                                        ),
                                        HasSetter =
                                            prop.SetMethod is not null
                                            && prop.SetMethod.DeclaredAccessibility
                                                == Accessibility.Public,
                                    }
                                );
                            }
                        }

                        dtoTypes.Add(
                            new DtoTypeInfo
                            {
                                FullyQualifiedName = fqn,
                                SafeName = safeName,
                                Properties = properties,
                            }
                        );
                        break;
                    }
                }
            }
        }
    }

    private static void GenerateModuleExtensions(
        SourceProductionContext context,
        ImmutableArray<ModuleInfoRecord> modules,
        bool hasDtoTypes
    )
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#pragma warning disable IL2026");
        sb.AppendLine("#pragma warning disable IL3050");
        sb.AppendLine("using Microsoft.AspNetCore.Http.Json;");
        sb.AppendLine("using Microsoft.Extensions.Configuration;");
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine();
        sb.AppendLine("namespace SimpleModule.Core;");
        sb.AppendLine();
        sb.AppendLine("public static class ModuleExtensions");
        sb.AppendLine("{");

        // Generate shared module instances
        foreach (var module in modules)
        {
            var fieldName = GetModuleFieldName(module.FullyQualifiedName);
            sb.AppendLine(
                $"    internal static readonly {module.FullyQualifiedName} {fieldName} = new();"
            );
        }

        sb.AppendLine();
        sb.AppendLine(
            "    public static IServiceCollection AddModules(this IServiceCollection services, IConfiguration configuration)"
        );
        sb.AppendLine("    {");

        foreach (var module in modules.Where(m => m.HasConfigureServices))
        {
            var fieldName = GetModuleFieldName(module.FullyQualifiedName);
            sb.AppendLine(
                $"        {fieldName}.ConfigureServices(services, configuration);"
            );
        }

        if (hasDtoTypes)
        {
            sb.AppendLine();
            sb.AppendLine("        services.ConfigureHttpJsonOptions(options =>");
            sb.AppendLine("        {");
            sb.AppendLine(
                "            options.SerializerOptions.TypeInfoResolver = System.Text.Json.Serialization.Metadata.JsonTypeInfoResolver.Combine("
            );
            sb.AppendLine("                ModulesJsonResolver.Instance,");
            sb.AppendLine(
                "                new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver());"
            );
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
        ImmutableArray<ModuleInfoRecord> modules
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
        sb.AppendLine(
            "    public static WebApplication MapModuleEndpoints(this WebApplication app)"
        );
        sb.AppendLine("    {");

        foreach (var module in modules.Where(m => m.HasConfigureEndpoints))
        {
            var fieldName = GetModuleFieldName(module.FullyQualifiedName);
            sb.AppendLine($"        ModuleExtensions.{fieldName}.ConfigureEndpoints(app);");
        }

        sb.AppendLine("        return app;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource(
            "EndpointExtensions.g.cs",
            SourceText.From(sb.ToString(), Encoding.UTF8)
        );
    }

    private static void GenerateJsonResolver(
        SourceProductionContext context,
        ImmutableArray<DtoTypeInfoRecord> dtoTypes
    )
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("#pragma warning disable IL2026");
        sb.AppendLine("#pragma warning disable IL3050");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Text.Json;");
        sb.AppendLine("using System.Text.Json.Serialization.Metadata;");
        sb.AppendLine();
        sb.AppendLine("namespace SimpleModule.Core;");
        sb.AppendLine();
        sb.AppendLine("public sealed class ModulesJsonResolver : IJsonTypeInfoResolver");
        sb.AppendLine("{");
        sb.AppendLine("    public static readonly ModulesJsonResolver Instance = new();");
        sb.AppendLine();
        sb.AppendLine(
            "    public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)"
        );
        sb.AppendLine("    {");

        foreach (var dto in dtoTypes)
        {
            sb.AppendLine($"        if (type == typeof({dto.FullyQualifiedName}))");
            sb.AppendLine($"            return Create_{dto.SafeName}(options);");
        }

        sb.AppendLine("        return null;");
        sb.AppendLine("    }");

        foreach (var dto in dtoTypes)
        {
            sb.AppendLine();
            sb.AppendLine(
                $"    private static JsonTypeInfo Create_{dto.SafeName}(JsonSerializerOptions options)"
            );
            sb.AppendLine("    {");
            sb.AppendLine(
                $"        var info = JsonTypeInfo.CreateJsonTypeInfo<{dto.FullyQualifiedName}>(options);"
            );
            sb.AppendLine(
                $"        info.CreateObject = static () => new {dto.FullyQualifiedName}();"
            );

            foreach (var prop in dto.Properties)
            {
                var jsonName = char.ToLowerInvariant(prop.Name[0]) + prop.Name.Substring(1);
                sb.AppendLine(
                    $"        var prop_{prop.Name} = info.CreateJsonPropertyInfo(typeof({prop.TypeFqn}), \"{jsonName}\");"
                );
                sb.AppendLine(
                    $"        prop_{prop.Name}.Get = static obj => (({dto.FullyQualifiedName})obj).{prop.Name};"
                );

                if (prop.HasSetter)
                {
                    sb.AppendLine(
                        $"        prop_{prop.Name}.Set = static (obj, val) => (({dto.FullyQualifiedName})obj).{prop.Name} = ({prop.TypeFqn})val!;"
                    );
                }

                sb.AppendLine($"        info.Properties.Add(prop_{prop.Name});");
            }

            sb.AppendLine("        return info;");
            sb.AppendLine("    }");
        }

        sb.AppendLine("}");

        context.AddSource(
            "ModulesJsonResolver.g.cs",
            SourceText.From(sb.ToString(), Encoding.UTF8)
        );
    }

    private static bool HasComponentBaseDescendant(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol componentBaseSymbol
    )
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            if (member is INamespaceSymbol childNamespace)
            {
                if (HasComponentBaseDescendant(childNamespace, componentBaseSymbol))
                    return true;
            }
            else if (member is INamedTypeSymbol typeSymbol)
            {
                if (InheritsFrom(typeSymbol, componentBaseSymbol))
                    return true;
            }
        }
        return false;
    }

    private static bool InheritsFrom(INamedTypeSymbol typeSymbol, INamedTypeSymbol baseType)
    {
        var current = typeSymbol.BaseType;
        while (current is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
                return true;
            current = current.BaseType;
        }
        return false;
    }

    private static void GenerateRazorComponentExtensions(
        SourceProductionContext context,
        ImmutableArray<ModuleInfoRecord> modules
    )
    {
        var razorModules = modules.Where(m => m.HasRazorComponents).ToList();

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("using Microsoft.AspNetCore.Builder;");
        sb.AppendLine();
        sb.AppendLine("namespace SimpleModule.Core;");
        sb.AppendLine();
        sb.AppendLine("public static class RazorComponentExtensions");
        sb.AppendLine("{");
        sb.AppendLine(
            "    public static RazorComponentsEndpointConventionBuilder AddModuleAssemblies("
        );
        sb.AppendLine(
            "        this RazorComponentsEndpointConventionBuilder builder)"
        );
        sb.AppendLine("    {");

        if (razorModules.Count > 0)
        {
            sb.AppendLine("        builder.AddAdditionalAssemblies(");
            for (var i = 0; i < razorModules.Count; i++)
            {
                var suffix = i < razorModules.Count - 1 ? "," : ");";
                sb.AppendLine(
                    $"            typeof({razorModules[i].FullyQualifiedName}).Assembly{suffix}"
                );
            }
        }

        sb.AppendLine("        return builder;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource(
            "RazorComponentExtensions.g.cs",
            SourceText.From(sb.ToString(), Encoding.UTF8)
        );
    }

    private static string GetModuleFieldName(string fullyQualifiedName)
    {
        var name = fullyQualifiedName.Replace("global::", "").Replace(".", "_");
        return $"s_{name}";
    }

    #region Equatable data model for incremental caching

    // These record types implement value equality so the incremental generator
    // pipeline can detect when the extracted data hasn't changed and skip
    // re-generating source files.

    private readonly record struct DiscoveryData(
        ImmutableArray<ModuleInfoRecord> Modules,
        ImmutableArray<DtoTypeInfoRecord> DtoTypes
    )
    {
        public static readonly DiscoveryData Empty = new(
            ImmutableArray<ModuleInfoRecord>.Empty,
            ImmutableArray<DtoTypeInfoRecord>.Empty
        );

        public bool Equals(DiscoveryData other)
        {
            return Modules.SequenceEqual(other.Modules)
                && DtoTypes.SequenceEqual(other.DtoTypes);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                foreach (var m in Modules)
                    hash = hash * 31 + m.GetHashCode();
                foreach (var d in DtoTypes)
                    hash = hash * 31 + d.GetHashCode();
                return hash;
            }
        }
    }

    private readonly record struct ModuleInfoRecord(
        string FullyQualifiedName,
        bool HasConfigureServices,
        bool HasConfigureEndpoints,
        bool HasRazorComponents
    );

    private readonly record struct DtoTypeInfoRecord(
        string FullyQualifiedName,
        string SafeName,
        ImmutableArray<DtoPropertyInfoRecord> Properties
    )
    {
        public bool Equals(DtoTypeInfoRecord other)
        {
            return FullyQualifiedName == other.FullyQualifiedName
                && SafeName == other.SafeName
                && Properties.SequenceEqual(other.Properties);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + FullyQualifiedName.GetHashCode();
                hash = hash * 31 + SafeName.GetHashCode();
                foreach (var p in Properties)
                    hash = hash * 31 + p.GetHashCode();
                return hash;
            }
        }
    }

    private readonly record struct DtoPropertyInfoRecord(
        string Name,
        string TypeFqn,
        bool HasSetter
    );

    #endregion

    #region Mutable working types (used during symbol traversal only)

    private sealed class ModuleInfo
    {
        public string FullyQualifiedName { get; set; } = "";
        public bool HasConfigureServices { get; set; }
        public bool HasConfigureEndpoints { get; set; }
        public bool HasRazorComponents { get; set; }
    }

    private sealed class DtoTypeInfo
    {
        public string FullyQualifiedName { get; set; } = "";
        public string SafeName { get; set; } = "";
        public List<DtoPropertyInfo> Properties { get; set; } = new();
    }

    private sealed class DtoPropertyInfo
    {
        public string Name { get; set; } = "";
        public string TypeFqn { get; set; } = "";
        public bool HasSetter { get; set; }
    }

    #endregion
}
