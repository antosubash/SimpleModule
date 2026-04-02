# P1 Implementation Plan

## P1-1: Decompose IModule Wide Interface (ISP Violation)

### Problem
`IModule` has 10 methods covering 7 unrelated concerns (DI, routing, middleware, menu, permissions, settings, feature flags, lifecycle, health). Adding a new hook requires changes across Core, Generator discovery, emitter code, and diagnostic checks.

### Approach: Focused Capability Interfaces

Keep `IModule` as a lean marker interface (just `[Module]` attribute discovery). Extract each concern into an opt-in interface:

```
IModule                        — marker, discovered by [Module] attribute
├── IModuleServices            — ConfigureServices(IServiceCollection, IConfiguration)
├── IModuleMiddleware          — ConfigureMiddleware(IApplicationBuilder)
├── IModuleMenu                — ConfigureMenu(IMenuBuilder)
├── IModulePermissions         — ConfigurePermissions(PermissionRegistryBuilder)  
├── IModuleSettings            — ConfigureSettings(ISettingsBuilder)
├── IModuleFeatureFlags        — ConfigureFeatureFlags(IFeatureFlagBuilder)
├── IModuleLifecycle           — OnStartAsync, OnStopAsync, CheckHealthAsync
└── ConfigureEndpoints stays   — escape hatch, already has HasConfigureEndpoints bool
```

### Files to Change

| File | Change |
|------|--------|
| `framework/SimpleModule.Core/IModule.cs` | Keep as marker with `ConfigureEndpoints` only. Extract 7 new interfaces. Optionally keep default methods on `IModule` for backward compat during transition. |
| `framework/SimpleModule.Core/IModuleServices.cs` (new) | `ConfigureServices(IServiceCollection, IConfiguration)` |
| `framework/SimpleModule.Core/IModuleMiddleware.cs` (new) | `ConfigureMiddleware(IApplicationBuilder)` |
| `framework/SimpleModule.Core/IModuleMenu.cs` (new) | `ConfigureMenu(IMenuBuilder)` |
| `framework/SimpleModule.Core/IModuleSettings.cs` (new) | `ConfigureSettings(ISettingsBuilder)` |
| `framework/SimpleModule.Core/IModuleFeatureFlags.cs` (new) | `ConfigureFeatureFlags(IFeatureFlagBuilder)` |
| `framework/SimpleModule.Core/IModuleLifecycle.cs` (new) | `OnStartAsync`, `OnStopAsync`, `CheckHealthAsync` |
| `framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs` | Replace `DeclaresMethod` checks with `ImplementsInterface` checks for each sub-interface |
| `framework/SimpleModule.Generator/Emitters/ModuleExtensionsEmitter.cs` | Cast to sub-interface before calling: `if (module is IModuleServices svc) svc.ConfigureServices(...)` |
| `framework/SimpleModule.Generator/Emitters/MenuExtensionsEmitter.cs` | Same pattern — cast to `IModuleMenu` |
| `framework/SimpleModule.Generator/Emitters/SettingsExtensionsEmitter.cs` | Cast to `IModuleSettings` |
| `framework/SimpleModule.Generator/Emitters/HostingExtensionsEmitter.cs` | Cast to `IModuleMiddleware` |
| `framework/SimpleModule.Generator/Emitters/DiagnosticEmitter.cs` | SM0043 (empty module) checks sub-interface implementation instead of `HasConfigure*` bools |
| `framework/SimpleModule.Core/Hosting/ModuleLifecycleHostedService.cs` | Cast to `IModuleLifecycle` instead of calling directly on `IModule` |
| All existing module classes | Implement the new sub-interfaces (can do `IModule, IModuleServices, IModuleMenu`) |

### Migration Strategy
- **Phase 1**: Add new interfaces. Keep default methods on `IModule` so existing modules don't break.
- **Phase 2**: Generator detects both `IModule` method overrides AND sub-interface implementation.
- **Phase 3**: Deprecate methods on `IModule`, migrate modules, remove defaults.

### Risk: High effort, touching every module. Recommend Phase 1+2 first for backward compat.

---

## P1-2: Fix Inertia Serialization to Use DI JSON Options

### Problem
`InertiaResult` uses a private static `JsonSerializerOptions` with only `CamelCase` naming. The DI-registered `HttpJsonOptions` includes `ModulesJsonResolver` (Vogen value objects, DTO type info). This means API endpoints and Inertia view endpoints serialize the same DTO differently.

### Approach: Resolve Options from DI at Render Time

