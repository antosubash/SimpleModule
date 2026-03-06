using System.Collections.Generic;
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
        var compilationProvider = context.CompilationProvider;

        context.RegisterSourceOutput(
            compilationProvider,
            static (spc, compilation) =>
            {
                var moduleAttributeSymbol = compilation.GetTypeByMetadataName(
                    "SimpleModule.Core.ModuleAttribute"
                );
                var dtoAttributeSymbol = compilation.GetTypeByMetadataName(
                    "SimpleModule.Core.DtoAttribute"
                );
                if (moduleAttributeSymbol is null)
                    return;

                var modules = new List<ModuleInfo>();

                foreach (var reference in compilation.References)
                {
                    if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assemblySymbol)
                        continue;

                    FindModuleTypes(assemblySymbol.GlobalNamespace, moduleAttributeSymbol, modules);
                }

                FindModuleTypes(compilation.Assembly.GlobalNamespace, moduleAttributeSymbol, modules);

                if (modules.Count == 0)
                    return;

                var dtoTypes = new List<DtoTypeInfo>();
                if (dtoAttributeSymbol is not null)
                {
                    foreach (var reference in compilation.References)
                    {
                        if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assemblySymbol)
                            continue;

                        FindDtoTypes(assemblySymbol.GlobalNamespace, dtoAttributeSymbol, dtoTypes);
                    }

                    FindDtoTypes(compilation.Assembly.GlobalNamespace, dtoAttributeSymbol, dtoTypes);
                }

                GenerateModuleExtensions(spc, modules, dtoTypes.Count > 0);
                GenerateEndpointExtensions(spc, modules);

                if (dtoTypes.Count > 0)
                    GenerateJsonResolver(spc, dtoTypes);
            }
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
                    if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, moduleAttributeSymbol))
                    {
                        modules.Add(new ModuleInfo
                        {
                            FullyQualifiedName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                            HasConfigureServices = DeclaresMethod(typeSymbol, "ConfigureServices"),
                            HasConfigureEndpoints = DeclaresMethod(typeSymbol, "ConfigureEndpoints"),
                        });
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
                    if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, dtoAttributeSymbol))
                    {
                        var fqn = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        var safeName = fqn.Replace("global::", "").Replace(".", "_");

                        var properties = new List<DtoPropertyInfo>();
                        foreach (var m in typeSymbol.GetMembers())
                        {
                            if (m is IPropertySymbol prop
                                && prop.DeclaredAccessibility == Accessibility.Public
                                && !prop.IsStatic
                                && !prop.IsIndexer
                                && prop.GetMethod is not null)
                            {
                                properties.Add(new DtoPropertyInfo
                                {
                                    Name = prop.Name,
                                    TypeFqn = prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                                    HasSetter = prop.SetMethod is not null
                                        && prop.SetMethod.DeclaredAccessibility == Accessibility.Public,
                                });
                            }
                        }

                        dtoTypes.Add(new DtoTypeInfo
                        {
                            FullyQualifiedName = fqn,
                            SafeName = safeName,
                            Properties = properties,
                        });
                        break;
                    }
                }
            }
        }
    }

    private static void GenerateModuleExtensions(
        SourceProductionContext context,
        List<ModuleInfo> modules,
        bool hasDtoTypes
    )
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#pragma warning disable IL2026");
        sb.AppendLine("#pragma warning disable IL3050");
        sb.AppendLine("using Microsoft.AspNetCore.Http.Json;");
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine();
        sb.AppendLine("namespace SimpleModule.Core;");
        sb.AppendLine();
        sb.AppendLine("public static class ModuleExtensions");
        sb.AppendLine("{");
        sb.AppendLine("    public static IServiceCollection AddModules(this IServiceCollection services)");
        sb.AppendLine("    {");

        foreach (var module in modules.Where(m => m.HasConfigureServices))
        {
            sb.AppendLine($"        new {module.FullyQualifiedName}().ConfigureServices(services);");
        }

        if (hasDtoTypes)
        {
            sb.AppendLine();
            sb.AppendLine("        services.ConfigureHttpJsonOptions(options =>");
            sb.AppendLine("        {");
            sb.AppendLine("            options.SerializerOptions.TypeInfoResolver = System.Text.Json.Serialization.Metadata.JsonTypeInfoResolver.Combine(");
            sb.AppendLine("                ModulesJsonResolver.Instance,");
            sb.AppendLine("                new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver());");
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
        List<ModuleInfo> modules
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

        foreach (var module in modules.Where(m => m.HasConfigureEndpoints))
        {
            sb.AppendLine($"        new {module.FullyQualifiedName}().ConfigureEndpoints(app);");
        }

        sb.AppendLine("        return app;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource("EndpointExtensions.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static void GenerateJsonResolver(
        SourceProductionContext context,
        List<DtoTypeInfo> dtoTypes
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
        sb.AppendLine("    public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)");
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
            sb.AppendLine($"    private static JsonTypeInfo Create_{dto.SafeName}(JsonSerializerOptions options)");
            sb.AppendLine("    {");
            sb.AppendLine($"        var info = JsonTypeInfo.CreateJsonTypeInfo<{dto.FullyQualifiedName}>(options);");
            sb.AppendLine($"        info.CreateObject = static () => new {dto.FullyQualifiedName}();");

            foreach (var prop in dto.Properties)
            {
                sb.AppendLine($"        var prop_{prop.Name} = info.CreateJsonPropertyInfo(typeof({prop.TypeFqn}), \"{prop.Name}\");");
                sb.AppendLine($"        prop_{prop.Name}.Get = static obj => (({dto.FullyQualifiedName})obj).{prop.Name};");

                if (prop.HasSetter)
                {
                    sb.AppendLine($"        prop_{prop.Name}.Set = static (obj, val) => (({dto.FullyQualifiedName})obj).{prop.Name} = ({prop.TypeFqn})val!;");
                }

                sb.AppendLine($"        info.Properties.Add(prop_{prop.Name});");
            }

            sb.AppendLine("        return info;");
            sb.AppendLine("    }");
        }

        sb.AppendLine("}");

        context.AddSource("ModulesJsonResolver.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private class ModuleInfo
    {
        public string FullyQualifiedName { get; set; } = "";
        public bool HasConfigureServices { get; set; }
        public bool HasConfigureEndpoints { get; set; }
    }

    private class DtoTypeInfo
    {
        public string FullyQualifiedName { get; set; } = "";
        public string SafeName { get; set; } = "";
        public List<DtoPropertyInfo> Properties { get; set; } = new();
    }

    private class DtoPropertyInfo
    {
        public string Name { get; set; } = "";
        public string TypeFqn { get; set; } = "";
        public bool HasSetter { get; set; }
    }
}
