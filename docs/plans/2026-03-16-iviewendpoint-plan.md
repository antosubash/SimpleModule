# IViewEndpoint Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Separate view endpoints from API endpoints via a new `IViewEndpoint` marker interface, with automatic Swagger exclusion for views.

**Architecture:** Add `IViewEndpoint` to Core with identical signature to `IEndpoint`. Update the source generator to classify endpoints by interface type instead of namespace convention. View route groups get `.ExcludeFromDescription()` in generated code.

**Tech Stack:** C# / Roslyn IIncrementalGenerator / ASP.NET Core Minimal APIs / xUnit + FluentAssertions

---

### Task 1: Add IViewEndpoint interface to Core

**Files:**
- Create: `src/SimpleModule.Core/IViewEndpoint.cs`

**Step 1: Create the interface**

```csharp
using Microsoft.AspNetCore.Routing;

namespace SimpleModule.Core;

public interface IViewEndpoint
{
    void Map(IEndpointRouteBuilder app);
}
```

**Step 2: Verify build**

Run: `dotnet build src/SimpleModule.Core`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add src/SimpleModule.Core/IViewEndpoint.cs
git commit -m "feat: add IViewEndpoint marker interface"
```

---

### Task 2: Update source generator to use interface-based classification

**Files:**
- Modify: `src/SimpleModule.Generator/ModuleDiscovererGenerator.cs`

**Step 1: Resolve the IViewEndpoint symbol**

In `ExtractDiscoveryData`, after resolving `IEndpoint` (line ~56), add:

```csharp
var viewEndpointInterfaceSymbol = compilation.GetTypeByMetadataName(
    "SimpleModule.Core.IViewEndpoint"
);
```

**Step 2: Update FindEndpointTypes to accept both interface symbols**

Change the signature to:

```csharp
private static void FindEndpointTypes(
    INamespaceSymbol namespaceSymbol,
    INamedTypeSymbol endpointInterfaceSymbol,
    INamedTypeSymbol? viewEndpointInterfaceSymbol,
    string moduleName,
    List<EndpointInfo> endpoints,
    List<ViewInfo> views
)
```

**Step 3: Replace namespace-based classification with interface check**

Replace the body of the type classification (lines 275-314) with:

```csharp
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
            className = className.Substring(
                0,
                className.Length - "Endpoint".Length
            );
        else if (className.EndsWith("View", StringComparison.Ordinal))
            className = className.Substring(
                0,
                className.Length - "View".Length
            );

        views.Add(
            new ViewInfo
            {
                FullyQualifiedName = fqn,
                Page = moduleName + "/" + className,
            }
        );
    }
    else if (ImplementsInterface(typeSymbol, endpointInterfaceSymbol))
    {
        endpoints.Add(
            new EndpointInfo { FullyQualifiedName = fqn }
        );
    }
}
```

**Step 4: Update the call site in ExtractDiscoveryData**

Pass `viewEndpointInterfaceSymbol` to `FindEndpointTypes`:

```csharp
FindEndpointTypes(
    assembly.GlobalNamespace,
    endpointInterfaceSymbol,
    viewEndpointInterfaceSymbol,
    module.ModuleName,
    module.Endpoints,
    module.Views
);
```

**Step 5: Add `.ExcludeFromDescription()` to view route groups in GenerateEndpointExtensions**

In the generated view group line (currently line 548), change from:

```csharp
$"            var viewGroup = app.MapGroup(\"{module.ViewPrefix}\").WithTags(\"{module.ModuleName}\");"
```

to:

```csharp
$"            var viewGroup = app.MapGroup(\"{module.ViewPrefix}\").WithTags(\"{module.ModuleName}\").ExcludeFromDescription();"
```

Also add the `using Microsoft.AspNetCore.Http;` import at the top of the generated file (for `ExcludeFromDescription`). Add after the existing `using Microsoft.AspNetCore.Routing;` line:

```csharp
sb.AppendLine("using Microsoft.AspNetCore.Http;");
```

For views without a ViewPrefix (the else branch), add `.ExcludeFromDescription()` on the individual endpoint mappings isn't possible directly — but this case means views are mapped directly on `app`, which is unusual. Leave as-is for now since all modules use ViewPrefix.

**Step 6: Verify build**

Run: `dotnet build src/SimpleModule.Generator`
Expected: Build succeeded

**Step 7: Commit**

```bash
git add src/SimpleModule.Generator/ModuleDiscovererGenerator.cs
git commit -m "feat: classify endpoints by IViewEndpoint interface, exclude views from Swagger"
```

---

### Task 3: Update generator tests for IViewEndpoint

**Files:**
- Modify: `tests/SimpleModule.Generator.Tests/ViewDiscoveryTests.cs`

**Step 1: Update all test sources to use IViewEndpoint instead of IEndpoint in Views namespace**

In every test that has view endpoints in a `.Views.` namespace, change `IEndpoint` to `IViewEndpoint`. For example, in `EndpointInViewsNamespace_DiscoveredAsView_RoutedUnderViewPrefix`:

```csharp
namespace TestApp.Views
{
    public class CreateEndpoint : IViewEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapGet("/create", () => "create");
        }
    }
}
```

Apply this to all 5 tests that use view endpoints:
- `EndpointInViewsNamespace_DiscoveredAsView_RoutedUnderViewPrefix`
- `PageNameDerived_FromModuleNameAndClassName_StrippingEndpointSuffix`
- `ViewPages_GeneratesTypeScriptIndex`
- `ModuleWithViewsAndEndpoints_BothCoexist`
- `ViewClassName_WithViewSuffix_StrippedCorrectly`

**Step 2: Update assertions to expect `.ExcludeFromDescription()`**

In tests that check for `viewGroup`, update the assertion:

```csharp
endpointExt.Should().Contain("var viewGroup = app.MapGroup(\"/test\").WithTags(\"Test\").ExcludeFromDescription()");
```

**Step 3: Add a new test — IEndpoint in Views namespace is NOT discovered as view**

```csharp
[Fact]
public void IEndpointInViewsNamespace_DiscoveredAsEndpoint_NotView()
{
    var source = """
        using Microsoft.AspNetCore.Builder;
        using Microsoft.AspNetCore.Routing;
        using SimpleModule.Core;

        namespace TestApp
        {
            [Module("Test", RoutePrefix = "/api/test", ViewPrefix = "/test")]
            public class TestModule : IModule { }
        }

        namespace TestApp.Views
        {
            public class ListEndpoint : IEndpoint
            {
                public void Map(IEndpointRouteBuilder app)
                {
                    app.MapGet("/", () => "list");
                }
            }
        }
        """;

    var compilation = GeneratorTestHelper.CreateCompilation(source);
    var result = GeneratorTestHelper.RunGenerator(compilation);

    var endpointExt = result
        .GeneratedTrees.First(t =>
            t.FilePath.EndsWith("EndpointExtensions.g.cs", StringComparison.Ordinal)
        )
        .GetText()
        .ToString();

    endpointExt.Should().Contain("new global::TestApp.Views.ListEndpoint().Map(group)");
    endpointExt.Should().NotContain("viewGroup");
}
```

**Step 4: Run tests**

Run: `dotnet test tests/SimpleModule.Generator.Tests`
Expected: All tests pass

**Step 5: Commit**

```bash
git add tests/SimpleModule.Generator.Tests/ViewDiscoveryTests.cs
git commit -m "test: update view discovery tests for IViewEndpoint interface"
```

---

### Task 4: Migrate Products view endpoints to IViewEndpoint

**Files:**
- Modify: `src/modules/Products/src/Products/Views/BrowseEndpoint.cs`
- Modify: `src/modules/Products/src/Products/Views/ManageEndpoint.cs`
- Modify: `src/modules/Products/src/Products/Views/CreateEndpoint.cs`
- Modify: `src/modules/Products/src/Products/Views/EditEndpoint.cs`

**Step 1: In each file, change `IEndpoint` to `IViewEndpoint`**

Example for BrowseEndpoint.cs:

```csharp
public class BrowseEndpoint : IViewEndpoint
```

Same for ManageEndpoint, CreateEndpoint, EditEndpoint.

**Step 2: Verify build**

Run: `dotnet build src/modules/Products/src/Products`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add src/modules/Products/src/Products/Views/
git commit -m "refactor: migrate Products view endpoints to IViewEndpoint"
```

---

### Task 5: Full build and verify

**Step 1: Build the whole solution**

Run: `dotnet build`
Expected: Build succeeded

**Step 2: Run all tests**

Run: `dotnet test`
Expected: All tests pass

**Step 3: Commit (if any remaining changes)**

Only if needed.