### Files to Change

| File | Change |
|------|--------|
| `framework/SimpleModule.Core/Inertia/InertiaResult.cs:15-18` | Remove static `_camelCaseOptions` field |
| `framework/SimpleModule.Core/Inertia/InertiaResult.cs:50,55,80` | Replace `_camelCaseOptions` with resolved options |
| `framework/SimpleModule.Core/Inertia/InertiaResult.cs` (ExecuteAsync) | Resolve `IOptions<JsonOptions>` from `httpContext.RequestServices`, clone options, ensure `CamelCase` naming is set, cache per-request |

### Implementation Detail

```csharp
// In ExecuteAsync:
var jsonOptions = httpContext.RequestServices
    .GetRequiredService<IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>>();
var options = new JsonSerializerOptions(jsonOptions.Value.SerializerOptions)
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
};
```

To avoid creating a new `JsonSerializerOptions` on every request, cache the merged options in a static `Lazy<>` initialized on first use from the `IServiceProvider`, or use a dedicated singleton service `IInertiaJsonOptionsProvider` registered at startup.

### Risk: Low. Single file change, clear fix path.

---

## P1-3: Support Configurable Contract Implementation Lifetime

### Problem
The generator always emits `services.AddScoped<IFoo, FooImpl>()`. Module authors who need Singleton or Transient must bypass auto-discovery.

### Approach: New `[ContractLifetime]` Attribute

### Files to Change

