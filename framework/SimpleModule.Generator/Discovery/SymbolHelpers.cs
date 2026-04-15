using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static class SymbolHelpers
{
    /// <summary>
    /// Extracts a serializable source location from a symbol, if available.
    /// Returns null for symbols only available in metadata (compiled DLLs).
    /// </summary>
    internal static SourceLocationRecord? GetSourceLocation(ISymbol symbol)
    {
        foreach (var loc in symbol.Locations)
        {
            if (loc.IsInSource)
            {
                var span = loc.GetLineSpan();
                return new SourceLocationRecord(
                    span.Path,
                    span.StartLinePosition.Line,
                    span.StartLinePosition.Character,
                    span.EndLinePosition.Line,
                    span.EndLinePosition.Character
                );
            }
        }
        return null;
    }

    internal static bool ImplementsInterface(
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

    internal static bool InheritsFrom(INamedTypeSymbol typeSymbol, INamedTypeSymbol baseType)
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

    internal static bool DeclaresMethod(INamedTypeSymbol typeSymbol, string methodName)
    {
        foreach (var member in typeSymbol.GetMembers(methodName))
        {
            if (member is IMethodSymbol method)
            {
                if (method.DeclaringSyntaxReferences.Length > 0)
                    return true;
                if (
                    !method.IsImplicitlyDeclared && method.Locations.Any(static l => l.IsInMetadata)
                )
                    return true;
            }
        }
        return false;
    }

    internal static void ScanModuleAssemblies(
        List<ModuleInfo> modules,
        Dictionary<string, INamedTypeSymbol> moduleSymbols,
        Action<IAssemblySymbol, ModuleInfo> action
    )
    {
        var scanned = new HashSet<IAssemblySymbol>(SymbolEqualityComparer.Default);
        foreach (var module in modules)
        {
            if (!moduleSymbols.TryGetValue(module.FullyQualifiedName, out var typeSymbol))
                continue;

            if (scanned.Add(typeSymbol.ContainingAssembly))
                action(typeSymbol.ContainingAssembly, module);
        }
    }

    internal static string FindClosestModuleName(string typeFqn, List<ModuleInfo> modules)
    {
        // Match by longest shared namespace prefix between the type and each module class.
        var bestMatch = "";
        var bestLength = -1;
        foreach (var module in modules)
        {
            var moduleFqn = TypeMappingHelpers.StripGlobalPrefix(module.FullyQualifiedName);
            var moduleNs = TypeMappingHelpers.ExtractNamespace(moduleFqn);

            if (
                typeFqn.StartsWith(moduleNs, StringComparison.Ordinal)
                && moduleNs.Length > bestLength
            )
            {
                bestLength = moduleNs.Length;
                bestMatch = module.ModuleName;
            }
        }

        return bestMatch.Length > 0 ? bestMatch : modules[0].ModuleName;
    }

    /// <summary>
    /// Recursively walks namespaces and invokes <paramref name="onMatch"/> for each
    /// concrete (non-abstract, non-static) class that implements the given interface.
    /// </summary>
    internal static void FindConcreteClassesImplementing(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol interfaceSymbol,
        Action<INamedTypeSymbol> onMatch
    )
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            if (member is INamespaceSymbol childNs)
            {
                FindConcreteClassesImplementing(childNs, interfaceSymbol, onMatch);
            }
            else if (
                member is INamedTypeSymbol typeSymbol
                && typeSymbol.TypeKind == TypeKind.Class
                && !typeSymbol.IsAbstract
                && !typeSymbol.IsStatic
                && ImplementsInterface(typeSymbol, interfaceSymbol)
            )
            {
                onMatch(typeSymbol);
            }
        }
    }
}
