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

    /// <summary>
    /// Pre-computed namespace → module-name index used by
    /// <see cref="FindClosestModuleNameFast"/>. Entries are sorted by namespace
    /// length descending so the first <c>StartsWith</c> match wins (longest
    /// namespace wins over shorter prefix matches).
    /// </summary>
    internal readonly struct ModuleNamespaceIndex
    {
        internal readonly (string Namespace, string ModuleName)[] Entries;
        internal readonly string FirstModuleName;

        internal ModuleNamespaceIndex(
            (string Namespace, string ModuleName)[] entries,
            string firstModuleName
        )
        {
            Entries = entries;
            FirstModuleName = firstModuleName;
        }
    }

    internal static ModuleNamespaceIndex BuildModuleNamespaceIndex(List<ModuleInfo> modules)
    {
        var entries = new (string Namespace, string ModuleName)[modules.Count];
        for (var i = 0; i < modules.Count; i++)
        {
            var moduleFqn = TypeMappingHelpers.StripGlobalPrefix(modules[i].FullyQualifiedName);
            entries[i] = (TypeMappingHelpers.ExtractNamespace(moduleFqn), modules[i].ModuleName);
        }

        System.Array.Sort(entries, (a, b) => b.Namespace.Length.CompareTo(a.Namespace.Length));

        return new ModuleNamespaceIndex(entries, modules[0].ModuleName);
    }

    internal static string FindClosestModuleNameFast(string typeFqn, ModuleNamespaceIndex index)
    {
        foreach (var (ns, moduleName) in index.Entries)
        {
            if (typeFqn.StartsWith(ns, StringComparison.Ordinal))
                return moduleName;
        }
        return index.FirstModuleName;
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
