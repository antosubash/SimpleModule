using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace SimpleModule.Generator;

[Generator]
public partial class ModuleDiscovererGenerator : IIncrementalGenerator
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
                GenerateMenuExtensions(spc, data.Modules);
                GenerateRazorComponentExtensions(spc, data.Modules);
                GenerateViewPages(spc, data.Modules);

                if (data.DtoTypes.Length > 0)
                {
                    GenerateJsonResolver(spc, data.DtoTypes);
                    GenerateTypeScriptDefinitions(spc, data.DtoTypes);
                }
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

        var endpointInterfaceSymbol = compilation.GetTypeByMetadataName(
            "SimpleModule.Core.IEndpoint"
        );

        var viewEndpointInterfaceSymbol = compilation.GetTypeByMetadataName(
            "SimpleModule.Core.IViewEndpoint"
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

        FindModuleTypes(compilation.Assembly.GlobalNamespace, moduleAttributeSymbol, modules);

        if (modules.Count == 0)
            return DiscoveryData.Empty;

        // Discover IEndpoint implementors per module assembly.
        // Classification is by interface type: IViewEndpoint → view, IEndpoint → API.
        if (endpointInterfaceSymbol is not null)
        {
            foreach (var module in modules)
            {
                var metadataName = module.FullyQualifiedName.Replace("global::", "");
                var typeSymbol = compilation.GetTypeByMetadataName(metadataName);
                if (typeSymbol is null)
                    continue;

                var assembly = typeSymbol.ContainingAssembly;
                FindEndpointTypes(
                    assembly.GlobalNamespace,
                    endpointInterfaceSymbol,
                    viewEndpointInterfaceSymbol,
                    module.ModuleName,
                    module.Endpoints,
                    module.Views
                );
            }
        }

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

            FindDtoTypes(compilation.Assembly.GlobalNamespace, dtoAttributeSymbol, dtoTypes);
        }

        var componentBaseSymbol = compilation.GetTypeByMetadataName(
            "Microsoft.AspNetCore.Components.ComponentBase"
        );
        if (componentBaseSymbol is not null)
        {
            var assembliesWithComponents = new HashSet<IAssemblySymbol>(
                SymbolEqualityComparer.Default
            );
            foreach (var reference in compilation.References)
            {
                if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol asm)
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
                    m.ModuleName,
                    m.HasConfigureServices,
                    m.HasConfigureEndpoints,
                    m.HasConfigureMenu,
                    m.HasRazorComponents,
                    m.RoutePrefix,
                    m.ViewPrefix,
                    m.Endpoints.Select(e => new EndpointInfoRecord(e.FullyQualifiedName))
                        .ToImmutableArray(),
                    m.Views.Select(v => new ViewInfoRecord(v.FullyQualifiedName, v.Page))
                        .ToImmutableArray()
                ))
                .ToImmutableArray(),
            dtoTypes
                .Select(d => new DtoTypeInfoRecord(
                    d.FullyQualifiedName,
                    d.SafeName,
                    d.Properties.Select(p => new DtoPropertyInfoRecord(
                            p.Name,
                            p.TypeFqn,
                            p.HasSetter
                        ))
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
                        var moduleName =
                            attr.ConstructorArguments.Length > 0
                                ? attr.ConstructorArguments[0].Value as string ?? ""
                                : "";
                        var routePrefix = "";
                        var viewPrefix = "";
                        foreach (var namedArg in attr.NamedArguments)
                        {
                            if (
                                namedArg.Key == "RoutePrefix"
                                && namedArg.Value.Value is string prefix
                            )
                            {
                                routePrefix = prefix;
                            }
                            else if (
                                namedArg.Key == "ViewPrefix"
                                && namedArg.Value.Value is string vPrefix
                            )
                            {
                                viewPrefix = vPrefix;
                            }
                        }

                        modules.Add(
                            new ModuleInfo
                            {
                                FullyQualifiedName = typeSymbol.ToDisplayString(
                                    SymbolDisplayFormat.FullyQualifiedFormat
                                ),
                                ModuleName = moduleName,
                                HasConfigureServices = DeclaresMethod(
                                    typeSymbol,
                                    "ConfigureServices"
                                ),
                                HasConfigureEndpoints = DeclaresMethod(
                                    typeSymbol,
                                    "ConfigureEndpoints"
                                ),
                                HasConfigureMenu = DeclaresMethod(typeSymbol, "ConfigureMenu"),
                                RoutePrefix = routePrefix,
                                ViewPrefix = viewPrefix,
                            }
                        );
                        break;
                    }
                }
            }
        }
    }

    private static void FindEndpointTypes(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol endpointInterfaceSymbol,
        INamedTypeSymbol? viewEndpointInterfaceSymbol,
        string moduleName,
        List<EndpointInfo> endpoints,
        List<ViewInfo> views
    )
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            if (member is INamespaceSymbol childNamespace)
            {
                FindEndpointTypes(
                    childNamespace,
                    endpointInterfaceSymbol,
                    viewEndpointInterfaceSymbol,
                    moduleName,
                    endpoints,
                    views
                );
            }
            else if (member is INamedTypeSymbol typeSymbol)
            {
                if (
                    !typeSymbol.IsAbstract
                    && !typeSymbol.IsStatic
                )
                {
                    var fqn = typeSymbol.ToDisplayString(
                        SymbolDisplayFormat.FullyQualifiedFormat
                    );

                    if (viewEndpointInterfaceSymbol is not null
                        && ImplementsInterface(typeSymbol, viewEndpointInterfaceSymbol))
                    {
                        var className = typeSymbol.Name;
                        if (className.EndsWith("Endpoint", StringComparison.Ordinal))
                            className = className.Substring(0, className.Length - "Endpoint".Length);
                        else if (className.EndsWith("View", StringComparison.Ordinal))
                            className = className.Substring(0, className.Length - "View".Length);

                        views.Add(new ViewInfo
                        {
                            FullyQualifiedName = fqn,
                            Page = moduleName + "/" + className,
                        });
                    }
                    else if (ImplementsInterface(typeSymbol, endpointInterfaceSymbol))
                    {
                        endpoints.Add(new EndpointInfo { FullyQualifiedName = fqn });
                    }
                }
            }
        }
    }

    private static bool ImplementsInterface(
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol interfaceSymbol
    )
    {
        foreach (var iface in typeSymbol.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(iface, interfaceSymbol))
                return true;
        }
        return false;
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
            sb.AppendLine($"        {fieldName}.ConfigureServices(services, configuration);");
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
        sb.AppendLine("using Microsoft.AspNetCore.Routing;");
        sb.AppendLine("using Microsoft.AspNetCore.Http;");
        sb.AppendLine();
        sb.AppendLine("namespace SimpleModule.Core;");
        sb.AppendLine();
        sb.AppendLine("public static class EndpointExtensions");
        sb.AppendLine("{");
        sb.AppendLine(
            "    public static WebApplication MapModuleEndpoints(this WebApplication app)"
        );
        sb.AppendLine("    {");

        // Auto-registered endpoints (IEndpoint implementors)
        // Skip modules that define ConfigureEndpoints — they manage their own registration.
        foreach (var module in modules)
        {
            if (module.Endpoints.Length == 0 || module.HasConfigureEndpoints)
                continue;

            sb.AppendLine();
            sb.AppendLine($"        // Auto-registered endpoints for {module.FullyQualifiedName}");
            sb.AppendLine("        {");

            if (!string.IsNullOrEmpty(module.RoutePrefix))
            {
                sb.AppendLine(
                    $"            var group = app.MapGroup(\"{module.RoutePrefix}\").WithTags(\"{module.ModuleName}\");"
                );
                foreach (var endpoint in module.Endpoints)
                {
                    sb.AppendLine($"            new {endpoint.FullyQualifiedName}().Map(group);");
                }
            }
            else
            {
                foreach (var endpoint in module.Endpoints)
                {
                    sb.AppendLine($"            new {endpoint.FullyQualifiedName}().Map(app);");
                }
            }

            sb.AppendLine("        }");
        }

        // Auto-registered view endpoints (IViewEndpoint implementors)
        foreach (var module in modules)
        {
            if (module.Views.Length == 0)
                continue;

            sb.AppendLine();
            sb.AppendLine(
                $"        // Auto-registered view endpoints for {module.FullyQualifiedName}"
            );
            sb.AppendLine("        {");

            if (!string.IsNullOrEmpty(module.ViewPrefix))
            {
                sb.AppendLine(
                    $"            var viewGroup = app.MapGroup(\"{module.ViewPrefix}\").WithTags(\"{module.ModuleName}\").ExcludeFromDescription();"
                );
                foreach (var view in module.Views)
                {
                    sb.AppendLine($"            new {view.FullyQualifiedName}().Map(viewGroup);");
                }
            }
            else
            {
                foreach (var view in module.Views)
                {
                    sb.AppendLine($"            new {view.FullyQualifiedName}().Map(app);");
                }
            }

            sb.AppendLine("        }");
        }

        // Manual ConfigureEndpoints (escape hatch)
        foreach (var module in modules.Where(m => m.HasConfigureEndpoints))
        {
            var fieldName = GetModuleFieldName(module.FullyQualifiedName);
            sb.AppendLine();
            sb.AppendLine($"        ModuleExtensions.{fieldName}.ConfigureEndpoints(app);");
        }

        sb.AppendLine();
        sb.AppendLine("        return app;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource("EndpointExtensions.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static void GenerateMenuExtensions(
        SourceProductionContext context,
        ImmutableArray<ModuleInfoRecord> modules
    )
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine("using SimpleModule.Core.Menu;");
        sb.AppendLine();
        sb.AppendLine("namespace SimpleModule.Core;");
        sb.AppendLine();
        sb.AppendLine("public static class MenuExtensions");
        sb.AppendLine("{");
        sb.AppendLine(
            "    public static IServiceCollection CollectModuleMenuItems(this IServiceCollection services)"
        );
        sb.AppendLine("    {");
        sb.AppendLine("        var menus = new MenuBuilder();");

        foreach (var module in modules.Where(m => m.HasConfigureMenu))
        {
            var fieldName = GetModuleFieldName(module.FullyQualifiedName);
            sb.AppendLine($"        ModuleExtensions.{fieldName}.ConfigureMenu(menus);");
        }

        sb.AppendLine(
            "        services.AddSingleton<IMenuRegistry>(new MenuRegistry(menus.ToList()));"
        );
        sb.AppendLine("        return services;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource("MenuExtensions.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
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

    private static void GenerateTypeScriptDefinitions(
        SourceProductionContext context,
        ImmutableArray<DtoTypeInfoRecord> dtoTypes
    )
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#if SIMPLEMODULE_TS");
        sb.AppendLine("/*");
        sb.AppendLine("// TypeScript interfaces generated from [Dto] types");
        sb.AppendLine();

        foreach (var dto in dtoTypes)
        {
            var typeName = dto.FullyQualifiedName;
            var shortName = typeName.Contains(".")
                ? typeName.Substring(typeName.LastIndexOf('.') + 1)
                : typeName;
            shortName = shortName.Replace("global::", "");

            sb.AppendLine($"export interface {shortName} {{");
            foreach (var prop in dto.Properties)
            {
                var tsType = MapCSharpTypeToTypeScript(prop.TypeFqn);
                var camelName = char.ToLowerInvariant(prop.Name[0]) + prop.Name.Substring(1);
                sb.AppendLine($"  {camelName}: {tsType};");
            }
            sb.AppendLine("}");
            sb.AppendLine();
        }

        sb.AppendLine("*/");
        sb.AppendLine("#endif");

        context.AddSource("DtoTypeScript.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static void GenerateViewPages(
        SourceProductionContext context,
        ImmutableArray<ModuleInfoRecord> modules
    )
    {
        foreach (var module in modules)
        {
            if (module.Views.Length == 0)
                continue;

            // Extract module name from FQN (e.g., "global::SimpleModule.Products.ProductsModule" → "Products")
            var fqn = module.FullyQualifiedName.Replace("global::", "");
            var moduleName = fqn.Contains(".") ? fqn.Substring(fqn.LastIndexOf('.') + 1) : fqn;
            if (moduleName.EndsWith("Module", StringComparison.Ordinal))
                moduleName = moduleName.Substring(0, moduleName.Length - "Module".Length);

            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("#if SIMPLEMODULE_TS");
            sb.AppendLine("/*");

            // Generate import statements — component name is the part after "/"
            foreach (var view in module.Views)
            {
                var componentName = view.Page.Contains("/")
                    ? view.Page.Substring(view.Page.LastIndexOf('/') + 1)
                    : view.Page;
                sb.AppendLine($"import {componentName} from '../Views/{componentName}';");
            }

            sb.AppendLine();
            sb.AppendLine("export const pages: Record<string, any> = {");

            for (var i = 0; i < module.Views.Length; i++)
            {
                var view = module.Views[i];
                var componentName = view.Page.Contains("/")
                    ? view.Page.Substring(view.Page.LastIndexOf('/') + 1)
                    : view.Page;
                var comma = i < module.Views.Length - 1 ? "," : "";
                sb.AppendLine($"  '{view.Page}': {componentName}{comma}");
            }

            sb.AppendLine("};");
            sb.AppendLine("*/");
            sb.AppendLine("#endif");

            context.AddSource(
                $"ViewPages_{moduleName}.g.cs",
                SourceText.From(sb.ToString(), Encoding.UTF8)
            );
        }
    }

    private static string MapCSharpTypeToTypeScript(string typeFqn)
    {
        var type = typeFqn.Replace("global::", "");

        // Nullable<T> → T | null
        if (
            type.StartsWith("System.Nullable<", StringComparison.Ordinal)
            && type.EndsWith(">", StringComparison.Ordinal)
        )
        {
            var inner = type.Substring(
                "System.Nullable<".Length,
                type.Length - "System.Nullable<".Length - 1
            );
            return MapCSharpTypeToTypeScript(inner) + " | null";
        }

        // Collection types
        if (
            type.StartsWith("System.Collections.Generic.List<", StringComparison.Ordinal)
            || type.StartsWith("System.Collections.Generic.IList<", StringComparison.Ordinal)
            || type.StartsWith("System.Collections.Generic.IEnumerable<", StringComparison.Ordinal)
            || type.StartsWith(
                "System.Collections.Generic.IReadOnlyList<",
                StringComparison.Ordinal
            )
            || type.StartsWith("System.Collections.Generic.ICollection<", StringComparison.Ordinal)
        )
        {
            var start = type.IndexOf('<') + 1;
            var inner = type.Substring(start, type.Length - start - 1);
            return MapCSharpTypeToTypeScript(inner) + "[]";
        }

        return type switch
        {
            "string" or "System.String" => "string",
            "int" or "System.Int32" => "number",
            "long" or "System.Int64" => "number",
            "short" or "System.Int16" => "number",
            "byte" or "System.Byte" => "number",
            "float" or "System.Single" => "number",
            "double" or "System.Double" => "number",
            "decimal" or "System.Decimal" => "number",
            "bool" or "System.Boolean" => "boolean",
            "System.DateTime"
            or "System.DateTimeOffset"
            or "System.DateOnly"
            or "System.TimeOnly" => "string",
            "System.Guid" => "string",
            _ => "any",
        };
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
        sb.AppendLine("        this RazorComponentsEndpointConventionBuilder builder)");
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

}
