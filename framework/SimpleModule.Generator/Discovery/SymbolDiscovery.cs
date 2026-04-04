using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static class SymbolDiscovery
{
    /// <summary>
    /// Extracts a serializable source location from a symbol, if available.
    /// Returns null for symbols only available in metadata (compiled DLLs).
    /// </summary>
    private static SourceLocationRecord? GetSourceLocation(ISymbol symbol)
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

    internal static DiscoveryData Extract(
        Compilation compilation,
        CancellationToken cancellationToken
    )
    {
        var hostAssemblyName = compilation.Assembly.Name;

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

        var agentDefinitionSymbol = compilation.GetTypeByMetadataName(
            "SimpleModule.Core.Agents.IAgentDefinition"
        );
        var agentToolProviderSymbol = compilation.GetTypeByMetadataName(
            "SimpleModule.Core.Agents.IAgentToolProvider"
        );
        var knowledgeSourceSymbol = compilation.GetTypeByMetadataName(
            "SimpleModule.Core.Rag.IKnowledgeSource"
        );

        // Resolve focused sub-interface symbols for module capability detection
        var moduleServicesSymbol = compilation.GetTypeByMetadataName(
            "SimpleModule.Core.IModuleServices"
        );
        var moduleMenuSymbol = compilation.GetTypeByMetadataName("SimpleModule.Core.IModuleMenu");
        var moduleMiddlewareSymbol = compilation.GetTypeByMetadataName(
            "SimpleModule.Core.IModuleMiddleware"
        );
        var moduleSettingsSymbol = compilation.GetTypeByMetadataName(
            "SimpleModule.Core.IModuleSettings"
        );

        var modules = new List<ModuleInfo>();

        foreach (var reference in compilation.References)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (
                compilation.GetAssemblyOrModuleSymbol(reference)
                is not IAssemblySymbol assemblySymbol
            )
                continue;

            FindModuleTypes(
                assemblySymbol.GlobalNamespace,
                moduleAttributeSymbol,
                moduleServicesSymbol,
                moduleMenuSymbol,
                moduleMiddlewareSymbol,
                moduleSettingsSymbol,
                modules,
                cancellationToken
            );
        }

        FindModuleTypes(
            compilation.Assembly.GlobalNamespace,
            moduleAttributeSymbol,
            moduleServicesSymbol,
            moduleMenuSymbol,
            moduleMiddlewareSymbol,
            moduleSettingsSymbol,
            modules,
            cancellationToken
        );

        if (modules.Count == 0)
            return DiscoveryData.Empty;

        // Resolve each module's type symbol once — avoids repeated GetTypeByMetadataName calls.
        var moduleSymbols = new Dictionary<string, INamedTypeSymbol>();
        foreach (var module in modules)
        {
            var metadataName = TypeMappingHelpers.StripGlobalPrefix(module.FullyQualifiedName);
            var typeSymbol = compilation.GetTypeByMetadataName(metadataName);
            if (typeSymbol is not null)
                moduleSymbols[module.FullyQualifiedName] = typeSymbol;
        }

        // Discover IEndpoint implementors per module assembly.
        // Classification is by interface type: IViewEndpoint -> view, IEndpoint -> API.
        // Scan each assembly once, then match endpoints to the closest module by namespace.
        if (endpointInterfaceSymbol is not null)
        {
            var endpointScannedAssemblies = new HashSet<IAssemblySymbol>(
                SymbolEqualityComparer.Default
            );
            foreach (var module in modules)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!moduleSymbols.TryGetValue(module.FullyQualifiedName, out var typeSymbol))
                    continue;

                var assembly = typeSymbol.ContainingAssembly;
                if (!endpointScannedAssemblies.Add(assembly))
                    continue;

                var rawEndpoints = new List<EndpointInfo>();
                var rawViews = new List<ViewInfo>();
                FindEndpointTypes(
                    assembly.GlobalNamespace,
                    endpointInterfaceSymbol,
                    viewEndpointInterfaceSymbol,
                    rawEndpoints,
                    rawViews,
                    cancellationToken
                );

                // Match each endpoint/view to the module whose namespace is closest
                foreach (var ep in rawEndpoints)
                {
                    var epFqn = TypeMappingHelpers.StripGlobalPrefix(ep.FullyQualifiedName);
                    var ownerName = FindClosestModuleName(epFqn, modules);
                    var owner = modules.Find(m => m.ModuleName == ownerName);
                    if (owner is not null)
                        owner.Endpoints.Add(ep);
                }

                foreach (var v in rawViews)
                {
                    var vFqn = TypeMappingHelpers.StripGlobalPrefix(v.FullyQualifiedName);
                    var ownerName = FindClosestModuleName(vFqn, modules);
                    var owner = modules.Find(m => m.ModuleName == ownerName);
                    if (owner is not null)
                    {
                        // Set page name using owner module name if not already set via [ViewPage]
                        if (v.Page is null)
                            v.Page = ownerName + "/" + v.InferredClassName;

                        owner.Views.Add(v);
                    }
                }
            }
        }

        // Discover DbContext subclasses and IEntityTypeConfiguration<T> per module assembly.
        // Scan each assembly once, then match DbContexts/configs to the nearest module by namespace.
        var dbContexts = new List<DbContextInfo>();
        var entityConfigs = new List<EntityConfigInfo>();
        var scannedAssemblies = new HashSet<IAssemblySymbol>(SymbolEqualityComparer.Default);
        foreach (var module in modules)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!moduleSymbols.TryGetValue(module.FullyQualifiedName, out var typeSymbol))
                continue;

            var assembly = typeSymbol.ContainingAssembly;
            if (!scannedAssemblies.Add(assembly))
                continue;

            // Collect unmatched items from this assembly
            var rawDbContexts = new List<DbContextInfo>();
            var rawEntityConfigs = new List<EntityConfigInfo>();
            FindDbContextTypes(assembly.GlobalNamespace, "", rawDbContexts, cancellationToken);
            FindEntityConfigTypes(
                assembly.GlobalNamespace,
                "",
                rawEntityConfigs,
                cancellationToken
            );

            // Match each DbContext to the module whose namespace is closest
            foreach (var ctx in rawDbContexts)
            {
                var ctxNs = TypeMappingHelpers.StripGlobalPrefix(ctx.FullyQualifiedName);
                ctx.ModuleName = FindClosestModuleName(ctxNs, modules);
                dbContexts.Add(ctx);
            }

            foreach (var cfg in rawEntityConfigs)
            {
                var cfgNs = TypeMappingHelpers.StripGlobalPrefix(cfg.ConfigFqn);
                cfg.ModuleName = FindClosestModuleName(cfgNs, modules);
                entityConfigs.Add(cfg);
            }
        }

        var dtoTypes = new List<DtoTypeInfo>();
        if (dtoAttributeSymbol is not null)
        {
            foreach (var reference in compilation.References)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (
                    compilation.GetAssemblyOrModuleSymbol(reference)
                    is not IAssemblySymbol assemblySymbol
                )
                    continue;

                FindDtoTypes(
                    assemblySymbol.GlobalNamespace,
                    dtoAttributeSymbol,
                    dtoTypes,
                    cancellationToken
                );
            }

            FindDtoTypes(
                compilation.Assembly.GlobalNamespace,
                dtoAttributeSymbol,
                dtoTypes,
                cancellationToken
            );
        }

        // --- Dependency inference ---
        cancellationToken.ThrowIfCancellationRequested();

        // Step 1: Build module assembly map (assembly name → module name)
        var moduleAssemblyMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var module in modules)
        {
            if (!moduleSymbols.TryGetValue(module.FullyQualifiedName, out var typeSymbol))
                continue;

            var assemblyName = typeSymbol.ContainingAssembly.Name;
            if (!moduleAssemblyMap.ContainsKey(assemblyName))
                moduleAssemblyMap[assemblyName] = module.ModuleName;
        }

        // Step 2: Build contracts-to-module map
        var contractsAssemblyMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var contractsAssemblySymbols = new Dictionary<string, IAssemblySymbol>(
            StringComparer.OrdinalIgnoreCase
        );

        foreach (var reference in compilation.References)
        {
            if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol asm)
                continue;

            var asmName = asm.Name;
            if (!asmName.EndsWith(".Contracts", StringComparison.OrdinalIgnoreCase))
                continue;

            var baseName = asmName.Substring(0, asmName.Length - ".Contracts".Length);

            // Try exact match on assembly name
            if (moduleAssemblyMap.TryGetValue(baseName, out var moduleName))
            {
                contractsAssemblyMap[asmName] = moduleName;
                contractsAssemblySymbols[asmName] = asm;
                continue;
            }

            // Try matching last segment of baseName to module names (case-insensitive)
            var lastDot = baseName.LastIndexOf('.');
            var lastSegment = lastDot >= 0 ? baseName.Substring(lastDot + 1) : baseName;

            foreach (var kvp in moduleAssemblyMap)
            {
                if (string.Equals(lastSegment, kvp.Value, StringComparison.OrdinalIgnoreCase))
                {
                    contractsAssemblyMap[asmName] = kvp.Value;
                    contractsAssemblySymbols[asmName] = asm;
                    break;
                }
            }
        }

        // Convention-based DTO discovery: all public types in *.Contracts assemblies
        var noDtoAttrSymbol = compilation.GetTypeByMetadataName(
            "SimpleModule.Core.NoDtoGenerationAttribute"
        );
        var eventInterfaceSymbol = compilation.GetTypeByMetadataName(
            "SimpleModule.Core.Events.IEvent"
        );
        var existingDtoFqns = new HashSet<string>();
        foreach (var d in dtoTypes)
            existingDtoFqns.Add(d.FullyQualifiedName);

        foreach (var kvp in contractsAssemblySymbols)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FindConventionDtoTypes(
                kvp.Value.GlobalNamespace,
                noDtoAttrSymbol,
                eventInterfaceSymbol,
                existingDtoFqns,
                dtoTypes,
                cancellationToken
            );
        }

        // Step 3: Scan contract interfaces
        var contractInterfaces = new List<ContractInterfaceInfoRecord>();
        foreach (var kvp in contractsAssemblySymbols)
        {
            ScanContractInterfaces(kvp.Value.GlobalNamespace, kvp.Key, contractInterfaces);
        }

        // Step 3b: Find implementations of contract interfaces in module assemblies
        var contractImplementations = new List<ContractImplementationInfo>();
        foreach (var module in modules)
        {
            if (!moduleSymbols.TryGetValue(module.FullyQualifiedName, out var typeSymbol))
                continue;

            var moduleAssembly = typeSymbol.ContainingAssembly;

            // Find which contracts assembly this module owns
            var expectedContractsAsm = moduleAssembly.Name + ".Contracts";
            if (!contractsAssemblySymbols.ContainsKey(expectedContractsAsm))
                continue;

            // Get the interface FQNs from this module's contracts
            var moduleContractInterfaceFqns = new HashSet<string>();
            foreach (var ci in contractInterfaces)
            {
                if (ci.ContractsAssemblyName == expectedContractsAsm)
                    moduleContractInterfaceFqns.Add(ci.InterfaceName);
            }
            if (moduleContractInterfaceFqns.Count == 0)
                continue;

            // Scan module assembly for implementations
            FindContractImplementations(
                moduleAssembly.GlobalNamespace,
                moduleContractInterfaceFqns,
                module.ModuleName,
                compilation,
                contractImplementations
            );
        }

        // Step 3c: Find IModulePermissions implementors in module and contracts assemblies
        var permissionClasses = new List<PermissionClassInfo>();
        var modulePermissionsSymbol = compilation.GetTypeByMetadataName(
            "SimpleModule.Core.Authorization.IModulePermissions"
        );
        if (modulePermissionsSymbol is not null)
        {
            foreach (var module in modules)
            {
                if (!moduleSymbols.TryGetValue(module.FullyQualifiedName, out var typeSymbol))
                    continue;

                var moduleAssembly = typeSymbol.ContainingAssembly;
                FindPermissionClasses(
                    moduleAssembly.GlobalNamespace,
                    modulePermissionsSymbol,
                    module.ModuleName,
                    permissionClasses
                );
            }

            // Also scan contracts assemblies for permission classes
            foreach (var kvp in contractsAssemblySymbols)
            {
                if (contractsAssemblyMap.TryGetValue(kvp.Key, out var moduleName))
                {
                    FindPermissionClasses(
                        kvp.Value.GlobalNamespace,
                        modulePermissionsSymbol,
                        moduleName,
                        permissionClasses
                    );
                }
            }
        }

        // Step 3d: Find IModuleFeatures implementors in module and contracts assemblies
        var featureClasses = new List<FeatureClassInfo>();
        var moduleFeaturesSymbol = compilation.GetTypeByMetadataName(
            "SimpleModule.Core.FeatureFlags.IModuleFeatures"
        );
        if (moduleFeaturesSymbol is not null)
        {
            foreach (var module in modules)
            {
                if (!moduleSymbols.TryGetValue(module.FullyQualifiedName, out var typeSymbol))
                    continue;

                var moduleAssembly = typeSymbol.ContainingAssembly;
                FindFeatureClasses(
                    moduleAssembly.GlobalNamespace,
                    moduleFeaturesSymbol,
                    module.ModuleName,
                    featureClasses
                );
            }

            // Also scan contracts assemblies for feature classes
            foreach (var kvp in contractsAssemblySymbols)
            {
                if (contractsAssemblyMap.TryGetValue(kvp.Key, out var moduleName))
                {
                    FindFeatureClasses(
                        kvp.Value.GlobalNamespace,
                        moduleFeaturesSymbol,
                        moduleName,
                        featureClasses
                    );
                }
            }
        }

        // Step 3e: Find ISaveChangesInterceptor implementors in module assemblies
        var interceptors = new List<InterceptorInfo>();
        var saveChangesInterceptorSymbol = compilation.GetTypeByMetadataName(
            "Microsoft.EntityFrameworkCore.Diagnostics.ISaveChangesInterceptor"
        );
        if (saveChangesInterceptorSymbol is not null)
        {
            ScanModuleAssemblies(
                modules,
                moduleSymbols,
                (assembly, module) =>
                {
                    FindInterceptorTypes(
                        assembly.GlobalNamespace,
                        saveChangesInterceptorSymbol,
                        module.ModuleName,
                        interceptors
                    );
                }
            );
        }

        // Step 3e: Discover Vogen value objects with EF Core value converters.
        // Scan Contracts assemblies and module assemblies only.
        var vogenValueObjects = new List<VogenValueObjectRecord>();
        var voScannedAssemblies = new HashSet<IAssemblySymbol>(SymbolEqualityComparer.Default);

        foreach (var kvp in contractsAssemblySymbols)
        {
            if (voScannedAssemblies.Add(kvp.Value))
            {
                FindVogenValueObjectsWithEfConverters(kvp.Value.GlobalNamespace, vogenValueObjects);
            }
        }

        ScanModuleAssemblies(
            modules,
            moduleSymbols,
            (assembly, _) =>
            {
                if (voScannedAssemblies.Add(assembly))
                {
                    FindVogenValueObjectsWithEfConverters(
                        assembly.GlobalNamespace,
                        vogenValueObjects
                    );
                }
            }
        );

        // Step 3f: Find IModuleOptions implementors in module and contracts assemblies
        var moduleOptionsList = new List<ModuleOptionsRecord>();
        var moduleOptionsSymbol = compilation.GetTypeByMetadataName(
            "SimpleModule.Core.IModuleOptions"
        );
        if (moduleOptionsSymbol is not null)
        {
            ScanModuleAssemblies(
                modules,
                moduleSymbols,
                (assembly, module) =>
                    FindModuleOptionsClasses(
                        assembly.GlobalNamespace,
                        moduleOptionsSymbol,
                        module.ModuleName,
                        moduleOptionsList
                    )
            );

            // Also scan contracts assemblies for module options classes
            foreach (var kvp in contractsAssemblySymbols)
            {
                if (contractsAssemblyMap.TryGetValue(kvp.Key, out var moduleName))
                {
                    FindModuleOptionsClasses(
                        kvp.Value.GlobalNamespace,
                        moduleOptionsSymbol,
                        moduleName,
                        moduleOptionsList
                    );
                }
            }
        }

        // Step 3g: Find IAgentDefinition, IAgentToolProvider, and IKnowledgeSource implementors
        var agentDefinitions = new List<DiscoveredTypeInfo>();
        var agentToolProviders = new List<DiscoveredTypeInfo>();
        var knowledgeSources = new List<DiscoveredTypeInfo>();

        if (agentDefinitionSymbol is not null)
        {
            ScanModuleAssemblies(
                modules,
                moduleSymbols,
                (assembly, module) =>
                    FindImplementors(
                        assembly.GlobalNamespace,
                        agentDefinitionSymbol,
                        module.ModuleName,
                        agentDefinitions
                    )
            );
        }

        if (agentToolProviderSymbol is not null)
        {
            ScanModuleAssemblies(
                modules,
                moduleSymbols,
                (assembly, module) =>
                    FindImplementors(
                        assembly.GlobalNamespace,
                        agentToolProviderSymbol,
                        module.ModuleName,
                        agentToolProviders
                    )
            );
        }

        if (knowledgeSourceSymbol is not null)
        {
            ScanModuleAssemblies(
                modules,
                moduleSymbols,
                (assembly, module) =>
                    FindImplementors(
                        assembly.GlobalNamespace,
                        knowledgeSourceSymbol,
                        module.ModuleName,
                        knowledgeSources
                    )
            );
        }

        // Step 4: Detect dependencies and illegal references
        var dependencies = new List<ModuleDependencyRecord>();
        var illegalReferences = new List<IllegalModuleReferenceRecord>();

        foreach (var module in modules)
        {
            if (!moduleSymbols.TryGetValue(module.FullyQualifiedName, out var typeSymbol))
                continue;

            var moduleAssembly = typeSymbol.ContainingAssembly;
            var thisModuleName = module.ModuleName;

            foreach (var asmModule in moduleAssembly.Modules)
            {
                foreach (var referencedAsm in asmModule.ReferencedAssemblySymbols)
                {
                    var refName = referencedAsm.Name;

                    // Check for illegal direct module-to-module reference
                    if (
                        moduleAssemblyMap.TryGetValue(refName, out var referencedModuleName)
                        && !string.Equals(
                            referencedModuleName,
                            thisModuleName,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        illegalReferences.Add(
                            new IllegalModuleReferenceRecord(
                                thisModuleName,
                                moduleAssembly.Name,
                                referencedModuleName,
                                refName,
                                module.Location
                            )
                        );
                    }

                    // Check for dependency via contracts
                    if (
                        contractsAssemblyMap.TryGetValue(refName, out var depModuleName)
                        && !string.Equals(
                            depModuleName,
                            thisModuleName,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        dependencies.Add(
                            new ModuleDependencyRecord(thisModuleName, depModuleName, refName)
                        );
                    }
                }
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
                    m.HasConfigurePermissions,
                    m.HasConfigureMiddleware,
                    m.HasConfigureSettings,
                    m.HasConfigureFeatureFlags,
                    m.HasConfigureAgents,
                    m.HasConfigureRateLimits,
                    m.RoutePrefix,
                    m.ViewPrefix,
                    m.Endpoints.Select(e => new EndpointInfoRecord(
                            e.FullyQualifiedName,
                            e.RequiredPermissions.ToImmutableArray(),
                            e.AllowAnonymous
                        ))
                        .ToImmutableArray(),
                    m.Views.Select(v => new ViewInfoRecord(
                            v.FullyQualifiedName,
                            v.Page ?? "",
                            v.Location
                        ))
                        .ToImmutableArray(),
                    m.Location
                ))
                .ToImmutableArray(),
            dtoTypes
                .Select(d => new DtoTypeInfoRecord(
                    d.FullyQualifiedName,
                    d.SafeName,
                    d.Properties.Select(p => new DtoPropertyInfoRecord(
                            p.Name,
                            p.TypeFqn,
                            p.UnderlyingTypeFqn,
                            p.HasSetter
                        ))
                        .ToImmutableArray()
                ))
                .ToImmutableArray(),
            dbContexts
                .Select(c => new DbContextInfoRecord(
                    c.FullyQualifiedName,
                    c.ModuleName,
                    c.IsIdentityDbContext,
                    c.IdentityUserTypeFqn,
                    c.IdentityRoleTypeFqn,
                    c.IdentityKeyTypeFqn,
                    c.DbSets.Select(d => new DbSetInfoRecord(d.PropertyName, d.EntityFqn))
                        .ToImmutableArray(),
                    c.Location
                ))
                .ToImmutableArray(),
            entityConfigs
                .Select(e => new EntityConfigInfoRecord(
                    e.ConfigFqn,
                    e.EntityFqn,
                    e.ModuleName,
                    e.Location
                ))
                .ToImmutableArray(),
            dependencies.ToImmutableArray(),
            illegalReferences.ToImmutableArray(),
            contractInterfaces.ToImmutableArray(),
            contractImplementations
                .Select(c => new ContractImplementationRecord(
                    c.InterfaceFqn,
                    c.ImplementationFqn,
                    c.ModuleName,
                    c.IsPublic,
                    c.IsAbstract,
                    c.DependsOnDbContext,
                    c.Location,
                    c.Lifetime
                ))
                .ToImmutableArray(),
            permissionClasses
                .Select(p => new PermissionClassRecord(
                    p.FullyQualifiedName,
                    p.ModuleName,
                    p.IsSealed,
                    p.Fields.Select(f => new PermissionFieldRecord(
                            f.FieldName,
                            f.Value,
                            f.IsConstString,
                            f.Location
                        ))
                        .ToImmutableArray(),
                    p.Location
                ))
                .ToImmutableArray(),
            featureClasses
                .Select(f => new FeatureClassRecord(
                    f.FullyQualifiedName,
                    f.ModuleName,
                    f.IsSealed,
                    f.Fields.Select(ff => new FeatureFieldRecord(
                            ff.FieldName,
                            ff.Value,
                            ff.IsConstString,
                            ff.Location
                        ))
                        .ToImmutableArray(),
                    f.Location
                ))
                .ToImmutableArray(),
            interceptors
                .Select(i => new InterceptorInfoRecord(
                    i.FullyQualifiedName,
                    i.ModuleName,
                    i.ConstructorParamTypeFqns.ToImmutableArray(),
                    i.Location
                ))
                .ToImmutableArray(),
            vogenValueObjects.ToImmutableArray(),
            moduleOptionsList.ToImmutableArray(),
            agentDefinitions
                .Select(a => new AgentDefinitionRecord(a.FullyQualifiedName, a.ModuleName))
                .ToImmutableArray(),
            agentToolProviders
                .Select(a => new AgentToolProviderRecord(a.FullyQualifiedName, a.ModuleName))
                .ToImmutableArray(),
            knowledgeSources
                .Select(k => new KnowledgeSourceRecord(k.FullyQualifiedName, k.ModuleName))
                .ToImmutableArray(),
            hostAssemblyName
        );
    }

    private static void FindModuleTypes(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol moduleAttributeSymbol,
        INamedTypeSymbol? moduleServicesSymbol,
        INamedTypeSymbol? moduleMenuSymbol,
        INamedTypeSymbol? moduleMiddlewareSymbol,
        INamedTypeSymbol? moduleSettingsSymbol,
        List<ModuleInfo> modules,
        CancellationToken cancellationToken
    )
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (member is INamespaceSymbol childNamespace)
            {
                FindModuleTypes(
                    childNamespace,
                    moduleAttributeSymbol,
                    moduleServicesSymbol,
                    moduleMenuSymbol,
                    moduleMiddlewareSymbol,
                    moduleSettingsSymbol,
                    modules,
                    cancellationToken
                );
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
                                HasConfigureServices =
                                    DeclaresMethod(typeSymbol, "ConfigureServices")
                                    || (
                                        moduleServicesSymbol is not null
                                        && ImplementsInterface(typeSymbol, moduleServicesSymbol)
                                    ),
                                HasConfigureEndpoints = DeclaresMethod(
                                    typeSymbol,
                                    "ConfigureEndpoints"
                                ),
                                HasConfigureMenu =
                                    DeclaresMethod(typeSymbol, "ConfigureMenu")
                                    || (
                                        moduleMenuSymbol is not null
                                        && ImplementsInterface(typeSymbol, moduleMenuSymbol)
                                    ),
                                HasConfigureMiddleware =
                                    DeclaresMethod(typeSymbol, "ConfigureMiddleware")
                                    || (
                                        moduleMiddlewareSymbol is not null
                                        && ImplementsInterface(typeSymbol, moduleMiddlewareSymbol)
                                    ),
                                HasConfigurePermissions = DeclaresMethod(
                                    typeSymbol,
                                    "ConfigurePermissions"
                                ),
                                HasConfigureSettings =
                                    DeclaresMethod(typeSymbol, "ConfigureSettings")
                                    || (
                                        moduleSettingsSymbol is not null
                                        && ImplementsInterface(typeSymbol, moduleSettingsSymbol)
                                    ),
                                HasConfigureFeatureFlags = DeclaresMethod(
                                    typeSymbol,
                                    "ConfigureFeatureFlags"
                                ),
                                HasConfigureAgents = DeclaresMethod(typeSymbol, "ConfigureAgents"),
                                HasConfigureRateLimits = DeclaresMethod(
                                    typeSymbol,
                                    "ConfigureRateLimits"
                                ),
                                RoutePrefix = routePrefix,
                                ViewPrefix = viewPrefix,
                                Location = GetSourceLocation(typeSymbol),
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
        List<EndpointInfo> endpoints,
        List<ViewInfo> views,
        CancellationToken cancellationToken
    )
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (member is INamespaceSymbol childNamespace)
            {
                FindEndpointTypes(
                    childNamespace,
                    endpointInterfaceSymbol,
                    viewEndpointInterfaceSymbol,
                    endpoints,
                    views,
                    cancellationToken
                );
            }
            else if (member is INamedTypeSymbol typeSymbol)
            {
                if (!typeSymbol.IsAbstract && !typeSymbol.IsStatic)
                {
                    var fqn = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                    if (
                        viewEndpointInterfaceSymbol is not null
                        && ImplementsInterface(typeSymbol, viewEndpointInterfaceSymbol)
                    )
                    {
                        // Prefer [ViewPage("Component")] attribute over class name inference
                        string? page = null;
                        foreach (var attr in typeSymbol.GetAttributes())
                        {
                            var attrName = attr.AttributeClass?.ToDisplayString(
                                SymbolDisplayFormat.FullyQualifiedFormat
                            );
                            if (
                                attrName == "global::SimpleModule.Core.ViewPageAttribute"
                                && attr.ConstructorArguments.Length > 0
                                && attr.ConstructorArguments[0].Value is string component
                            )
                            {
                                page = component;
                                break;
                            }
                        }

                        // Infer class name for deferred page name computation
                        var className = typeSymbol.Name;
                        if (className.EndsWith("Endpoint", StringComparison.Ordinal))
                            className = className.Substring(
                                0,
                                className.Length - "Endpoint".Length
                            );
                        else if (className.EndsWith("View", StringComparison.Ordinal))
                            className = className.Substring(0, className.Length - "View".Length);

                        views.Add(
                            new ViewInfo
                            {
                                FullyQualifiedName = fqn,
                                Page = page,
                                InferredClassName = className,
                                Location = GetSourceLocation(typeSymbol),
                            }
                        );
                    }
                    else if (ImplementsInterface(typeSymbol, endpointInterfaceSymbol))
                    {
                        var info = new EndpointInfo { FullyQualifiedName = fqn };

                        foreach (var attr in typeSymbol.GetAttributes())
                        {
                            var attrName = attr.AttributeClass?.ToDisplayString(
                                SymbolDisplayFormat.FullyQualifiedFormat
                            );

                            if (
                                attrName
                                == "global::SimpleModule.Core.Authorization.RequirePermissionAttribute"
                            )
                            {
                                if (attr.ConstructorArguments.Length > 0)
                                {
                                    var arg = attr.ConstructorArguments[0];
                                    if (arg.Kind == TypedConstantKind.Array)
                                    {
                                        foreach (var val in arg.Values)
                                        {
                                            if (val.Value is string s)
                                                info.RequiredPermissions.Add(s);
                                        }
                                    }
                                    else if (arg.Value is string single)
                                    {
                                        info.RequiredPermissions.Add(single);
                                    }
                                }
                            }
                            else if (
                                attrName
                                == "global::Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute"
                            )
                            {
                                info.AllowAnonymous = true;
                            }
                        }

                        endpoints.Add(info);
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

    private static void ScanModuleAssemblies(
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

    private static bool DeclaresMethod(INamedTypeSymbol typeSymbol, string methodName)
    {
        foreach (var member in typeSymbol.GetMembers(methodName))
        {
            if (member is IMethodSymbol method)
            {
                // Source types: method has syntax in source code
                if (method.DeclaringSyntaxReferences.Length > 0)
                    return true;

                // Metadata types: method exists in compiled IL (not synthesized)
                // IsImplicitlyDeclared filters out compiler-synthesized stubs for
                // default interface method dispatch
                if (
                    !method.IsImplicitlyDeclared && method.Locations.Any(static l => l.IsInMetadata)
                )
                    return true;
            }
        }
        return false;
    }

    private static void FindDtoTypes(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol dtoAttributeSymbol,
        List<DtoTypeInfo> dtoTypes,
        CancellationToken cancellationToken
    )
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (member is INamespaceSymbol childNamespace)
            {
                FindDtoTypes(childNamespace, dtoAttributeSymbol, dtoTypes, cancellationToken);
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
                        var safeName = TypeMappingHelpers.StripGlobalPrefix(fqn).Replace(".", "_");

                        dtoTypes.Add(
                            new DtoTypeInfo
                            {
                                FullyQualifiedName = fqn,
                                SafeName = safeName,
                                Properties = ExtractDtoProperties(typeSymbol),
                            }
                        );
                        break;
                    }
                }
            }
        }
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

    /// <summary>
    /// Reads [ContractLifetime(ServiceLifetime.X)] from the type.
    /// Returns 1 (Scoped) if the attribute is not present.
    /// ServiceLifetime: Singleton=0, Scoped=1, Transient=2
    /// </summary>
    private static int GetContractLifetime(INamedTypeSymbol typeSymbol)
    {
        foreach (var attr in typeSymbol.GetAttributes())
        {
            var attrName = attr.AttributeClass?.ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat
            );
            if (
                attrName == "global::SimpleModule.Core.ContractLifetimeAttribute"
                && attr.ConstructorArguments.Length > 0
                && attr.ConstructorArguments[0].Value is int lifetime
            )
            {
                return lifetime;
            }
        }
        return 1; // Default: Scoped
    }

    private static bool HasDbContextConstructorParam(INamedTypeSymbol typeSymbol)
    {
        foreach (var ctor in typeSymbol.Constructors)
        {
            if (ctor.DeclaredAccessibility != Accessibility.Public || ctor.IsStatic)
                continue;

            foreach (var param in ctor.Parameters)
            {
                var paramType = param.Type;
                // Walk the base type chain to check for DbContext ancestry
                var current = paramType.BaseType;
                while (current != null)
                {
                    var baseFqn = current.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    if (
                        baseFqn == "global::Microsoft.EntityFrameworkCore.DbContext"
                        || baseFqn.StartsWith(
                            "global::Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext",
                            StringComparison.Ordinal
                        )
                    )
                    {
                        return true;
                    }

                    current = current.BaseType;
                }
            }
        }

        return false;
    }

    private static string FindClosestModuleName(string typeFqn, List<ModuleInfo> modules)
    {
        // Match by longest shared namespace prefix between the type and each module class.
        var bestMatch = "";
        var bestLength = -1;
        foreach (var module in modules)
        {
            var moduleFqn = TypeMappingHelpers.StripGlobalPrefix(module.FullyQualifiedName);
            var moduleNs = moduleFqn.Contains(".")
                ? moduleFqn.Substring(0, moduleFqn.LastIndexOf('.'))
                : "";

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

    private static void FindDbContextTypes(
        INamespaceSymbol namespaceSymbol,
        string moduleName,
        List<DbContextInfo> dbContexts,
        CancellationToken cancellationToken
    )
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (member is INamespaceSymbol childNamespace)
            {
                FindDbContextTypes(childNamespace, moduleName, dbContexts, cancellationToken);
            }
            else if (
                member is INamedTypeSymbol typeSymbol
                && !typeSymbol.IsAbstract
                && !typeSymbol.IsStatic
            )
            {
                // Walk base type chain looking for DbContext
                var isDbContext = false;
                var isIdentity = false;
                string identityUserFqn = "";
                string identityRoleFqn = "";
                string identityKeyFqn = "";

                var current = typeSymbol.BaseType;
                while (current is not null)
                {
                    var baseFqn = current.OriginalDefinition.ToDisplayString(
                        SymbolDisplayFormat.FullyQualifiedFormat
                    );

                    if (
                        baseFqn
                        == "global::Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext<TUser, TRole, TKey>"
                    )
                    {
                        isDbContext = true;
                        isIdentity = true;
                        if (current.TypeArguments.Length >= 3)
                        {
                            identityUserFqn = current
                                .TypeArguments[0]
                                .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                            identityRoleFqn = current
                                .TypeArguments[1]
                                .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                            identityKeyFqn = current
                                .TypeArguments[2]
                                .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        }
                        break;
                    }

                    if (baseFqn == "global::Microsoft.EntityFrameworkCore.DbContext")
                    {
                        isDbContext = true;
                        break;
                    }

                    current = current.BaseType;
                }

                if (!isDbContext)
                    continue;

                var info = new DbContextInfo
                {
                    FullyQualifiedName = typeSymbol.ToDisplayString(
                        SymbolDisplayFormat.FullyQualifiedFormat
                    ),
                    ModuleName = moduleName,
                    IsIdentityDbContext = isIdentity,
                    IdentityUserTypeFqn = identityUserFqn,
                    IdentityRoleTypeFqn = identityRoleFqn,
                    IdentityKeyTypeFqn = identityKeyFqn,
                    Location = GetSourceLocation(typeSymbol),
                };

                // Collect DbSet<T> properties
                foreach (var m in typeSymbol.GetMembers())
                {
                    if (
                        m is IPropertySymbol prop
                        && prop.DeclaredAccessibility == Accessibility.Public
                        && !prop.IsStatic
                        && prop.Type is INamedTypeSymbol propType
                        && propType.IsGenericType
                        && propType.OriginalDefinition.ToDisplayString(
                            SymbolDisplayFormat.FullyQualifiedFormat
                        ) == "global::Microsoft.EntityFrameworkCore.DbSet<TEntity>"
                    )
                    {
                        var entityFqn = propType
                            .TypeArguments[0]
                            .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        info.DbSets.Add(
                            new DbSetInfo { PropertyName = prop.Name, EntityFqn = entityFqn }
                        );
                    }
                }

                dbContexts.Add(info);
            }
        }
    }

    private static void FindEntityConfigTypes(
        INamespaceSymbol namespaceSymbol,
        string moduleName,
        List<EntityConfigInfo> entityConfigs,
        CancellationToken cancellationToken
    )
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (member is INamespaceSymbol childNamespace)
            {
                FindEntityConfigTypes(childNamespace, moduleName, entityConfigs, cancellationToken);
            }
            else if (
                member is INamedTypeSymbol typeSymbol
                && !typeSymbol.IsAbstract
                && !typeSymbol.IsStatic
            )
            {
                foreach (var iface in typeSymbol.AllInterfaces)
                {
                    if (
                        iface.IsGenericType
                        && iface.OriginalDefinition.ToDisplayString(
                            SymbolDisplayFormat.FullyQualifiedFormat
                        )
                            == "global::Microsoft.EntityFrameworkCore.IEntityTypeConfiguration<TEntity>"
                    )
                    {
                        var entityFqn = iface
                            .TypeArguments[0]
                            .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        entityConfigs.Add(
                            new EntityConfigInfo
                            {
                                ConfigFqn = typeSymbol.ToDisplayString(
                                    SymbolDisplayFormat.FullyQualifiedFormat
                                ),
                                EntityFqn = entityFqn,
                                ModuleName = moduleName,
                                Location = GetSourceLocation(typeSymbol),
                            }
                        );
                        break;
                    }
                }
            }
        }
    }

    private static void ScanContractInterfaces(
        INamespaceSymbol namespaceSymbol,
        string assemblyName,
        List<ContractInterfaceInfoRecord> results
    )
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            if (member is INamespaceSymbol childNs)
            {
                ScanContractInterfaces(childNs, assemblyName, results);
            }
            else if (
                member is INamedTypeSymbol typeSymbol
                && typeSymbol.TypeKind == TypeKind.Interface
                && typeSymbol.DeclaredAccessibility == Accessibility.Public
            )
            {
                var methodCount = 0;
                foreach (var m in typeSymbol.GetMembers())
                {
                    if (m is IMethodSymbol ms && ms.MethodKind == MethodKind.Ordinary)
                        methodCount++;
                }

                results.Add(
                    new ContractInterfaceInfoRecord(
                        assemblyName,
                        typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        methodCount,
                        GetSourceLocation(typeSymbol)
                    )
                );
            }
        }
    }

    private static void FindContractImplementations(
        INamespaceSymbol namespaceSymbol,
        HashSet<string> contractInterfaceFqns,
        string moduleName,
        Compilation compilation,
        List<ContractImplementationInfo> results
    )
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            if (member is INamespaceSymbol childNs)
            {
                FindContractImplementations(
                    childNs,
                    contractInterfaceFqns,
                    moduleName,
                    compilation,
                    results
                );
            }
            else if (member is INamedTypeSymbol typeSymbol && typeSymbol.TypeKind == TypeKind.Class)
            {
                foreach (var iface in typeSymbol.AllInterfaces)
                {
                    var ifaceFqn = iface.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    if (contractInterfaceFqns.Contains(ifaceFqn))
                    {
                        results.Add(
                            new ContractImplementationInfo
                            {
                                InterfaceFqn = ifaceFqn,
                                ImplementationFqn = typeSymbol.ToDisplayString(
                                    SymbolDisplayFormat.FullyQualifiedFormat
                                ),
                                ModuleName = moduleName,
                                IsPublic = typeSymbol.DeclaredAccessibility == Accessibility.Public,
                                IsAbstract = typeSymbol.IsAbstract,
                                DependsOnDbContext = HasDbContextConstructorParam(typeSymbol),
                                Location = GetSourceLocation(typeSymbol),
                                Lifetime = GetContractLifetime(typeSymbol),
                            }
                        );
                    }
                }
            }
        }
    }

    private static void FindPermissionClasses(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol modulePermissionsSymbol,
        string moduleName,
        List<PermissionClassInfo> results
    )
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            if (member is INamespaceSymbol childNs)
            {
                FindPermissionClasses(childNs, modulePermissionsSymbol, moduleName, results);
            }
            else if (
                member is INamedTypeSymbol typeSymbol
                && typeSymbol.TypeKind == TypeKind.Class
                && ImplementsInterface(typeSymbol, modulePermissionsSymbol)
            )
            {
                var info = new PermissionClassInfo
                {
                    FullyQualifiedName = typeSymbol.ToDisplayString(
                        SymbolDisplayFormat.FullyQualifiedFormat
                    ),
                    ModuleName = moduleName,
                    IsSealed = typeSymbol.IsSealed,
                    Location = GetSourceLocation(typeSymbol),
                };

                // Collect public const string fields
                foreach (var m in typeSymbol.GetMembers())
                {
                    if (
                        m is IFieldSymbol field
                        && field.DeclaredAccessibility == Accessibility.Public
                    )
                    {
                        info.Fields.Add(
                            new PermissionFieldInfo
                            {
                                FieldName = field.Name,
                                Value =
                                    field.HasConstantValue && field.ConstantValue is string s
                                        ? s
                                        : "",
                                IsConstString =
                                    field.IsConst
                                    && field.Type.SpecialType == SpecialType.System_String,
                                Location = GetSourceLocation(field),
                            }
                        );
                    }
                }

                results.Add(info);
            }
        }
    }

    private static void FindFeatureClasses(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol moduleFeaturesSymbol,
        string moduleName,
        List<FeatureClassInfo> results
    )
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            if (member is INamespaceSymbol childNs)
            {
                FindFeatureClasses(childNs, moduleFeaturesSymbol, moduleName, results);
            }
            else if (
                member is INamedTypeSymbol typeSymbol
                && typeSymbol.TypeKind == TypeKind.Class
                && ImplementsInterface(typeSymbol, moduleFeaturesSymbol)
            )
            {
                var info = new FeatureClassInfo
                {
                    FullyQualifiedName = typeSymbol.ToDisplayString(
                        SymbolDisplayFormat.FullyQualifiedFormat
                    ),
                    ModuleName = moduleName,
                    IsSealed = typeSymbol.IsSealed,
                    Location = GetSourceLocation(typeSymbol),
                };

                // Collect public const string fields
                foreach (var m in typeSymbol.GetMembers())
                {
                    if (
                        m is IFieldSymbol field
                        && field.DeclaredAccessibility == Accessibility.Public
                    )
                    {
                        info.Fields.Add(
                            new FeatureFieldInfo
                            {
                                FieldName = field.Name,
                                Value =
                                    field.HasConstantValue && field.ConstantValue is string s
                                        ? s
                                        : "",
                                IsConstString =
                                    field.IsConst
                                    && field.Type.SpecialType == SpecialType.System_String,
                                Location = GetSourceLocation(field),
                            }
                        );
                    }
                }

                results.Add(info);
            }
        }
    }

    private static void FindModuleOptionsClasses(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol moduleOptionsSymbol,
        string moduleName,
        List<ModuleOptionsRecord> results
    )
    {
        FindConcreteClassesImplementing(
            namespaceSymbol,
            moduleOptionsSymbol,
            typeSymbol =>
                results.Add(
                    new ModuleOptionsRecord(
                        typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        moduleName,
                        GetSourceLocation(typeSymbol)
                    )
                )
        );
    }

    /// <summary>
    /// Recursively walks namespaces and invokes <paramref name="onMatch"/> for each
    /// concrete (non-abstract, non-static) class that implements the given interface.
    /// </summary>
    private static void FindConcreteClassesImplementing(
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

    private static void FindInterceptorTypes(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol saveChangesInterceptorSymbol,
        string moduleName,
        List<InterceptorInfo> results
    )
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            if (member is INamespaceSymbol childNs)
            {
                FindInterceptorTypes(childNs, saveChangesInterceptorSymbol, moduleName, results);
            }
            else if (
                member is INamedTypeSymbol typeSymbol
                && typeSymbol.TypeKind == TypeKind.Class
                && !typeSymbol.IsAbstract
                && !typeSymbol.IsStatic
                && ImplementsInterface(typeSymbol, saveChangesInterceptorSymbol)
            )
            {
                var info = new InterceptorInfo
                {
                    FullyQualifiedName = typeSymbol.ToDisplayString(
                        SymbolDisplayFormat.FullyQualifiedFormat
                    ),
                    ModuleName = moduleName,
                    Location = GetSourceLocation(typeSymbol),
                };

                // Extract constructor parameter type FQNs
                foreach (var ctor in typeSymbol.Constructors)
                {
                    if (ctor.DeclaredAccessibility != Accessibility.Public)
                        continue;

                    foreach (var param in ctor.Parameters)
                    {
                        info.ConstructorParamTypeFqns.Add(
                            param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                        );
                    }

                    // Only process the first public constructor
                    break;
                }

                results.Add(info);
            }
        }
    }

    private static void FindConventionDtoTypes(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol? noDtoAttrSymbol,
        INamedTypeSymbol? eventInterfaceSymbol,
        HashSet<string> existingFqns,
        List<DtoTypeInfo> dtoTypes,
        CancellationToken cancellationToken
    )
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (member is INamespaceSymbol childNs)
            {
                FindConventionDtoTypes(
                    childNs,
                    noDtoAttrSymbol,
                    eventInterfaceSymbol,
                    existingFqns,
                    dtoTypes,
                    cancellationToken
                );
            }
            else if (
                member is INamedTypeSymbol typeSymbol
                && typeSymbol.DeclaredAccessibility == Accessibility.Public
                && !typeSymbol.IsStatic
                && typeSymbol.TypeKind != TypeKind.Interface
                && typeSymbol.TypeKind != TypeKind.Enum
                && typeSymbol.TypeKind != TypeKind.Delegate
            )
            {
                var fqn = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                // Skip if already found via [Dto]
                if (existingFqns.Contains(fqn))
                    continue;

                // Skip if [NoDtoGeneration]
                if (noDtoAttrSymbol is not null)
                {
                    var hasNoDtoAttr = false;
                    foreach (var attr in typeSymbol.GetAttributes())
                    {
                        if (
                            SymbolEqualityComparer.Default.Equals(
                                attr.AttributeClass,
                                noDtoAttrSymbol
                            )
                        )
                        {
                            hasNoDtoAttr = true;
                            break;
                        }
                    }
                    if (hasNoDtoAttr)
                        continue;
                }

                // Skip types that implement IEvent (events are not DTOs)
                if (eventInterfaceSymbol is not null)
                {
                    var isEvent = false;
                    foreach (var iface in typeSymbol.AllInterfaces)
                    {
                        if (SymbolEqualityComparer.Default.Equals(iface, eventInterfaceSymbol))
                        {
                            isEvent = true;
                            break;
                        }
                    }
                    if (isEvent)
                        continue;
                }

                // Skip generic type definitions (open generics like PagedResult<T>)
                if (typeSymbol.IsGenericType)
                    continue;

                // Skip Vogen-generated infrastructure types
                if (
                    typeSymbol.Name == "VogenTypesFactory"
                    || fqn.StartsWith("global::Vogen", StringComparison.Ordinal)
                )
                    continue;

                // Skip Vogen value objects — they have their own JsonConverter
                // and must not be treated as regular DTOs in the JSON resolver
                if (IsVogenValueObject(typeSymbol))
                    continue;

                var safeName = TypeMappingHelpers.StripGlobalPrefix(fqn).Replace(".", "_");

                existingFqns.Add(fqn);
                dtoTypes.Add(
                    new DtoTypeInfo
                    {
                        FullyQualifiedName = fqn,
                        SafeName = safeName,
                        Properties = ExtractDtoProperties(typeSymbol),
                    }
                );
            }
        }
    }

    private static List<DtoPropertyInfo> ExtractDtoProperties(INamedTypeSymbol typeSymbol)
    {
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
                var resolvedType = ResolveUnderlyingType(prop.Type);
                var actualType = prop.Type.ToDisplayString(
                    SymbolDisplayFormat.FullyQualifiedFormat
                );
                properties.Add(
                    new DtoPropertyInfo
                    {
                        Name = prop.Name,
                        TypeFqn = actualType,
                        UnderlyingTypeFqn = resolvedType != actualType ? resolvedType : null,
                        HasSetter =
                            prop.SetMethod is not null
                            && prop.SetMethod.DeclaredAccessibility == Accessibility.Public,
                    }
                );
            }
        }
        return properties;
    }

    private static void FindVogenValueObjectsWithEfConverters(
        INamespaceSymbol ns,
        List<VogenValueObjectRecord> results
    )
    {
        foreach (var type in ns.GetTypeMembers())
        {
            if (!IsVogenValueObject(type))
                continue;

            var converterMembers = type.GetTypeMembers("EfCoreValueConverter");
            var comparerMembers = type.GetTypeMembers("EfCoreValueComparer");

            if (converterMembers.Length == 0 || comparerMembers.Length == 0)
                continue;

            var typeFqn = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var converterFqn = converterMembers[0]
                .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var comparerFqn = comparerMembers[0]
                .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            results.Add(new VogenValueObjectRecord(typeFqn, converterFqn, comparerFqn));
        }

        foreach (var childNs in ns.GetNamespaceMembers())
        {
            FindVogenValueObjectsWithEfConverters(childNs, results);
        }
    }

    private static bool IsVogenValueObject(INamedTypeSymbol typeSymbol)
    {
        foreach (var attr in typeSymbol.GetAttributes())
        {
            var attrClass = attr.AttributeClass;
            if (
                attrClass is not null
                && attrClass.IsGenericType
                && attrClass.Name == "ValueObjectAttribute"
                && attrClass.ContainingNamespace.ToDisplayString() == "Vogen"
            )
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// If the type is a Vogen value object, returns the FQN of its underlying primitive type.
    /// Otherwise returns the type's own FQN.
    /// </summary>
    private static string ResolveUnderlyingType(ITypeSymbol typeSymbol)
    {
        foreach (var attr in typeSymbol.GetAttributes())
        {
            var attrClass = attr.AttributeClass;
            if (attrClass is null)
                continue;

            // Vogen uses generic attribute ValueObjectAttribute<T>
            if (
                attrClass.IsGenericType
                && attrClass.Name == "ValueObjectAttribute"
                && attrClass.ContainingNamespace.ToDisplayString() == "Vogen"
                && attrClass.TypeArguments.Length == 1
            )
            {
                return attrClass
                    .TypeArguments[0]
                    .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }
        }

        return typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    private static void FindImplementors(
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
                && ImplementsInterface(typeSymbol, interfaceSymbol)
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
}
