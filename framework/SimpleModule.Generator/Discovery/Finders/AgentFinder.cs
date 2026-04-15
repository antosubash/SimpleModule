using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static class AgentFinder
{
    internal static void FindImplementors(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol interfaceSymbol,
        string moduleName,
        List<DiscoveredTypeInfo> results
    )
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            if (member is INamespaceSymbol childNamespace)
            {
                FindImplementors(childNamespace, interfaceSymbol, moduleName, results);
            }
            else if (
                member is INamedTypeSymbol typeSymbol
                && !typeSymbol.IsAbstract
                && typeSymbol.TypeKind == TypeKind.Class
                && SymbolHelpers.ImplementsInterface(typeSymbol, interfaceSymbol)
            )
            {
                results.Add(
                    new DiscoveredTypeInfo
                    {
                        FullyQualifiedName = typeSymbol.ToDisplayString(
                            SymbolDisplayFormat.FullyQualifiedFormat
                        ),
                        ModuleName = moduleName,
                    }
                );
            }
        }
    }

    /// <summary>
    /// Scans every module's implementation assembly for three agent-related
    /// interface implementors: IAgentDefinition, IAgentToolProvider,
    /// IKnowledgeSource. Each is scanned independently; a null symbol in
    /// <paramref name="symbols"/> skips that scan.
    /// </summary>
    internal static void DiscoverAll(
        List<ModuleInfo> modules,
        Dictionary<string, INamedTypeSymbol> moduleSymbols,
        CoreSymbols symbols,
        List<DiscoveredTypeInfo> agentDefinitions,
        List<DiscoveredTypeInfo> agentToolProviders,
        List<DiscoveredTypeInfo> knowledgeSources
    )
    {
        if (symbols.AgentDefinition is not null)
        {
            SymbolHelpers.ScanModuleAssemblies(
                modules,
                moduleSymbols,
                (assembly, module) =>
                    FindImplementors(
                        assembly.GlobalNamespace,
                        symbols.AgentDefinition,
                        module.ModuleName,
                        agentDefinitions
                    )
            );
        }

        if (symbols.AgentToolProvider is not null)
        {
            SymbolHelpers.ScanModuleAssemblies(
                modules,
                moduleSymbols,
                (assembly, module) =>
                    FindImplementors(
                        assembly.GlobalNamespace,
                        symbols.AgentToolProvider,
                        module.ModuleName,
                        agentToolProviders
                    )
            );
        }

        if (symbols.KnowledgeSource is not null)
        {
            SymbolHelpers.ScanModuleAssemblies(
                modules,
                moduleSymbols,
                (assembly, module) =>
                    FindImplementors(
                        assembly.GlobalNamespace,
                        symbols.KnowledgeSource,
                        module.ModuleName,
                        knowledgeSources
                    )
            );
        }
    }
}
