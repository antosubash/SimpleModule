# IViewEndpoint Interface Design

## Goal

Separate view endpoints (Inertia pages) from API endpoints by introducing an `IViewEndpoint` interface. View endpoints should be excluded from Swagger/OpenAPI documentation automatically.

## Design

### New Interface — `SimpleModule.Core.IViewEndpoint`

```csharp
public interface IViewEndpoint
{
    void Map(IEndpointRouteBuilder app);
}
```

Independent from `IEndpoint` (no inheritance). Same signature — acts as a marker for the source generator.

### Source Generator Changes (`ModuleDiscovererGenerator`)

1. Resolve `SimpleModule.Core.IViewEndpoint` symbol alongside `IEndpoint`
2. Replace namespace-based classification (`.Views.` check) with interface-based:
   - `IEndpoint` implementors → API endpoints
   - `IViewEndpoint` implementors → view endpoints
3. Page name derivation unchanged (strip `Endpoint`/`View` suffix from class name)
4. Generated `MapModuleEndpoints()`: view endpoint route groups get `.ExcludeFromDescription()` to hide from Swagger

### Module Migration

All existing view endpoints (in `Views/` folders across Products, Users, Dashboard, Orders) change from `IEndpoint` to `IViewEndpoint`.

### Unchanged

- `Inertia.Render()` usage
- Route grouping with `ViewPrefix`
- `ConfigureEndpoints()` escape hatch
- `GenerateViewPages()` output