| File | Change |
|------|--------|
| `framework/SimpleModule.Core/ContractLifetimeAttribute.cs` (new) | `[ContractLifetime(ServiceLifetime.Singleton)]` attribute targeting classes |
| `framework/SimpleModule.Generator/Discovery/DiscoveryData.cs:288` | Add `int Lifetime` field to `ContractImplementationRecord` (use int instead of enum since generator targets netstandard2.0 and can't reference `ServiceLifetime`) |
| `framework/SimpleModule.Generator/Discovery/DiscoveryData.cs` (mutable) | Add `int Lifetime` to `ContractImplementationInfo` |
| `framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs` (~line 868) | In `FindContractImplementations`, check for `[ContractLifetime]` attribute, read `ServiceLifetime` value. Default to 1 (Scoped) if absent. |
| `framework/SimpleModule.Generator/Emitters/ModuleExtensionsEmitter.cs:104` | Emit `AddScoped`, `AddSingleton`, or `AddTransient` based on `impl.Lifetime` |

### Implementation Detail

```csharp
// Core attribute:
[AttributeUsage(AttributeTargets.Class)]
public sealed class ContractLifetimeAttribute(ServiceLifetime lifetime) : Attribute
{
    public ServiceLifetime Lifetime { get; } = lifetime;
}

// Generator emitter (line 104):
var method = impl.Lifetime switch
{
    0 => "AddSingleton",  // ServiceLifetime.Singleton = 0
    2 => "AddTransient",  // ServiceLifetime.Transient = 2
    _ => "AddScoped",     // ServiceLifetime.Scoped = 1 (default)
};
sb.AppendLine($"            services.{method}<{impl.InterfaceFqn}, {impl.ImplementationFqn}>();");
```

### Risk: Low. Additive change, backward compatible (no attribute = Scoped).

---

## P1-4: Add Event Bus Pipeline Behavior

### Problem
`EventBus.PublishAsync` dispatches directly to handlers with no interception points. No way to add logging, metrics, retries, or transaction boundaries at the bus level. `BackgroundEventChannel` uses unbounded channel with no backpressure.

### Approach: MediatR-style `IEventPipelineBehavior<T>`

### Files to Change

| File | Change |
|------|--------|
| `framework/SimpleModule.Core/Events/IEventPipelineBehavior.cs` (new) | Pipeline behavior interface |
| `framework/SimpleModule.Core/Events/EventBus.cs:113-142` | Resolve `IEventPipelineBehavior<T>` from DI, build middleware chain, invoke before handler loop |
| `framework/SimpleModule.Core/Events/BackgroundEventChannel.cs:9` | Change `Channel.CreateUnbounded` to `Channel.CreateBounded` with configurable capacity |

### Implementation Detail

```csharp
// New interface:
public interface IEventPipelineBehavior<in T> where T : IEvent
{
    Task HandleAsync(T @event, Func<Task> next, CancellationToken cancellationToken);
}

// In EventBus.PublishAsync:
var behaviors = _serviceProvider.GetServices<IEventPipelineBehavior<T>>();
Task DispatchToHandlers() => InvokeHandlers(handlers, @event, cancellationToken);

var pipeline = behaviors.Reverse().Aggregate(
    (Func<Task>)DispatchToHandlers,
    (next, behavior) => () => behavior.HandleAsync(@event, next, cancellationToken)
);
await pipeline();
```

For backpressure:
```csharp
// BackgroundEventChannel.cs:
Channel.CreateBounded<Func<IServiceProvider, CancellationToken, Task>>(
    new BoundedChannelOptions(1000) { FullMode = BoundedChannelFullMode.Wait }
);
```

### Risk: Medium. The pipeline pattern is additive (no behaviors = no overhead). Bounded channel needs capacity tuning consideration.

---

## P1-5: Enforce Endpoint Attributes in Generated Code

### Problem
The generator discovers `[AllowAnonymous]` and `[RequirePermission("...")]` on endpoint classes but doesn't emit the corresponding routing calls. The group-level `.RequireAuthorization()` applies to all endpoints, and individual overrides must be done manually inside `Map()`.

### Approach: Emit Authorization Modifiers After `Map()` Call

### Prerequisite
`IEndpoint.Map()` and `IViewEndpoint.Map()` currently return `void`. To chain `.AllowAnonymous()` or `.RequireAuthorization()`, the `Map` method must return a value (`IEndpointConventionBuilder`). This is a **breaking change** to the endpoint interface.

**Alternative (non-breaking)**: Instead of changing the interface, apply authorization at the **route group level per-endpoint** by having the generator wrap each endpoint in its own sub-group with the appropriate policy. Or, scan attributes and apply them to the group before calling `Map()`.

### Recommended Approach: Post-Map Wrapper

Since we can't easily change `Map()` return type without breaking all endpoints, use a different strategy: have the generator emit attribute-based endpoint filters on the route group.

Actually, the simplest non-breaking approach: the generator should emit per-endpoint sub-groups:

```csharp
// Generated for an [AllowAnonymous] endpoint:
{
    var epGroup = group.MapGroup("");
    epGroup.AllowAnonymous();
    new BrowseEndpoint().Map(epGroup);
}

// Generated for [RequirePermission("Products.Edit")] endpoint:
{
    var epGroup = group.MapGroup("");
    epGroup.RequireAuthorization(new RequirePermissionAttribute("Products.Edit"));
    new EditEndpoint().Map(epGroup);
}
```

### Files to Change

| File | Change |
|------|--------|
| `framework/SimpleModule.Generator/Emitters/EndpointExtensionsEmitter.cs:47-48` | For endpoints with `AllowAnonymous` or `RequiredPermissions`, wrap in a sub-group with the appropriate authorization |
| `framework/SimpleModule.Generator/Emitters/EndpointExtensionsEmitter.cs:80-81` | Same for view endpoints |

### Implementation Detail

```csharp
// In the emitter, for each endpoint:
if (endpoint.AllowAnonymous)
{
    sb.AppendLine($"            {{ var _eg = group.MapGroup(\"\"); _eg.AllowAnonymous(); new {endpoint.FullyQualifiedName}().Map(_eg); }}");
}
else if (endpoint.RequiredPermissions.Length > 0)
{
    var perms = string.Join("\", \"", endpoint.RequiredPermissions);
    sb.AppendLine($"            {{ var _eg = group.MapGroup(\"\"); _eg.RequirePermission(\"{perms}\"); new {endpoint.FullyQualifiedName}().Map(_eg); }}");
}
else
{
    sb.AppendLine($"            new {endpoint.FullyQualifiedName}().Map(group);");
}
```

### Risk: Low-Medium. The sub-group pattern is a known ASP.NET Minimal API technique. Need to verify it doesn't create issues with route resolution or Swagger grouping.

---

## Priority & Sequencing

| Order | Issue | Effort | Breaking? | Dependencies |
|-------|-------|--------|-----------|-------------|
| 1 | **P1-3** Contract lifetime | Low | No | None |
| 2 | **P1-2** Inertia JSON fix | Low | No | None |
| 3 | **P1-5** Endpoint enforcement | Low-Med | No | None |
| 4 | **P1-4** Event bus pipeline | Medium | No | None |
| 5 | **P1-1** IModule decomposition | High | Yes (phased) | All modules |

Start with P1-3 and P1-2 (quick wins), then P1-5 (moderate), then P1-4 (design-heavy), and finally P1-1 (major refactor needing phased rollout).
