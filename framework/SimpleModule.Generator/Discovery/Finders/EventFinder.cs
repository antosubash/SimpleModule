using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

/// <summary>
/// Discovers domain events a module publishes (types implementing
/// <c>SimpleModule.Core.Events.IEvent</c> declared in the module's implementation
/// or contracts assembly) and consumes (first parameters of Wolverine-convention
/// handler methods: classes named <c>*Handler</c>/<c>*Consumer</c> with a public
/// <c>Handle</c>/<c>HandleAsync</c>/<c>Consume</c>/<c>ConsumeAsync</c> method).
/// </summary>
internal static class EventFinder
{
    private static readonly string[] HandlerMethodNames =
    [
        "Handle",
        "HandleAsync",
        "Consume",
        "ConsumeAsync",
    ];

    internal static void Discover(
        List<ModuleInfo> modules,
        Dictionary<string, INamedTypeSymbol> moduleSymbols,
        Dictionary<string, IAssemblySymbol> contractsAssemblySymbols,
        Dictionary<string, string> contractsAssemblyMap,
        CoreSymbols s,
        List<EventTypeRecord> eventTypes,
        List<EventHandlerRecord> eventHandlers,
        CancellationToken cancellationToken
    )
    {
        if (s.EventInterface is null)
            return;

        // An assembly is walked once even if it declares several [Module] classes;
        // its events are attributed to the first module encountered.
        var walkedAssemblies = new HashSet<string>(StringComparer.Ordinal);

        foreach (var module in modules)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!moduleSymbols.TryGetValue(module.FullyQualifiedName, out var moduleSymbol))
                continue;

            var implAssembly = moduleSymbol.ContainingAssembly;
            if (walkedAssemblies.Add(implAssembly.Name))
            {
                Walk(
                    implAssembly.GlobalNamespace,
                    s.EventInterface,
                    module.ModuleName,
                    eventTypes,
                    eventHandlers,
                    cancellationToken
                );
            }
        }

        foreach (var kvp in contractsAssemblyMap)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!contractsAssemblySymbols.TryGetValue(kvp.Key, out var contractsAssembly))
                continue;

            if (walkedAssemblies.Add(contractsAssembly.Name))
            {
                Walk(
                    contractsAssembly.GlobalNamespace,
                    s.EventInterface,
                    kvp.Value,
                    eventTypes,
                    eventHandlers,
                    cancellationToken
                );
            }
        }
    }

    private static void Walk(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol eventInterface,
        string moduleName,
        List<EventTypeRecord> eventTypes,
        List<EventHandlerRecord> eventHandlers,
        CancellationToken cancellationToken
    )
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (member is INamespaceSymbol childNs)
            {
                Walk(
                    childNs,
                    eventInterface,
                    moduleName,
                    eventTypes,
                    eventHandlers,
                    cancellationToken
                );
                continue;
            }

            if (member is not INamedTypeSymbol typeSymbol || typeSymbol.TypeKind != TypeKind.Class)
                continue;

            if (
                !typeSymbol.IsAbstract
                && SymbolHelpers.ImplementsInterface(typeSymbol, eventInterface)
            )
            {
                eventTypes.Add(
                    new EventTypeRecord(
                        typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        moduleName
                    )
                );
                continue;
            }

            if (
                typeSymbol.Name.EndsWith("Handler", StringComparison.Ordinal)
                || typeSymbol.Name.EndsWith("Consumer", StringComparison.Ordinal)
            )
            {
                CollectHandledEvents(typeSymbol, eventInterface, moduleName, eventHandlers);
            }
        }
    }

    private static void CollectHandledEvents(
        INamedTypeSymbol handlerType,
        INamedTypeSymbol eventInterface,
        string moduleName,
        List<EventHandlerRecord> eventHandlers
    )
    {
        foreach (var member in handlerType.GetMembers())
        {
            if (
                member is not IMethodSymbol method
                || method.DeclaredAccessibility != Accessibility.Public
                || method.IsStatic
                || method.Parameters.Length == 0
                || Array.IndexOf(HandlerMethodNames, method.Name) < 0
            )
                continue;

            if (
                method.Parameters[0].Type is INamedTypeSymbol eventType
                && SymbolHelpers.ImplementsInterface(eventType, eventInterface)
            )
            {
                eventHandlers.Add(
                    new EventHandlerRecord(
                        eventType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        moduleName
                    )
                );
            }
        }
    }
}
