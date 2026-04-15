using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static class ContractAndDtoChecks
{
    internal static void Run(SourceProductionContext context, DiscoveryData data)
    {
        // SM0012/SM0013: Contract interface size
        foreach (var iface in data.ContractInterfaces)
        {
            if (iface.MethodCount > 20)
            {
                var shortName = ExtractShortName(iface.InterfaceName);
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.ContractInterfaceTooLargeError,
                        LocationHelper.ToLocation(iface.Location),
                        Strip(iface.InterfaceName),
                        iface.MethodCount,
                        shortName
                    )
                );
            }
            else if (iface.MethodCount >= 15)
            {
                var shortName = ExtractShortName(iface.InterfaceName);
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.ContractInterfaceTooLargeWarning,
                        LocationHelper.ToLocation(iface.Location),
                        Strip(iface.InterfaceName),
                        iface.MethodCount,
                        shortName
                    )
                );
            }
        }

        // SM0014: Missing contract interfaces in referenced contracts assemblies
        var contractsWithInterfaces = new HashSet<string>();
        foreach (var iface in data.ContractInterfaces)
            contractsWithInterfaces.Add(iface.ContractsAssemblyName);

        // Deduplicate: only report once per (module, contracts assembly) pair
        var reported = new HashSet<string>();
        foreach (var dep in data.Dependencies)
        {
            var key = dep.ModuleName + "|" + dep.ContractsAssemblyName;
            if (!contractsWithInterfaces.Contains(dep.ContractsAssemblyName) && reported.Add(key))
            {
                // Find the module's location for this diagnostic
                SourceLocationRecord? depModuleLoc = null;
                foreach (var module in data.Modules)
                {
                    if (module.ModuleName == dep.ModuleName)
                    {
                        depModuleLoc = module.Location;
                        break;
                    }
                }

                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.MissingContractInterfaces,
                        LocationHelper.ToLocation(depModuleLoc),
                        dep.ModuleName,
                        dep.ContractsAssemblyName
                    )
                );
            }
        }

        // SM0025/SM0026/SM0028/SM0029: Contract implementation diagnostics
        // Group all implementations by interface FQN
        var implsByInterface = new Dictionary<string, List<ContractImplementationRecord>>();
        foreach (var impl in data.ContractImplementations)
        {
            if (!implsByInterface.TryGetValue(impl.InterfaceFqn, out var list))
            {
                list = new List<ContractImplementationRecord>();
                implsByInterface[impl.InterfaceFqn] = list;
            }
            list.Add(impl);
        }

        // SM0028: Non-public implementations
        // SM0029: Abstract implementations
        foreach (var impl in data.ContractImplementations)
        {
            if (!impl.IsPublic)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.ContractImplementationNotPublic,
                        LocationHelper.ToLocation(impl.Location),
                        Strip(impl.ImplementationFqn),
                        Strip(impl.InterfaceFqn)
                    )
                );
            }

            if (impl.IsAbstract)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.ContractImplementationIsAbstract,
                        LocationHelper.ToLocation(impl.Location),
                        Strip(impl.ImplementationFqn),
                        Strip(impl.InterfaceFqn)
                    )
                );
            }
        }

        // SM0025: No implementation for a contract interface
        foreach (var iface in data.ContractInterfaces)
        {
            if (!implsByInterface.ContainsKey(iface.InterfaceName))
            {
                // Derive module name from contracts assembly name
                var moduleName = iface.ContractsAssemblyName;
                if (moduleName.EndsWith(".Contracts", System.StringComparison.Ordinal))
                    moduleName = moduleName.Substring(0, moduleName.Length - ".Contracts".Length);

                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.NoContractImplementation,
                        LocationHelper.ToLocation(iface.Location),
                        Strip(iface.InterfaceName),
                        moduleName
                    )
                );
            }
        }

        // SM0026: Multiple valid implementations for the same interface
        foreach (var kvp in implsByInterface)
        {
            var validImpls = new List<ContractImplementationRecord>();
            foreach (var impl in kvp.Value)
            {
                if (impl.IsPublic && !impl.IsAbstract)
                    validImpls.Add(impl);
            }

            if (validImpls.Count > 1)
            {
                var names = new List<string>();
                foreach (var impl in validImpls)
                    names.Add(Strip(impl.ImplementationFqn));

                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.MultipleContractImplementations,
                        LocationHelper.ToLocation(validImpls[1].Location),
                        Strip(kvp.Key),
                        validImpls[0].ModuleName,
                        string.Join(", ", names)
                    )
                );
            }
        }

        // SM0035: DTO type in contracts with no public properties
        // Exclude permission and feature classes — they only have const string fields, not properties
        var permissionClassFqns = new HashSet<string>();
        foreach (var perm in data.PermissionClasses)
            permissionClassFqns.Add(perm.FullyQualifiedName);
        foreach (var feat in data.FeatureClasses)
            permissionClassFqns.Add(feat.FullyQualifiedName);

        foreach (var dto in data.DtoTypes)
        {
            if (
                dto.FullyQualifiedName.Contains(".Contracts.")
                && dto.Properties.Length == 0
                && !permissionClassFqns.Contains(dto.FullyQualifiedName)
            )
            {
                // Extract assembly/namespace context from FQN
                var fqn = Strip(dto.FullyQualifiedName);
                var contractsIdx = fqn.IndexOf(".Contracts.", System.StringComparison.Ordinal);
                var contractsAsm =
                    contractsIdx >= 0 ? fqn.Substring(0, contractsIdx + ".Contracts".Length) : fqn;

                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.DtoTypeNoProperties,
                        Location.None,
                        fqn,
                        contractsAsm
                    )
                );
            }
        }

        // SM0038: Infrastructure type (DbContext subclass) in Contracts assembly
        foreach (var dto in data.DtoTypes)
        {
            if (
                dto.FullyQualifiedName.Contains(".Contracts.")
                && dto.FullyQualifiedName.Contains("DbContext")
            )
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.InfrastructureTypeInContracts,
                        Location.None,
                        Strip(dto.FullyQualifiedName)
                    )
                );
            }
        }
    }

    private static string Strip(string fqn) => TypeMappingHelpers.StripGlobalPrefix(fqn);

    private static string ExtractShortName(string interfaceName)
    {
        var name = Strip(interfaceName);
        if (name.Contains("."))
            name = name.Substring(name.LastIndexOf('.') + 1);
        if (name.StartsWith("I", System.StringComparison.Ordinal) && name.Length > 1)
            name = name.Substring(1);
        if (name.EndsWith("Contracts", System.StringComparison.Ordinal))
            name = name.Substring(0, name.Length - "Contracts".Length);
        return name;
    }
}
