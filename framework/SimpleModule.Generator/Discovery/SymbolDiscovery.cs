using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static class SymbolDiscovery
{
    internal static DiscoveryData Extract(
        Compilation compilation,
        CancellationToken cancellationToken
    )
    {
        var hostAssemblyName = compilation.Assembly.Name;

        var symbols = CoreSymbols.TryResolve(compilation);
        if (symbols is null)
            return DiscoveryData.Empty;
        var s = symbols.Value;

        var modules = new List<ModuleInfo>();

        foreach (var reference in compilation.References)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (
                compilation.GetAssemblyOrModuleSymbol(reference)
                is not IAssemblySymbol assemblySymbol
            )
                continue;

            ModuleFinder.FindModuleTypes(
                assemblySymbol.GlobalNamespace,
                s,
                modules,
                cancellationToken
            );
        }

        ModuleFinder.FindModuleTypes(
            compilation.Assembly.GlobalNamespace,
            s,
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
            EndpointFinder.FindEndpointTypes(
                assembly.GlobalNamespace,
                s,
                rawEndpoints,
                rawViews,
                cancellationToken
            );

            // Match each endpoint/view to the module whose namespace is closest
            foreach (var ep in rawEndpoints)
            {
                var epFqn = TypeMappingHelpers.StripGlobalPrefix(ep.FullyQualifiedName);
                var ownerName = SymbolHelpers.FindClosestModuleName(epFqn, modules);
                var owner = modules.Find(m => m.ModuleName == ownerName);
                if (owner is not null)
                    owner.Endpoints.Add(ep);
            }

            // Pre-compute module namespace per module name for page inference
            var moduleNsByName = new Dictionary<string, string>();
            foreach (var m in modules)
            {
                if (!moduleNsByName.ContainsKey(m.ModuleName))
                {
                    var mFqn = TypeMappingHelpers.StripGlobalPrefix(m.FullyQualifiedName);
                    moduleNsByName[m.ModuleName] = TypeMappingHelpers.ExtractNamespace(mFqn);
                }
            }

            foreach (var v in rawViews)
            {
                var vFqn = TypeMappingHelpers.StripGlobalPrefix(v.FullyQualifiedName);
                var ownerName = SymbolHelpers.FindClosestModuleName(vFqn, modules);
                var owner = modules.Find(m => m.ModuleName == ownerName);
                if (owner is not null)
                {
                    // Derive page name from namespace segments between module NS and class name.
                    // e.g. SimpleModule.Users.Pages.Account.LoginEndpoint → Users/Account/Login
                    if (v.Page is null)
                    {
                        var moduleNs = moduleNsByName[ownerName];
                        var typeNs = TypeMappingHelpers.ExtractNamespace(vFqn);

                        // Extract segments after the module namespace, stripping Views/Pages
                        var remaining =
                            typeNs.Length > moduleNs.Length
                                ? typeNs.Substring(moduleNs.Length).TrimStart('.')
                                : "";

                        var segments = remaining.Split('.');
                        var pathParts = new List<string>();
                        foreach (var seg in segments)
                        {
                            if (
                                seg.Length > 0
                                && !seg.Equals("Views", StringComparison.Ordinal)
                                && !seg.Equals("Pages", StringComparison.Ordinal)
                            )
                            {
                                pathParts.Add(seg);
                            }
                        }

                        var subPath = pathParts.Count > 0 ? string.Join("/", pathParts) + "/" : "";
                        v.Page = ownerName + "/" + subPath + v.InferredClassName;
                    }

                    owner.Views.Add(v);
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
            DbContextFinder.FindDbContextTypes(
                assembly.GlobalNamespace,
                "",
                rawDbContexts,
                cancellationToken
            );
            DbContextFinder.FindEntityConfigTypes(
                assembly.GlobalNamespace,
                "",
                rawEntityConfigs,
                cancellationToken
            );

            // Match each DbContext to the module whose namespace is closest
            foreach (var ctx in rawDbContexts)
            {
                var ctxNs = TypeMappingHelpers.StripGlobalPrefix(ctx.FullyQualifiedName);
                ctx.ModuleName = SymbolHelpers.FindClosestModuleName(ctxNs, modules);
                dbContexts.Add(ctx);
            }

            foreach (var cfg in rawEntityConfigs)
            {
                var cfgNs = TypeMappingHelpers.StripGlobalPrefix(cfg.ConfigFqn);
                cfg.ModuleName = SymbolHelpers.FindClosestModuleName(cfgNs, modules);
                entityConfigs.Add(cfg);
            }
        }

        var dtoTypes = new List<DtoTypeInfo>();
        if (s.DtoAttribute is not null)
        {
            foreach (var reference in compilation.References)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (
                    compilation.GetAssemblyOrModuleSymbol(reference)
                    is not IAssemblySymbol assemblySymbol
                )
                    continue;

                DtoFinder.FindDtoTypes(
                    assemblySymbol.GlobalNamespace,
                    s.DtoAttribute,
                    dtoTypes,
                    cancellationToken
                );
            }

            DtoFinder.FindDtoTypes(
                compilation.Assembly.GlobalNamespace,
                s.DtoAttribute,
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
        var existingDtoFqns = new HashSet<string>();
        foreach (var d in dtoTypes)
            existingDtoFqns.Add(d.FullyQualifiedName);

        foreach (var kvp in contractsAssemblySymbols)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DtoFinder.FindConventionDtoTypes(
                kvp.Value.GlobalNamespace,
                s.NoDtoAttribute,
                s.EventInterface,
                existingDtoFqns,
                dtoTypes,
                cancellationToken
            );
        }

        // Step 3: Scan contract interfaces
        var contractInterfaces = new List<ContractInterfaceInfoRecord>();
        foreach (var kvp in contractsAssemblySymbols)
        {
            ContractFinder.ScanContractInterfaces(
                kvp.Value.GlobalNamespace,
                kvp.Key,
                contractInterfaces
            );
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
            ContractFinder.FindContractImplementations(
                moduleAssembly.GlobalNamespace,
                moduleContractInterfaceFqns,
                module.ModuleName,
                compilation,
                contractImplementations
            );
        }

        // Step 3c: Find IModulePermissions implementors in module and contracts assemblies
        var permissionClasses = new List<PermissionClassInfo>();
        if (s.ModulePermissions is not null)
        {
            foreach (var module in modules)
            {
                if (!moduleSymbols.TryGetValue(module.FullyQualifiedName, out var typeSymbol))
                    continue;

                var moduleAssembly = typeSymbol.ContainingAssembly;
                FindPermissionClasses(
                    moduleAssembly.GlobalNamespace,
                    s.ModulePermissions,
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
                        s.ModulePermissions,
                        moduleName,
                        permissionClasses
                    );
                }
            }
        }

        // Step 3d: Find IModuleFeatures implementors in module and contracts assemblies
        var featureClasses = new List<FeatureClassInfo>();
        if (s.ModuleFeatures is not null)
        {
            foreach (var module in modules)
            {
                if (!moduleSymbols.TryGetValue(module.FullyQualifiedName, out var typeSymbol))
                    continue;

                var moduleAssembly = typeSymbol.ContainingAssembly;
                FindFeatureClasses(
                    moduleAssembly.GlobalNamespace,
                    s.ModuleFeatures,
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
                        s.ModuleFeatures,
                        moduleName,
                        featureClasses
                    );
                }
            }
        }

        // Step 3e: Find ISaveChangesInterceptor implementors in module assemblies
        var interceptors = new List<InterceptorInfo>();
        if (s.SaveChangesInterceptor is not null)
        {
            SymbolHelpers.ScanModuleAssemblies(
                modules,
                moduleSymbols,
                (assembly, module) =>
                {
                    FindInterceptorTypes(
                        assembly.GlobalNamespace,
                        s.SaveChangesInterceptor,
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

        SymbolHelpers.ScanModuleAssemblies(
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
        if (s.ModuleOptions is not null)
        {
            SymbolHelpers.ScanModuleAssemblies(
                modules,
                moduleSymbols,
                (assembly, module) =>
                    FindModuleOptionsClasses(
                        assembly.GlobalNamespace,
                        s.ModuleOptions,
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
                        s.ModuleOptions,
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

        if (s.AgentDefinition is not null)
        {
            SymbolHelpers.ScanModuleAssemblies(
                modules,
                moduleSymbols,
                (assembly, module) =>
                    FindImplementors(
                        assembly.GlobalNamespace,
                        s.AgentDefinition,
                        module.ModuleName,
                        agentDefinitions
                    )
            );
        }

        if (s.AgentToolProvider is not null)
        {
            SymbolHelpers.ScanModuleAssemblies(
                modules,
                moduleSymbols,
                (assembly, module) =>
                    FindImplementors(
                        assembly.GlobalNamespace,
                        s.AgentToolProvider,
                        module.ModuleName,
                        agentToolProviders
                    )
            );
        }

        if (s.KnowledgeSource is not null)
        {
            SymbolHelpers.ScanModuleAssemblies(
                modules,
                moduleSymbols,
                (assembly, module) =>
                    FindImplementors(
                        assembly.GlobalNamespace,
                        s.KnowledgeSource,
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
                    m.AssemblyName,
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
                            e.AllowAnonymous,
                            e.RouteTemplate,
                            e.HttpMethod
                        ))
                        .ToImmutableArray(),
                    m.Views.Select(v => new ViewInfoRecord(
                            v.FullyQualifiedName,
                            v.Page ?? "",
                            v.RouteTemplate,
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
                    d.BaseTypeFqn,
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
                    c.DbSets.Select(d => new DbSetInfoRecord(
                            d.PropertyName,
                            d.EntityFqn,
                            d.EntityAssemblyName,
                            d.EntityLocation
                        ))
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
            contractsAssemblyMap.Keys.ToImmutableArray(),
            s.HasAgentsAssembly,
            hostAssemblyName
        );
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
                && SymbolHelpers.ImplementsInterface(typeSymbol, modulePermissionsSymbol)
            )
            {
                var info = new PermissionClassInfo
                {
                    FullyQualifiedName = typeSymbol.ToDisplayString(
                        SymbolDisplayFormat.FullyQualifiedFormat
                    ),
                    ModuleName = moduleName,
                    IsSealed = typeSymbol.IsSealed,
                    Location = SymbolHelpers.GetSourceLocation(typeSymbol),
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
                                Location = SymbolHelpers.GetSourceLocation(field),
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
                && SymbolHelpers.ImplementsInterface(typeSymbol, moduleFeaturesSymbol)
            )
            {
                var info = new FeatureClassInfo
                {
                    FullyQualifiedName = typeSymbol.ToDisplayString(
                        SymbolDisplayFormat.FullyQualifiedFormat
                    ),
                    ModuleName = moduleName,
                    IsSealed = typeSymbol.IsSealed,
                    Location = SymbolHelpers.GetSourceLocation(typeSymbol),
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
                                Location = SymbolHelpers.GetSourceLocation(field),
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
        SymbolHelpers.FindConcreteClassesImplementing(
            namespaceSymbol,
            moduleOptionsSymbol,
            typeSymbol =>
                results.Add(
                    new ModuleOptionsRecord(
                        typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        moduleName,
                        SymbolHelpers.GetSourceLocation(typeSymbol)
                    )
                )
        );
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
                && SymbolHelpers.ImplementsInterface(typeSymbol, saveChangesInterceptorSymbol)
            )
            {
                var info = new InterceptorInfo
                {
                    FullyQualifiedName = typeSymbol.ToDisplayString(
                        SymbolDisplayFormat.FullyQualifiedFormat
                    ),
                    ModuleName = moduleName,
                    Location = SymbolHelpers.GetSourceLocation(typeSymbol),
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

    internal static bool IsVogenValueObject(INamedTypeSymbol typeSymbol)
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
    internal static string ResolveUnderlyingType(ITypeSymbol typeSymbol)
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
}
