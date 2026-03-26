# CLI Improvements Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extend `sm` CLI with 7 new doctor checks, full-stack feature scaffolding (Views + Pages/index.ts), dry-run mode on all `new` commands, and tree output / progress spinners.

**Architecture:** New doctor checks implement `IDoctorCheck` and get registered in `DoctorCommand`. Feature scaffolding extends `NewFeatureCommand` with two new artifacts (React view + Pages/index.ts entry). Dry-run is a cross-cutting flag that collects `(path, FileAction)` pairs instead of writing files, then renders a Spectre.Console `Tree`.

**Tech Stack:** C# 13 / .NET 10, Spectre.Console 0.49, xUnit.v3, FluentAssertions, System.Xml.Linq

---

## File Map

### New files
| Path | Responsibility |
|---|---|
| `cli/SimpleModule.Cli/Infrastructure/FileAction.cs` | Enum: `Create` / `Modify` — used by dry-run tree |
| `cli/SimpleModule.Cli/Commands/Doctor/Checks/ContractsIsolationCheck.cs` | FAIL if contracts csproj refs anything besides Core |
| `cli/SimpleModule.Cli/Commands/Doctor/Checks/ModuleAttributeCheck.cs` | FAIL if module class missing `[Module]` attribute with RoutePrefix |
| `cli/SimpleModule.Cli/Commands/Doctor/Checks/ViewEndpointNamingCheck.cs` | WARN if endpoint classes don't follow naming/location convention |
| `cli/SimpleModule.Cli/Commands/Doctor/Checks/PagesRegistryCheck.cs` | FAIL if Inertia.Render call has no matching Pages/index.ts entry |
| `cli/SimpleModule.Cli/Commands/Doctor/Checks/ViteConfigCheck.cs` | WARN if vite.config.ts missing or not configured for library mode |
| `cli/SimpleModule.Cli/Commands/Doctor/Checks/PackageJsonCheck.cs` | WARN if React/Inertia listed as `dependencies` instead of `peerDependencies` |
| `cli/SimpleModule.Cli/Commands/Doctor/Checks/NpmWorkspaceCheck.cs` | FAIL if module not covered by root package.json workspaces |
| `cli/SimpleModule.Cli/Infrastructure/PagesRegistryFixer.cs` | Adds stub entry to Pages/index.ts (used by `--fix`) |
| `cli/SimpleModule.Cli/Infrastructure/NpmWorkspaceFixer.cs` | Adds workspace glob to root package.json (used by `--fix`) |
| `tests/SimpleModule.Cli.Tests/ContractsIsolationCheckTests.cs` | Tests for ContractsIsolationCheck |
| `tests/SimpleModule.Cli.Tests/ModuleAttributeCheckTests.cs` | Tests for ModuleAttributeCheck |
| `tests/SimpleModule.Cli.Tests/ViewEndpointNamingCheckTests.cs` | Tests for ViewEndpointNamingCheck |
| `tests/SimpleModule.Cli.Tests/PagesRegistryCheckTests.cs` | Tests for PagesRegistryCheck |
| `tests/SimpleModule.Cli.Tests/ViteConfigCheckTests.cs` | Tests for ViteConfigCheck |
| `tests/SimpleModule.Cli.Tests/PackageJsonCheckTests.cs` | Tests for PackageJsonCheck |
| `tests/SimpleModule.Cli.Tests/NpmWorkspaceCheckTests.cs` | Tests for NpmWorkspaceCheck |
| `tests/SimpleModule.Cli.Tests/PagesRegistryFixerTests.cs` | Tests for PagesRegistryFixer |
| `tests/SimpleModule.Cli.Tests/NewFeatureViewScaffoldingTests.cs` | Tests for View + Pages/index.ts generation |
| `tests/SimpleModule.Cli.Tests/DryRunTests.cs` | Tests for dry-run flag behavior |

### Modified files
| Path | Change |
|---|---|
| `cli/SimpleModule.Cli/Infrastructure/SolutionContext.cs` | Add `GetModuleViewsPath()`, `GetModulePagesIndexPath()` |
| `cli/SimpleModule.Cli/Commands/Doctor/Checks/CsprojConventionCheck.cs` | Remove duplicate contracts-isolation warning (superseded by `ContractsIsolationCheck`) |
| `cli/SimpleModule.Cli/Commands/Doctor/DoctorCommand.cs` | Register 7 new checks + 2 new fix dispatchers |
| `cli/SimpleModule.Cli/Templates/FeatureTemplates.cs` | Add `ViewComponent()` method |
| `cli/SimpleModule.Cli/Commands/New/NewFeatureSettings.cs` | Add `--dry-run`, `--no-view` flags |
| `cli/SimpleModule.Cli/Commands/New/NewFeatureCommand.cs` | Add Views + Pages/index.ts generation, dry-run, tree output |
| `cli/SimpleModule.Cli/Commands/New/NewModuleSettings.cs` | Add `--dry-run` flag |
| `cli/SimpleModule.Cli/Commands/New/NewModuleCommand.cs` | Add spinner, tree output, dry-run |
| `cli/SimpleModule.Cli/Commands/New/NewProjectSettings.cs` | Add `--dry-run` flag |
| `cli/SimpleModule.Cli/Commands/New/NewProjectCommand.cs` | Add dry-run, tree output, improved error messages |

---

## Task 1: Foundations — FileAction enum + SolutionContext helpers

**Files:**
- Create: `cli/SimpleModule.Cli/Infrastructure/FileAction.cs`
- Modify: `cli/SimpleModule.Cli/Infrastructure/SolutionContext.cs`
- Test: `tests/SimpleModule.Cli.Tests/SolutionContextTests.cs`

- [ ] **Step 1: Write failing tests for new SolutionContext helpers**

Add to `tests/SimpleModule.Cli.Tests/SolutionContextTests.cs`:

```csharp
[Fact]
public void GetModuleViewsPath_ReturnsCorrectPath()
{
    File.WriteAllText(Path.Combine(_tempDir, "Test.slnx"), "<Solution />");

    var ctx = SolutionContext.Discover(_tempDir)!;

    ctx.GetModuleViewsPath("Products")
        .Should()
        .Be(Path.Combine(_tempDir, "src", "modules", "Products", "src", "Products", "Views"));
}

[Fact]
public void GetModulePagesIndexPath_ReturnsCorrectPath()
{
    File.WriteAllText(Path.Combine(_tempDir, "Test.slnx"), "<Solution />");

    var ctx = SolutionContext.Discover(_tempDir)!;

    ctx.GetModulePagesIndexPath("Products")
        .Should()
        .Be(Path.Combine(_tempDir, "src", "modules", "Products", "src", "Products", "Pages", "index.ts"));
}
```

- [ ] **Step 2: Run tests to verify they fail**

```
dotnet test tests/SimpleModule.Cli.Tests/ --filter "GetModuleViewsPath_ReturnsCorrectPath|GetModulePagesIndexPath_ReturnsCorrectPath"
```

Expected: FAIL — `GetModuleViewsPath` and `GetModulePagesIndexPath` not defined.

- [ ] **Step 3: Create FileAction enum**

Create `cli/SimpleModule.Cli/Infrastructure/FileAction.cs`:

```csharp
namespace SimpleModule.Cli.Infrastructure;

public enum FileAction
{
    Create,
    Modify,
}
```

- [ ] **Step 4: Add helpers to SolutionContext**

In `cli/SimpleModule.Cli/Infrastructure/SolutionContext.cs`, add after `GetTestProjectPath`:

```csharp
public string GetModuleViewsPath(string moduleName) =>
    Path.Combine(ModulesPath, moduleName, "src", moduleName, "Views");

public string GetModulePagesIndexPath(string moduleName) =>
    Path.Combine(ModulesPath, moduleName, "src", moduleName, "Pages", "index.ts");
```

- [ ] **Step 5: Run tests to verify they pass**

```
dotnet test tests/SimpleModule.Cli.Tests/ --filter "GetModuleViewsPath_ReturnsCorrectPath|GetModulePagesIndexPath_ReturnsCorrectPath"
```

Expected: PASS.

- [ ] **Step 6: Commit**

```
git add cli/SimpleModule.Cli/Infrastructure/FileAction.cs cli/SimpleModule.Cli/Infrastructure/SolutionContext.cs tests/SimpleModule.Cli.Tests/SolutionContextTests.cs
git commit -m "feat(cli): add FileAction enum and SolutionContext path helpers"
```

---

## Task 2: ContractsIsolationCheck

**Files:**
- Create: `cli/SimpleModule.Cli/Commands/Doctor/Checks/ContractsIsolationCheck.cs`
- Modify: `cli/SimpleModule.Cli/Commands/Doctor/Checks/CsprojConventionCheck.cs`
- Test: `tests/SimpleModule.Cli.Tests/ContractsIsolationCheckTests.cs`

- [ ] **Step 1: Write failing tests**

Create `tests/SimpleModule.Cli.Tests/ContractsIsolationCheckTests.cs`:

```csharp
using FluentAssertions;
using SimpleModule.Cli.Commands.Doctor.Checks;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Tests;

public sealed class ContractsIsolationCheckTests : IDisposable
{
    private readonly string _tempDir;

    public ContractsIsolationCheckTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"sm-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true);
    }

    private SolutionContext CreateSolution(string moduleName, string contractsCsprojContent)
    {
        File.WriteAllText(Path.Combine(_tempDir, "Test.slnx"), "<Solution />");
        var modulesDir = Path.Combine(_tempDir, "src", "modules");
        var contractsDir = Path.Combine(modulesDir, moduleName, "src", $"{moduleName}.Contracts");
        Directory.CreateDirectory(contractsDir);
        File.WriteAllText(Path.Combine(contractsDir, $"{moduleName}.Contracts.csproj"), contractsCsprojContent);
        return SolutionContext.Discover(_tempDir)!;
    }

    [Fact]
    public void Run_Pass_WhenContractsOnlyRefsCore()
    {
        var solution = CreateSolution("Products", """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <ProjectReference Include="..\..\..\..\src\SimpleModule.Core\SimpleModule.Core.csproj" />
              </ItemGroup>
            </Project>
            """);

        var results = new ContractsIsolationCheck().Run(solution).ToList();

        results.Should().ContainSingle(r =>
            r.Name == "Products.Contracts isolation" && r.Status == CheckStatus.Pass);
    }

    [Fact]
    public void Run_Fail_WhenContractsRefsAnotherModuleImpl()
    {
        var solution = CreateSolution("Products", """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <ProjectReference Include="..\..\..\..\src\SimpleModule.Core\SimpleModule.Core.csproj" />
                <ProjectReference Include="..\..\..\Orders\src\Orders\Orders.csproj" />
              </ItemGroup>
            </Project>
            """);

        var results = new ContractsIsolationCheck().Run(solution).ToList();

        results.Should().ContainSingle(r =>
            r.Name == "Products.Contracts isolation" && r.Status == CheckStatus.Fail);
    }

    [Fact]
    public void Run_Warning_WhenContractsCsprojNotFound()
    {
        File.WriteAllText(Path.Combine(_tempDir, "Test.slnx"), "<Solution />");
        var modulesDir = Path.Combine(_tempDir, "src", "modules");
        Directory.CreateDirectory(Path.Combine(modulesDir, "Products"));
        var solution = SolutionContext.Discover(_tempDir)!;

        var results = new ContractsIsolationCheck().Run(solution).ToList();

        results.Should().ContainSingle(r =>
            r.Name == "Products.Contracts isolation" && r.Status == CheckStatus.Warning);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```
dotnet test tests/SimpleModule.Cli.Tests/ --filter "ContractsIsolationCheckTests"
```

Expected: FAIL — `ContractsIsolationCheck` not defined.

- [ ] **Step 3: Implement ContractsIsolationCheck**

Create `cli/SimpleModule.Cli/Commands/Doctor/Checks/ContractsIsolationCheck.cs`:

```csharp
using System.Xml.Linq;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Commands.Doctor.Checks;

public sealed class ContractsIsolationCheck : IDoctorCheck
{
    public IEnumerable<CheckResult> Run(SolutionContext solution)
    {
        foreach (var module in solution.ExistingModules)
        {
            var contractsCsproj = Path.Combine(
                solution.GetModuleContractsPath(module),
                $"{module}.Contracts.csproj"
            );

            if (!File.Exists(contractsCsproj))
            {
                yield return new CheckResult(
                    $"{module}.Contracts isolation",
                    CheckStatus.Warning,
                    $"{module}.Contracts.csproj not found"
                );
                continue;
            }

            var doc = XDocument.Load(contractsCsproj);
            var nonCoreRefs = doc.Root!
                .Descendants("ProjectReference")
                .Select(pr => pr.Attribute("Include")?.Value ?? "")
                .Where(r => !r.Contains("Core", StringComparison.OrdinalIgnoreCase)
                         && !r.Contains("Contracts", StringComparison.OrdinalIgnoreCase))
                .ToList();

            yield return nonCoreRefs.Count == 0
                ? new CheckResult(
                    $"{module}.Contracts isolation",
                    CheckStatus.Pass,
                    "references Core only"
                )
                : new CheckResult(
                    $"{module}.Contracts isolation",
                    CheckStatus.Fail,
                    $"references non-Core projects: {string.Join(", ", nonCoreRefs.Select(Path.GetFileNameWithoutExtension))}"
                );
        }
    }
}
```

- [ ] **Step 4: Remove duplicate warning from CsprojConventionCheck**

In `cli/SimpleModule.Cli/Commands/Doctor/Checks/CsprojConventionCheck.cs`, remove the entire contracts-refs block (lines that check `onlyRefsCore`). The `ContractsIsolationCheck` now owns this. The method body for the contracts section should become:

```csharp
// Contracts isolation is checked by ContractsIsolationCheck
```

Specifically, delete this block from `CsprojConventionCheck.Run`:

```csharp
var contractsCsproj = Path.Combine(
    solution.GetModuleContractsPath(module),
    $"{module}.Contracts.csproj"
);
if (File.Exists(contractsCsproj))
{
    var doc = XDocument.Load(contractsCsproj);
    var refs = doc.Root!.Descendants("ProjectReference")
        .Select(pr => pr.Attribute("Include")?.Value ?? "")
        .ToList();

    var onlyRefsCore = refs.All(r =>
        r.Contains("Core", StringComparison.OrdinalIgnoreCase)
    );
    yield return onlyRefsCore
        ? new CheckResult(
            $"{module}.Contracts refs",
            CheckStatus.Pass,
            "references Core only"
        )
        : new CheckResult(
            $"{module}.Contracts refs",
            CheckStatus.Warning,
            "contracts project references more than just Core"
        );
}
```

- [ ] **Step 5: Run tests to verify they pass**

```
dotnet test tests/SimpleModule.Cli.Tests/ --filter "ContractsIsolationCheckTests"
```

Expected: PASS.

- [ ] **Step 6: Commit**

```
git add cli/SimpleModule.Cli/Commands/Doctor/Checks/ContractsIsolationCheck.cs cli/SimpleModule.Cli/Commands/Doctor/Checks/CsprojConventionCheck.cs tests/SimpleModule.Cli.Tests/ContractsIsolationCheckTests.cs
git commit -m "feat(cli): add ContractsIsolationCheck doctor check"
```

---

## Task 3: ModuleAttributeCheck

**Files:**
- Create: `cli/SimpleModule.Cli/Commands/Doctor/Checks/ModuleAttributeCheck.cs`
- Test: `tests/SimpleModule.Cli.Tests/ModuleAttributeCheckTests.cs`

- [ ] **Step 1: Write failing tests**

Create `tests/SimpleModule.Cli.Tests/ModuleAttributeCheckTests.cs`:

```csharp
using FluentAssertions;
using SimpleModule.Cli.Commands.Doctor.Checks;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Tests;

public sealed class ModuleAttributeCheckTests : IDisposable
{
    private readonly string _tempDir;

    public ModuleAttributeCheckTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"sm-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true);
    }

    private SolutionContext CreateSolutionWithModule(string moduleName, string? moduleClassContent = null)
    {
        File.WriteAllText(Path.Combine(_tempDir, "Test.slnx"), "<Solution />");
        var moduleDir = Path.Combine(_tempDir, "src", "modules", moduleName, "src", moduleName);
        Directory.CreateDirectory(moduleDir);
        if (moduleClassContent is not null)
            File.WriteAllText(Path.Combine(moduleDir, $"{moduleName}Module.cs"), moduleClassContent);
        return SolutionContext.Discover(_tempDir)!;
    }

    [Fact]
    public void Run_Pass_WhenModuleAttributePresentWithRoutePrefix()
    {
        var solution = CreateSolutionWithModule("Products", """
            [Module("Products", RoutePrefix = "products")]
            public class ProductsModule : IModule { }
            """);

        var results = new ModuleAttributeCheck().Run(solution).ToList();

        results.Should().ContainSingle(r =>
            r.Name == "Products [Module] attribute" && r.Status == CheckStatus.Pass);
    }

    [Fact]
    public void Run_Fail_WhenModuleClassHasNoModuleAttribute()
    {
        var solution = CreateSolutionWithModule("Products", """
            public class ProductsModule : IModule { }
            """);

        var results = new ModuleAttributeCheck().Run(solution).ToList();

        results.Should().ContainSingle(r =>
            r.Name == "Products [Module] attribute" && r.Status == CheckStatus.Fail);
    }

    [Fact]
    public void Run_Warning_WhenModuleClassFileMissing()
    {
        var solution = CreateSolutionWithModule("Products", moduleClassContent: null);

        var results = new ModuleAttributeCheck().Run(solution).ToList();

        results.Should().ContainSingle(r =>
            r.Name == "Products [Module] attribute" && r.Status == CheckStatus.Warning);
    }

    [Fact]
    public void Run_Fail_WhenModuleAttributeMissingRoutePrefix()
    {
        var solution = CreateSolutionWithModule("Products", """
            [Module("Products")]
            public class ProductsModule : IModule { }
            """);

        var results = new ModuleAttributeCheck().Run(solution).ToList();

        results.Should().ContainSingle(r =>
            r.Name == "Products [Module] attribute" && r.Status == CheckStatus.Fail);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```
dotnet test tests/SimpleModule.Cli.Tests/ --filter "ModuleAttributeCheckTests"
```

Expected: FAIL — `ModuleAttributeCheck` not defined.

- [ ] **Step 3: Implement ModuleAttributeCheck**

Create `cli/SimpleModule.Cli/Commands/Doctor/Checks/ModuleAttributeCheck.cs`:

```csharp
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Commands.Doctor.Checks;

public sealed class ModuleAttributeCheck : IDoctorCheck
{
    public IEnumerable<CheckResult> Run(SolutionContext solution)
    {
        foreach (var module in solution.ExistingModules)
        {
            var moduleClassPath = Path.Combine(
                solution.GetModuleProjectPath(module),
                $"{module}Module.cs"
            );

            if (!File.Exists(moduleClassPath))
            {
                yield return new CheckResult(
                    $"{module} [Module] attribute",
                    CheckStatus.Warning,
                    $"{module}Module.cs not found — skipping attribute check"
                );
                continue;
            }

            var content = File.ReadAllText(moduleClassPath);
            var hasAttribute = content.Contains("[Module(", StringComparison.Ordinal);
            var hasRoutePrefix = content.Contains("RoutePrefix", StringComparison.Ordinal);

            yield return (hasAttribute && hasRoutePrefix)
                ? new CheckResult(
                    $"{module} [Module] attribute",
                    CheckStatus.Pass,
                    "[Module] attribute with RoutePrefix present"
                )
                : new CheckResult(
                    $"{module} [Module] attribute",
                    CheckStatus.Fail,
                    hasAttribute
                        ? "[Module] attribute present but RoutePrefix is missing"
                        : $"{module}Module.cs missing [Module] attribute"
                );
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```
dotnet test tests/SimpleModule.Cli.Tests/ --filter "ModuleAttributeCheckTests"
```

Expected: PASS.

- [ ] **Step 5: Commit**

```
git add cli/SimpleModule.Cli/Commands/Doctor/Checks/ModuleAttributeCheck.cs tests/SimpleModule.Cli.Tests/ModuleAttributeCheckTests.cs
git commit -m "feat(cli): add ModuleAttributeCheck doctor check"
```

---

## Task 4: ViewEndpointNamingCheck

**Files:**
- Create: `cli/SimpleModule.Cli/Commands/Doctor/Checks/ViewEndpointNamingCheck.cs`
- Test: `tests/SimpleModule.Cli.Tests/ViewEndpointNamingCheckTests.cs`

- [ ] **Step 1: Write failing tests**

Create `tests/SimpleModule.Cli.Tests/ViewEndpointNamingCheckTests.cs`:

```csharp
using FluentAssertions;
using SimpleModule.Cli.Commands.Doctor.Checks;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Tests;

public sealed class ViewEndpointNamingCheckTests : IDisposable
{
    private readonly string _tempDir;

    public ViewEndpointNamingCheckTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"sm-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true);
    }

    private SolutionContext CreateSolutionWithEndpoints(string moduleName, params string[] endpointFileNames)
    {
        File.WriteAllText(Path.Combine(_tempDir, "Test.slnx"), "<Solution />");
        var endpointsDir = Path.Combine(_tempDir, "src", "modules", moduleName, "src", moduleName, "Endpoints", moduleName);
        Directory.CreateDirectory(endpointsDir);
        foreach (var name in endpointFileNames)
            File.WriteAllText(Path.Combine(endpointsDir, name), "// stub");
        return SolutionContext.Discover(_tempDir)!;
    }

    [Fact]
    public void Run_Pass_WhenAllEndpointFilesFollowConvention()
    {
        var solution = CreateSolutionWithEndpoints("Products", "GetAllEndpoint.cs", "CreateEndpoint.cs");

        var results = new ViewEndpointNamingCheck().Run(solution).ToList();

        results.Should().OnlyContain(r => r.Status == CheckStatus.Pass);
    }

    [Fact]
    public void Run_Warn_WhenEndpointFileDoesNotEndWithEndpoint()
    {
        var solution = CreateSolutionWithEndpoints("Products", "GetProducts.cs");

        var results = new ViewEndpointNamingCheck().Run(solution).ToList();

        results.Should().ContainSingle(r =>
            r.Name.Contains("GetProducts") && r.Status == CheckStatus.Warning);
    }

    [Fact]
    public void Run_Pass_WhenNoEndpointsDirectory()
    {
        File.WriteAllText(Path.Combine(_tempDir, "Test.slnx"), "<Solution />");
        var moduleDir = Path.Combine(_tempDir, "src", "modules", "Products", "src", "Products");
        Directory.CreateDirectory(moduleDir);
        var solution = SolutionContext.Discover(_tempDir)!;

        var results = new ViewEndpointNamingCheck().Run(solution).ToList();

        results.Should().BeEmpty();
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```
dotnet test tests/SimpleModule.Cli.Tests/ --filter "ViewEndpointNamingCheckTests"
```

Expected: FAIL — `ViewEndpointNamingCheck` not defined.

- [ ] **Step 3: Implement ViewEndpointNamingCheck**

Create `cli/SimpleModule.Cli/Commands/Doctor/Checks/ViewEndpointNamingCheck.cs`:

```csharp
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Commands.Doctor.Checks;

public sealed class ViewEndpointNamingCheck : IDoctorCheck
{
    public IEnumerable<CheckResult> Run(SolutionContext solution)
    {
        foreach (var module in solution.ExistingModules)
        {
            var endpointsDir = Path.Combine(
                solution.GetModuleProjectPath(module),
                "Endpoints",
                module
            );

            if (!Directory.Exists(endpointsDir))
                continue;

            foreach (var file in Directory.GetFiles(endpointsDir, "*.cs"))
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                // Skip validators — they follow a different convention
                if (fileName.EndsWith("Validator", StringComparison.Ordinal))
                    continue;

                yield return fileName.EndsWith("Endpoint", StringComparison.Ordinal)
                    ? new CheckResult(
                        $"{module}/{fileName}",
                        CheckStatus.Pass,
                        "follows Endpoint naming convention"
                    )
                    : new CheckResult(
                        $"{module}/{fileName}",
                        CheckStatus.Warning,
                        $"'{fileName}' does not end with 'Endpoint' — rename to '{fileName}Endpoint'"
                    );
            }
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```
dotnet test tests/SimpleModule.Cli.Tests/ --filter "ViewEndpointNamingCheckTests"
```

Expected: PASS.

- [ ] **Step 5: Commit**

```
git add cli/SimpleModule.Cli/Commands/Doctor/Checks/ViewEndpointNamingCheck.cs tests/SimpleModule.Cli.Tests/ViewEndpointNamingCheckTests.cs
git commit -m "feat(cli): add ViewEndpointNamingCheck doctor check"
```

---

## Task 5: PagesRegistryCheck + PagesRegistryFixer

**Files:**
- Create: `cli/SimpleModule.Cli/Commands/Doctor/Checks/PagesRegistryCheck.cs`
- Create: `cli/SimpleModule.Cli/Infrastructure/PagesRegistryFixer.cs`
- Test: `tests/SimpleModule.Cli.Tests/PagesRegistryCheckTests.cs`
- Test: `tests/SimpleModule.Cli.Tests/PagesRegistryFixerTests.cs`

- [ ] **Step 1: Write failing tests for PagesRegistryCheck**

Create `tests/SimpleModule.Cli.Tests/PagesRegistryCheckTests.cs`:

```csharp
using FluentAssertions;
using SimpleModule.Cli.Commands.Doctor.Checks;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Tests;

public sealed class PagesRegistryCheckTests : IDisposable
{
    private readonly string _tempDir;

    public PagesRegistryCheckTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"sm-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true);
    }

    private SolutionContext CreateSolution(
        string moduleName,
        string[]? csFiles = null,
        string? pagesIndexContent = null)
    {
        File.WriteAllText(Path.Combine(_tempDir, "Test.slnx"), "<Solution />");
        var moduleDir = Path.Combine(_tempDir, "src", "modules", moduleName, "src", moduleName);
        var pagesDir = Path.Combine(moduleDir, "Pages");
        Directory.CreateDirectory(pagesDir);

        if (csFiles is not null)
        {
            var endpointsDir = Path.Combine(moduleDir, "Endpoints", moduleName);
            Directory.CreateDirectory(endpointsDir);
            foreach (var (fileName, content) in csFiles.Select((c, i) => ($"Endpoint{i}.cs", c)))
                File.WriteAllText(Path.Combine(endpointsDir, fileName), content);
        }

        if (pagesIndexContent is not null)
            File.WriteAllText(Path.Combine(pagesDir, "index.ts"), pagesIndexContent);

        return SolutionContext.Discover(_tempDir)!;
    }

    [Fact]
    public void Run_Pass_WhenAllInertiaCallsHavePageEntry()
    {
        var solution = CreateSolution(
            "Products",
            csFiles: [@"Inertia.Render(""Products/Browse"", props)"],
            pagesIndexContent: """
                export const pages: Record<string, any> = {
                    "Products/Browse": () => import("../Views/Browse"),
                };
                """);

        var results = new PagesRegistryCheck().Run(solution).ToList();

        results.Should().ContainSingle(r =>
            r.Name == "Pages -> Products/Browse" && r.Status == CheckStatus.Pass);
    }

    [Fact]
    public void Run_Fail_WhenInertiaCallHasNoPageEntry()
    {
        var solution = CreateSolution(
            "Products",
            csFiles: [@"Inertia.Render(""Products/Browse"", props)"],
            pagesIndexContent: """
                export const pages: Record<string, any> = {};
                """);

        var results = new PagesRegistryCheck().Run(solution).ToList();

        results.Should().ContainSingle(r =>
            r.Name == "Pages -> Products/Browse" && r.Status == CheckStatus.Fail);
    }

    [Fact]
    public void Run_Fail_WhenPagesIndexTsMissing()
    {
        var solution = CreateSolution(
            "Products",
            csFiles: [@"Inertia.Render(""Products/Browse"", props)"],
            pagesIndexContent: null);

        var results = new PagesRegistryCheck().Run(solution).ToList();

        results.Should().ContainSingle(r =>
            r.Name == "Pages -> Products/Browse" && r.Status == CheckStatus.Fail);
    }

    [Fact]
    public void Run_Pass_WhenNoInertiaCallsExist()
    {
        var solution = CreateSolution(
            "Products",
            csFiles: ["// no inertia calls here"],
            pagesIndexContent: "export const pages: Record<string, any> = {};");

        var results = new PagesRegistryCheck().Run(solution).ToList();

        results.Should().BeEmpty();
    }
}
```

- [ ] **Step 2: Write failing tests for PagesRegistryFixer**

Create `tests/SimpleModule.Cli.Tests/PagesRegistryFixerTests.cs`:

```csharp
using FluentAssertions;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Tests;

public sealed class PagesRegistryFixerTests : IDisposable
{
    private readonly string _tempDir;

    public PagesRegistryFixerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"sm-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void AddEntry_AppendsToExistingPagesIndex()
    {
        var indexPath = Path.Combine(_tempDir, "index.ts");
        File.WriteAllText(indexPath, """
            export const pages: Record<string, any> = {
                "Products/Browse": () => import("../Views/Browse"),
            };
            """);

        PagesRegistryFixer.AddEntry(indexPath, "Products/Create", "../Views/Create");

        var content = File.ReadAllText(indexPath);
        content.Should().Contain("\"Products/Create\": () => import(\"../Views/Create\")");
        content.Should().Contain("\"Products/Browse\"");
    }

    [Fact]
    public void AddEntry_CreatesFileFromScratchWhenMissing()
    {
        var indexPath = Path.Combine(_tempDir, "index.ts");

        PagesRegistryFixer.AddEntry(indexPath, "Products/Create", "../Views/Create");

        var content = File.ReadAllText(indexPath);
        content.Should().Contain("export const pages");
        content.Should().Contain("\"Products/Create\": () => import(\"../Views/Create\")");
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

```
dotnet test tests/SimpleModule.Cli.Tests/ --filter "PagesRegistryCheckTests|PagesRegistryFixerTests"
```

Expected: FAIL — types not defined.

- [ ] **Step 4: Implement PagesRegistryCheck**

Create `cli/SimpleModule.Cli/Commands/Doctor/Checks/PagesRegistryCheck.cs`:

```csharp
using System.Text.RegularExpressions;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Commands.Doctor.Checks;

public sealed partial class PagesRegistryCheck : IDoctorCheck
{
    [GeneratedRegex(@"Inertia\.Render\s*\(\s*""([^""]+)""")]
    private static partial Regex InertiaRenderPattern();

    [GeneratedRegex(@"""([^""]+)""\s*:\s*(?:\(\s*\)|(?:async\s*)?\(\s*\)\s*=>|import)")]
    private static partial Regex PagesKeyPattern();

    public IEnumerable<CheckResult> Run(SolutionContext solution)
    {
        foreach (var module in solution.ExistingModules)
        {
            var moduleDir = solution.GetModuleProjectPath(module);
            if (!Directory.Exists(moduleDir))
                continue;

            var csFiles = Directory.GetFiles(moduleDir, "*.cs", SearchOption.AllDirectories);
            var inertiaComponents = csFiles
                .SelectMany(f => InertiaRenderPattern().Matches(File.ReadAllText(f)))
                .Select(m => m.Groups[1].Value)
                .Distinct()
                .ToList();

            if (inertiaComponents.Count == 0)
                continue;

            var indexPath = solution.GetModulePagesIndexPath(module);
            var registeredKeys = new HashSet<string>(StringComparer.Ordinal);

            if (File.Exists(indexPath))
            {
                var indexContent = File.ReadAllText(indexPath);
                foreach (Match m in PagesKeyPattern().Matches(indexContent))
                    registeredKeys.Add(m.Groups[1].Value);
            }

            foreach (var component in inertiaComponents)
            {
                yield return registeredKeys.Contains(component)
                    ? new CheckResult(
                        $"Pages -> {component}",
                        CheckStatus.Pass,
                        "registered in Pages/index.ts"
                    )
                    : new CheckResult(
                        $"Pages -> {component}",
                        CheckStatus.Fail,
                        $"'{component}' used in Inertia.Render but missing from Pages/index.ts"
                    );
            }
        }
    }
}
```

- [ ] **Step 5: Implement PagesRegistryFixer**

Create `cli/SimpleModule.Cli/Infrastructure/PagesRegistryFixer.cs`:

```csharp
namespace SimpleModule.Cli.Infrastructure;

public static class PagesRegistryFixer
{
    public static void AddEntry(string indexPath, string componentKey, string importPath)
    {
        if (!File.Exists(indexPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(indexPath)!);
            File.WriteAllText(indexPath, $$"""
                export const pages: Record<string, any> = {
                    "{{componentKey}}": () => import("{{importPath}}"),
                };
                """);
            return;
        }

        var content = File.ReadAllText(indexPath);
        var entry = $"    \"{componentKey}\": () => import(\"{importPath}\"),";

        // Find last closing } of the pages object and insert before it
        var lastBrace = content.LastIndexOf('}');
        if (lastBrace < 0)
        {
            File.AppendAllText(indexPath, $"\n{entry}");
            return;
        }

        // Insert before the last }
        var newContent = content[..lastBrace] + entry + "\n" + content[lastBrace..];
        File.WriteAllText(indexPath, newContent);
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

```
dotnet test tests/SimpleModule.Cli.Tests/ --filter "PagesRegistryCheckTests|PagesRegistryFixerTests"
```

Expected: PASS.

- [ ] **Step 7: Commit**

```
git add cli/SimpleModule.Cli/Commands/Doctor/Checks/PagesRegistryCheck.cs cli/SimpleModule.Cli/Infrastructure/PagesRegistryFixer.cs tests/SimpleModule.Cli.Tests/PagesRegistryCheckTests.cs tests/SimpleModule.Cli.Tests/PagesRegistryFixerTests.cs
git commit -m "feat(cli): add PagesRegistryCheck and PagesRegistryFixer"
```

---

## Task 6: ViteConfigCheck

**Files:**
- Create: `cli/SimpleModule.Cli/Commands/Doctor/Checks/ViteConfigCheck.cs`
- Test: `tests/SimpleModule.Cli.Tests/ViteConfigCheckTests.cs`

- [ ] **Step 1: Write failing tests**

Create `tests/SimpleModule.Cli.Tests/ViteConfigCheckTests.cs`:

```csharp
using FluentAssertions;
using SimpleModule.Cli.Commands.Doctor.Checks;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Tests;

public sealed class ViteConfigCheckTests : IDisposable
{
    private readonly string _tempDir;

    public ViteConfigCheckTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"sm-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true);
    }

    private SolutionContext CreateSolutionWithViteConfig(string moduleName, string? viteConfigContent)
    {
        File.WriteAllText(Path.Combine(_tempDir, "Test.slnx"), "<Solution />");
        var moduleDir = Path.Combine(_tempDir, "src", "modules", moduleName, "src", moduleName);
        Directory.CreateDirectory(moduleDir);
        if (viteConfigContent is not null)
            File.WriteAllText(Path.Combine(moduleDir, "vite.config.ts"), viteConfigContent);
        return SolutionContext.Discover(_tempDir)!;
    }

    [Fact]
    public void Run_Pass_WhenViteConfigCorrect()
    {
        var solution = CreateSolutionWithViteConfig("Products", """
            import { defineConfig } from 'vite'
            export default defineConfig({
              build: { lib: { entry: 'Pages/index.ts' } },
              external: ['react', 'react-dom', '@inertiajs/react'],
            })
            """);

        var results = new ViteConfigCheck().Run(solution).ToList();

        results.Should().ContainSingle(r =>
            r.Name == "Products vite.config.ts" && r.Status == CheckStatus.Pass);
    }

    [Fact]
    public void Run_Warn_WhenViteConfigMissing()
    {
        var solution = CreateSolutionWithViteConfig("Products", viteConfigContent: null);

        var results = new ViteConfigCheck().Run(solution).ToList();

        results.Should().ContainSingle(r =>
            r.Name == "Products vite.config.ts" && r.Status == CheckStatus.Warning);
    }

    [Fact]
    public void Run_Warn_WhenLibModeNotConfigured()
    {
        var solution = CreateSolutionWithViteConfig("Products", """
            import { defineConfig } from 'vite'
            export default defineConfig({})
            """);

        var results = new ViteConfigCheck().Run(solution).ToList();

        results.Should().ContainSingle(r =>
            r.Name == "Products vite.config.ts" && r.Status == CheckStatus.Warning);
    }

    [Fact]
    public void Run_Warn_WhenExternalsIncomplete()
    {
        var solution = CreateSolutionWithViteConfig("Products", """
            build: { lib: { entry: 'Pages/index.ts' } },
            external: ['react'],
            """);

        var results = new ViteConfigCheck().Run(solution).ToList();

        results.Should().ContainSingle(r =>
            r.Name == "Products vite.config.ts" && r.Status == CheckStatus.Warning);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```
dotnet test tests/SimpleModule.Cli.Tests/ --filter "ViteConfigCheckTests"
```

Expected: FAIL — `ViteConfigCheck` not defined.

- [ ] **Step 3: Implement ViteConfigCheck**

Create `cli/SimpleModule.Cli/Commands/Doctor/Checks/ViteConfigCheck.cs`:

```csharp
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Commands.Doctor.Checks;

public sealed class ViteConfigCheck : IDoctorCheck
{
    private static readonly string[] RequiredExternals = ["react", "react-dom", "@inertiajs/react"];

    public IEnumerable<CheckResult> Run(SolutionContext solution)
    {
        foreach (var module in solution.ExistingModules)
        {
            var viteConfigPath = Path.Combine(
                solution.GetModuleProjectPath(module),
                "vite.config.ts"
            );

            if (!File.Exists(viteConfigPath))
            {
                yield return new CheckResult(
                    $"{module} vite.config.ts",
                    CheckStatus.Warning,
                    "vite.config.ts not found — module won't build as a library"
                );
                continue;
            }

            var content = File.ReadAllText(viteConfigPath);
            var hasLibMode = content.Contains("lib:", StringComparison.Ordinal);
            var missingExternals = RequiredExternals
                .Where(e => !content.Contains(e, StringComparison.Ordinal))
                .ToList();

            if (hasLibMode && missingExternals.Count == 0)
            {
                yield return new CheckResult(
                    $"{module} vite.config.ts",
                    CheckStatus.Pass,
                    "library mode configured with correct externals"
                );
            }
            else
            {
                var issues = new List<string>();
                if (!hasLibMode) issues.Add("missing lib mode");
                if (missingExternals.Count > 0) issues.Add($"missing externals: {string.Join(", ", missingExternals)}");

                yield return new CheckResult(
                    $"{module} vite.config.ts",
                    CheckStatus.Warning,
                    string.Join("; ", issues)
                );
            }
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```
dotnet test tests/SimpleModule.Cli.Tests/ --filter "ViteConfigCheckTests"
```

Expected: PASS.

- [ ] **Step 5: Commit**

```
git add cli/SimpleModule.Cli/Commands/Doctor/Checks/ViteConfigCheck.cs tests/SimpleModule.Cli.Tests/ViteConfigCheckTests.cs
git commit -m "feat(cli): add ViteConfigCheck doctor check"
```

---

## Task 7: PackageJsonCheck

**Files:**
- Create: `cli/SimpleModule.Cli/Commands/Doctor/Checks/PackageJsonCheck.cs`
- Test: `tests/SimpleModule.Cli.Tests/PackageJsonCheckTests.cs`

- [ ] **Step 1: Write failing tests**

Create `tests/SimpleModule.Cli.Tests/PackageJsonCheckTests.cs`:

```csharp
using FluentAssertions;
using SimpleModule.Cli.Commands.Doctor.Checks;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Tests;

public sealed class PackageJsonCheckTests : IDisposable
{
    private readonly string _tempDir;

    public PackageJsonCheckTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"sm-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true);
    }

    private SolutionContext CreateSolutionWithPackageJson(string moduleName, string? packageJsonContent)
    {
        File.WriteAllText(Path.Combine(_tempDir, "Test.slnx"), "<Solution />");
        var moduleDir = Path.Combine(_tempDir, "src", "modules", moduleName, "src", moduleName);
        Directory.CreateDirectory(moduleDir);
        if (packageJsonContent is not null)
            File.WriteAllText(Path.Combine(moduleDir, "package.json"), packageJsonContent);
        return SolutionContext.Discover(_tempDir)!;
    }

    [Fact]
    public void Run_Pass_WhenReactAndInertiaAreInPeerDeps()
    {
        var solution = CreateSolutionWithPackageJson("Products", """
            {
              "peerDependencies": {
                "react": "^19.0.0",
                "@inertiajs/react": "^2.0.0"
              }
            }
            """);

        var results = new PackageJsonCheck().Run(solution).ToList();

        results.Should().ContainSingle(r =>
            r.Name == "Products package.json" && r.Status == CheckStatus.Pass);
    }

    [Fact]
    public void Run_Warn_WhenReactIsInDependenciesNotPeerDeps()
    {
        var solution = CreateSolutionWithPackageJson("Products", """
            {
              "dependencies": { "react": "^19.0.0" },
              "peerDependencies": { "@inertiajs/react": "^2.0.0" }
            }
            """);

        var results = new PackageJsonCheck().Run(solution).ToList();

        results.Should().ContainSingle(r =>
            r.Name == "Products package.json" && r.Status == CheckStatus.Warning);
    }

    [Fact]
    public void Run_Warn_WhenPackageJsonMissing()
    {
        var solution = CreateSolutionWithPackageJson("Products", packageJsonContent: null);

        var results = new PackageJsonCheck().Run(solution).ToList();

        results.Should().ContainSingle(r =>
            r.Name == "Products package.json" && r.Status == CheckStatus.Warning);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```
dotnet test tests/SimpleModule.Cli.Tests/ --filter "PackageJsonCheckTests"
```

Expected: FAIL — `PackageJsonCheck` not defined.

- [ ] **Step 3: Implement PackageJsonCheck**

Create `cli/SimpleModule.Cli/Commands/Doctor/Checks/PackageJsonCheck.cs`:

```csharp
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Commands.Doctor.Checks;

public sealed class PackageJsonCheck : IDoctorCheck
{
    private static readonly string[] RequiredPeerDeps = ["react", "@inertiajs/react"];

    public IEnumerable<CheckResult> Run(SolutionContext solution)
    {
        foreach (var module in solution.ExistingModules)
        {
            var packageJsonPath = Path.Combine(
                solution.GetModuleProjectPath(module),
                "package.json"
            );

            if (!File.Exists(packageJsonPath))
            {
                yield return new CheckResult(
                    $"{module} package.json",
                    CheckStatus.Warning,
                    "package.json not found"
                );
                continue;
            }

            var content = File.ReadAllText(packageJsonPath);

            // Check peerDependencies section contains required packages
            var peerDepsStart = content.IndexOf("\"peerDependencies\"", StringComparison.Ordinal);
            var missingFromPeerDeps = RequiredPeerDeps
                .Where(dep =>
                {
                    var inPeerDeps = peerDepsStart >= 0 &&
                        content.IndexOf($"\"{dep}\"", peerDepsStart, StringComparison.Ordinal) >= 0;
                    return !inPeerDeps;
                })
                .ToList();

            // Check if any required packages are wrongly in dependencies
            var depsStart = content.IndexOf("\"dependencies\"", StringComparison.Ordinal);
            var inWrongSection = peerDepsStart >= 0 ? RequiredPeerDeps
                .Where(dep =>
                {
                    var inDeps = depsStart >= 0 && depsStart < peerDepsStart &&
                        content.IndexOf($"\"{dep}\"", depsStart, StringComparison.Ordinal) is var idx &&
                        idx >= 0 && idx < peerDepsStart;
                    return inDeps;
                })
                .ToList() : [];

            if (missingFromPeerDeps.Count == 0 && inWrongSection.Count == 0)
            {
                yield return new CheckResult(
                    $"{module} package.json",
                    CheckStatus.Pass,
                    "React and @inertiajs/react declared as peerDependencies"
                );
            }
            else
            {
                var issues = new List<string>();
                if (missingFromPeerDeps.Count > 0)
                    issues.Add($"missing from peerDependencies: {string.Join(", ", missingFromPeerDeps)}");
                if (inWrongSection.Count > 0)
                    issues.Add($"should be peerDependencies, not dependencies: {string.Join(", ", inWrongSection)}");

                yield return new CheckResult(
                    $"{module} package.json",
                    CheckStatus.Warning,
                    string.Join("; ", issues)
                );
            }
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```
dotnet test tests/SimpleModule.Cli.Tests/ --filter "PackageJsonCheckTests"
```

Expected: PASS.

- [ ] **Step 5: Commit**

```
git add cli/SimpleModule.Cli/Commands/Doctor/Checks/PackageJsonCheck.cs tests/SimpleModule.Cli.Tests/PackageJsonCheckTests.cs
git commit -m "feat(cli): add PackageJsonCheck doctor check"
```

---

## Task 8: NpmWorkspaceCheck + NpmWorkspaceFixer

**Files:**
- Create: `cli/SimpleModule.Cli/Commands/Doctor/Checks/NpmWorkspaceCheck.cs`
- Create: `cli/SimpleModule.Cli/Infrastructure/NpmWorkspaceFixer.cs`
- Test: `tests/SimpleModule.Cli.Tests/NpmWorkspaceCheckTests.cs`

- [ ] **Step 1: Write failing tests**

Create `tests/SimpleModule.Cli.Tests/NpmWorkspaceCheckTests.cs`:

```csharp
using FluentAssertions;
using SimpleModule.Cli.Commands.Doctor.Checks;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Tests;

public sealed class NpmWorkspaceCheckTests : IDisposable
{
    private readonly string _tempDir;

    public NpmWorkspaceCheckTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"sm-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true);
    }

    private SolutionContext CreateSolution(string moduleName, string? rootPackageJson)
    {
        File.WriteAllText(Path.Combine(_tempDir, "Test.slnx"), "<Solution />");
        var modulesDir = Path.Combine(_tempDir, "src", "modules");
        Directory.CreateDirectory(Path.Combine(modulesDir, moduleName));
        if (rootPackageJson is not null)
            File.WriteAllText(Path.Combine(_tempDir, "package.json"), rootPackageJson);
        return SolutionContext.Discover(_tempDir)!;
    }

    [Fact]
    public void Run_Pass_WhenModuleCoveredByWorkspaceGlob()
    {
        var solution = CreateSolution("Products", """
            {
              "workspaces": ["src/modules/*/src/*"]
            }
            """);

        var results = new NpmWorkspaceCheck().Run(solution).ToList();

        results.Should().ContainSingle(r =>
            r.Name == "NpmWorkspace -> Products" && r.Status == CheckStatus.Pass);
    }

    [Fact]
    public void Run_Fail_WhenModuleNotInWorkspaces()
    {
        var solution = CreateSolution("Products", """
            {
              "workspaces": ["packages/*"]
            }
            """);

        var results = new NpmWorkspaceCheck().Run(solution).ToList();

        results.Should().ContainSingle(r =>
            r.Name == "NpmWorkspace -> Products" && r.Status == CheckStatus.Fail);
    }

    [Fact]
    public void Run_Warning_WhenRootPackageJsonMissing()
    {
        var solution = CreateSolution("Products", rootPackageJson: null);

        var results = new NpmWorkspaceCheck().Run(solution).ToList();

        results.Should().ContainSingle(r =>
            r.Name == "NpmWorkspace -> Products" && r.Status == CheckStatus.Warning);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```
dotnet test tests/SimpleModule.Cli.Tests/ --filter "NpmWorkspaceCheckTests"
```

Expected: FAIL — `NpmWorkspaceCheck` not defined.

- [ ] **Step 3: Implement NpmWorkspaceCheck**

Create `cli/SimpleModule.Cli/Commands/Doctor/Checks/NpmWorkspaceCheck.cs`:

```csharp
using System.Text.RegularExpressions;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Commands.Doctor.Checks;

public sealed partial class NpmWorkspaceCheck : IDoctorCheck
{
    [GeneratedRegex(@"""workspaces""\s*:\s*\[([^\]]*)\]", RegexOptions.Singleline)]
    private static partial Regex WorkspacesPattern();

    public IEnumerable<CheckResult> Run(SolutionContext solution)
    {
        var rootPackageJson = Path.Combine(solution.RootPath, "package.json");

        if (!File.Exists(rootPackageJson))
        {
            foreach (var module in solution.ExistingModules)
            {
                yield return new CheckResult(
                    $"NpmWorkspace -> {module}",
                    CheckStatus.Warning,
                    "root package.json not found — cannot verify workspaces"
                );
            }
            yield break;
        }

        var content = File.ReadAllText(rootPackageJson);
        var match = WorkspacesPattern().Match(content);
        var workspaceGlobs = match.Success
            ? match.Groups[1].Value
                .Split(',')
                .Select(g => g.Trim().Trim('"'))
                .Where(g => !string.IsNullOrWhiteSpace(g))
                .ToList()
            : [];

        foreach (var module in solution.ExistingModules)
        {
            // modules live at src/modules/{Name}/src/{Name} — check if any glob covers this pattern
            var modulePath = $"src/modules/{module}/src/{module}";
            var isCovered = workspaceGlobs.Any(glob => GlobCoversPath(glob, modulePath));

            yield return isCovered
                ? new CheckResult(
                    $"NpmWorkspace -> {module}",
                    CheckStatus.Pass,
                    "covered by workspace glob"
                )
                : new CheckResult(
                    $"NpmWorkspace -> {module}",
                    CheckStatus.Fail,
                    $"'{modulePath}' not covered by any workspace glob in root package.json"
                );
        }
    }

    private static bool GlobCoversPath(string glob, string path)
    {
        // Convert glob wildcards to a simple prefix match
        // "src/modules/*/src/*" covers "src/modules/Products/src/Products"
        var pattern = "^" + Regex.Escape(glob).Replace(@"\*", "[^/]+") + "$";
        return Regex.IsMatch(path, pattern, RegexOptions.IgnoreCase);
    }
}
```

- [ ] **Step 4: Implement NpmWorkspaceFixer**

Create `cli/SimpleModule.Cli/Infrastructure/NpmWorkspaceFixer.cs`:

```csharp
namespace SimpleModule.Cli.Infrastructure;

public static class NpmWorkspaceFixer
{
    public static void AddWorkspaceGlob(string packageJsonPath, string glob)
    {
        var content = File.ReadAllText(packageJsonPath);

        // Find existing workspaces array and append
        var workspacesStart = content.IndexOf("\"workspaces\"", StringComparison.Ordinal);
        if (workspacesStart >= 0)
        {
            var arrayStart = content.IndexOf('[', workspacesStart);
            var arrayEnd = content.IndexOf(']', arrayStart);
            if (arrayStart >= 0 && arrayEnd >= 0)
            {
                var entry = $"\"{glob}\"";
                var existing = content[arrayStart..arrayEnd].Trim('[').Trim();
                var separator = existing.Length > 0 ? ", " : "";
                var newContent = content[..(arrayEnd)] + separator + entry + content[arrayEnd..];
                File.WriteAllText(packageJsonPath, newContent);
                return;
            }
        }

        // No workspaces key — insert before closing }
        var lastBrace = content.LastIndexOf('}');
        if (lastBrace >= 0)
        {
            var insert = $",\n  \"workspaces\": [\"{glob}\"]";
            File.WriteAllText(packageJsonPath, content[..lastBrace] + insert + "\n}");
        }
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```
dotnet test tests/SimpleModule.Cli.Tests/ --filter "NpmWorkspaceCheckTests"
```

Expected: PASS.

- [ ] **Step 6: Commit**

```
git add cli/SimpleModule.Cli/Commands/Doctor/Checks/NpmWorkspaceCheck.cs cli/SimpleModule.Cli/Infrastructure/NpmWorkspaceFixer.cs tests/SimpleModule.Cli.Tests/NpmWorkspaceCheckTests.cs
git commit -m "feat(cli): add NpmWorkspaceCheck and NpmWorkspaceFixer"
```

---

## Task 9: Register all new checks and fixers in DoctorCommand

**Files:**
- Modify: `cli/SimpleModule.Cli/Commands/Doctor/DoctorCommand.cs`

- [ ] **Step 1: Update checks array**

In `cli/SimpleModule.Cli/Commands/Doctor/DoctorCommand.cs`, replace the existing `checks` array:

```csharp
IDoctorCheck[] checks =
[
    new SolutionStructureCheck(),
    new ProjectReferenceCheck(),
    new SlnxEntriesCheck(),
    new CsprojConventionCheck(),
    new ContractsIsolationCheck(),
    new ModulePatternCheck(),
    new ModuleAttributeCheck(),
    new ViewEndpointNamingCheck(),
    new PagesRegistryCheck(),
    new ViteConfigCheck(),
    new PackageJsonCheck(),
    new NpmWorkspaceCheck(),
];
```

- [ ] **Step 2: Add new fix dispatchers to AutoFix method**

In the `AutoFix` method, add after the existing fix blocks:

```csharp
// Fix missing Pages/index.ts entries
if (result.Name.StartsWith("Pages -> ", StringComparison.Ordinal))
{
    var componentKey = result.Name["Pages -> ".Length..];
    // Derive module name from the component key (e.g. "Products/Browse" → "Products")
    var moduleName = componentKey.Contains('/') ? componentKey[..componentKey.IndexOf('/')] : componentKey;
    var featureName = componentKey.Contains('/') ? componentKey[(componentKey.IndexOf('/') + 1)..] : componentKey;
    var indexPath = solution.GetModulePagesIndexPath(moduleName);
    var importPath = $"../Views/{featureName}";
    PagesRegistryFixer.AddEntry(indexPath, componentKey, importPath);
    AnsiConsole.MarkupLine($"[green]  Fixed: added '{componentKey}' to Pages/index.ts[/]");
}

// Fix missing npm workspace entries
if (result.Name.StartsWith("NpmWorkspace -> ", StringComparison.Ordinal))
{
    var moduleName = result.Name["NpmWorkspace -> ".Length..];
    var rootPackageJson = Path.Combine(solution.RootPath, "package.json");
    if (File.Exists(rootPackageJson))
    {
        NpmWorkspaceFixer.AddWorkspaceGlob(rootPackageJson, $"src/modules/{moduleName}/src/*");
        AnsiConsole.MarkupLine($"[green]  Fixed: added {moduleName} workspace glob to package.json[/]");
    }
}
```

- [ ] **Step 3: Update --fix hint message**

Replace the existing hint line:
```csharp
AnsiConsole.MarkupLine(
    "[dim]Run with --fix to auto-fix missing slnx entries, project references, Pages registry entries, and npm workspace globs.[/]"
);
```

- [ ] **Step 4: Build to verify**

```
dotnet build cli/SimpleModule.Cli/
```

Expected: Build succeeds with no errors.

- [ ] **Step 5: Run all CLI tests**

```
dotnet test tests/SimpleModule.Cli.Tests/
```

Expected: All tests PASS.

- [ ] **Step 6: Commit**

```
git add cli/SimpleModule.Cli/Commands/Doctor/DoctorCommand.cs
git commit -m "feat(cli): register all new doctor checks and fix dispatchers"
```

---

## Task 10: FeatureTemplates.ViewComponent() + Pages/index.ts updater

**Files:**
- Modify: `cli/SimpleModule.Cli/Templates/FeatureTemplates.cs`
- Test: `tests/SimpleModule.Cli.Tests/NewFeatureViewScaffoldingTests.cs` (partial — template method tests)

- [ ] **Step 1: Write failing tests for ViewComponent**

Create `tests/SimpleModule.Cli.Tests/NewFeatureViewScaffoldingTests.cs`:

```csharp
using FluentAssertions;
using SimpleModule.Cli.Infrastructure;
using SimpleModule.Cli.Templates;

namespace SimpleModule.Cli.Tests;

public sealed class NewFeatureViewScaffoldingTests : IDisposable
{
    private readonly string _tempDir;

    public NewFeatureViewScaffoldingTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"sm-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true);
    }

    private SolutionContext CreateSolution()
    {
        File.WriteAllText(Path.Combine(_tempDir, "Test.slnx"), "<Solution />");
        return SolutionContext.Discover(_tempDir)!;
    }

    [Fact]
    public void ViewComponent_ContainsFeatureNameAsComponentName()
    {
        var solution = CreateSolution();
        var templates = new FeatureTemplates(solution);

        var result = templates.ViewComponent("Products", "Create");

        result.Should().Contain("export default function Create");
    }

    [Fact]
    public void ViewComponent_ContainsPropsType()
    {
        var solution = CreateSolution();
        var templates = new FeatureTemplates(solution);

        var result = templates.ViewComponent("Products", "Browse");

        result.Should().Contain("type Props =");
    }

    [Fact]
    public void ViewComponent_ContainsHeadingWithFeatureName()
    {
        var solution = CreateSolution();
        var templates = new FeatureTemplates(solution);

        var result = templates.ViewComponent("Products", "Create");

        result.Should().Contain("<h1>Create</h1>");
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```
dotnet test tests/SimpleModule.Cli.Tests/ --filter "NewFeatureViewScaffoldingTests"
```

Expected: FAIL — `ViewComponent` method not defined.

- [ ] **Step 3: Add ViewComponent to FeatureTemplates**

In `cli/SimpleModule.Cli/Templates/FeatureTemplates.cs`, add this public method:

```csharp
public static string ViewComponent(string moduleName, string featureName) =>
    $$"""
    type Props = {
        // TODO: add props from your endpoint's response
    }

    export default function {{featureName}}({ }: Props) {
        return (
            <div>
                <h1>{{featureName}}</h1>
            </div>
        )
    }
    """;
```

- [ ] **Step 4: Run tests to verify they pass**

```
dotnet test tests/SimpleModule.Cli.Tests/ --filter "NewFeatureViewScaffoldingTests"
```

Expected: PASS.

- [ ] **Step 5: Commit**

```
git add cli/SimpleModule.Cli/Templates/FeatureTemplates.cs tests/SimpleModule.Cli.Tests/NewFeatureViewScaffoldingTests.cs
git commit -m "feat(cli): add ViewComponent template method to FeatureTemplates"
```

---

## Task 11: NewFeatureSettings flags + NewFeatureCommand view scaffolding

**Files:**
- Modify: `cli/SimpleModule.Cli/Commands/New/NewFeatureSettings.cs`
- Modify: `cli/SimpleModule.Cli/Commands/New/NewFeatureCommand.cs`
- Test: `tests/SimpleModule.Cli.Tests/NewFeatureViewScaffoldingTests.cs` (extend)

- [ ] **Step 1: Write failing tests for view scaffolding integration**

Add to `tests/SimpleModule.Cli.Tests/NewFeatureViewScaffoldingTests.cs`:

```csharp
[Fact]
public void PagesRegistryFixer_AddsEntryForNewFeature()
{
    var pagesDir = Path.Combine(_tempDir, "Pages");
    Directory.CreateDirectory(pagesDir);
    var indexPath = Path.Combine(pagesDir, "index.ts");
    File.WriteAllText(indexPath, """
        export const pages: Record<string, any> = {
            "Products/Browse": () => import("../Views/Browse"),
        };
        """);

    PagesRegistryFixer.AddEntry(indexPath, "Products/Create", "../Views/Create");

    var content = File.ReadAllText(indexPath);
    content.Should().Contain("\"Products/Create\": () => import(\"../Views/Create\")");
    content.Should().Contain("\"Products/Browse\"");
}
```

- [ ] **Step 2: Run test to confirm it passes** (PagesRegistryFixer already implemented)

```
dotnet test tests/SimpleModule.Cli.Tests/ --filter "PagesRegistryFixer_AddsEntryForNewFeature"
```

Expected: PASS.

- [ ] **Step 3: Add --dry-run and --no-view flags to NewFeatureSettings**

In `cli/SimpleModule.Cli/Commands/New/NewFeatureSettings.cs`, add:

```csharp
[CommandOption("--no-view")]
[Description("Skip creating the React view component and Pages/index.ts entry")]
public bool NoView { get; set; }

[CommandOption("--dry-run")]
[Description("Show what would be created without writing any files")]
public bool DryRun { get; set; }
```

- [ ] **Step 4: Rewrite NewFeatureCommand.Execute to add view scaffolding and dry-run**

Replace the body of `Execute` in `cli/SimpleModule.Cli/Commands/New/NewFeatureCommand.cs`:

```csharp
public override int Execute(CommandContext context, NewFeatureSettings settings)
{
    var solution = SolutionContext.Discover();
    if (solution is null)
    {
        AnsiConsole.MarkupLine("[red]No .slnx file found. Run this command from inside a SimpleModule project.[/]");
        return 1;
    }

    if (solution.ExistingModules.Count == 0)
    {
        AnsiConsole.MarkupLine("[red]No modules found. Create a module first with 'sm new module'.[/]");
        return 1;
    }

    var moduleName = settings.ResolveModule(solution);
    var featureName = settings.ResolveName();
    var httpMethod = settings.ResolveHttpMethod();
    var route = settings.ResolveRoute();
    var includeValidator = settings.ResolveIncludeValidator();
    var singularName = ModuleTemplates.GetSingularName(moduleName);

    var templates = new FeatureTemplates(solution);
    var ops = new List<(string Path, FileAction Action)>();

    var endpointsDir = Path.Combine(solution.GetModuleProjectPath(moduleName), "Endpoints", moduleName);

    // Collect operations
    ops.Add((Path.Combine(endpointsDir, $"{featureName}Endpoint.cs"), FileAction.Create));
    if (includeValidator)
        ops.Add((Path.Combine(endpointsDir, $"{featureName}RequestValidator.cs"), FileAction.Create));

    if (!settings.NoView)
    {
        ops.Add((Path.Combine(solution.GetModuleViewsPath(moduleName), $"{featureName}.tsx"), FileAction.Create));
        ops.Add((solution.GetModulePagesIndexPath(moduleName), FileAction.Modify));
    }

    if (settings.DryRun)
    {
        RenderDryRunTree(ops);
        return 0;
    }

    // Execute
    Directory.CreateDirectory(endpointsDir);
    WriteFile(Path.Combine(endpointsDir, $"{featureName}Endpoint.cs"),
        templates.Endpoint(moduleName, featureName, httpMethod, route, singularName));

    if (includeValidator)
        WriteFile(Path.Combine(endpointsDir, $"{featureName}RequestValidator.cs"),
            templates.Validator(moduleName, featureName, singularName));

    if (!settings.NoView)
    {
        var viewsDir = solution.GetModuleViewsPath(moduleName);
        Directory.CreateDirectory(viewsDir);
        WriteFile(Path.Combine(viewsDir, $"{featureName}.tsx"),
            FeatureTemplates.ViewComponent(moduleName, featureName));

        var indexPath = solution.GetModulePagesIndexPath(moduleName);
        PagesRegistryFixer.AddEntry(indexPath, $"{moduleName}/{featureName}", $"../Views/{featureName}");
        AnsiConsole.MarkupLine($"[green]  ~ Pages/index.ts[/]");
    }

    AnsiConsole.MarkupLine($"\n[green]Feature '{featureName}' added to '{moduleName}'.[/]");
    return 0;
}

private static void WriteFile(string path, string content)
{
    File.WriteAllText(path, content);
    AnsiConsole.MarkupLine($"[green]  + {Path.GetRelativePath(Directory.GetCurrentDirectory(), path)}[/]");
}

private static void RenderDryRunTree(List<(string Path, FileAction Action)> ops)
{
    AnsiConsole.MarkupLine("[dim]Dry run — no files written[/]\n");
    var tree = new Tree("[dim]Would create/modify:[/]");
    foreach (var (path, action) in ops)
    {
        var label = action == FileAction.Modify
            ? $"[yellow]{Markup.Escape(Path.GetFileName(path))}[/] [dim](modify)[/]"
            : $"[green]{Markup.Escape(Path.GetFileName(path))}[/] [dim](create)[/]";
        tree.AddNode(label);
    }
    AnsiConsole.Write(tree);
}
```

- [ ] **Step 5: Add missing using statements to NewFeatureCommand.cs**

Ensure top of file has:

```csharp
using SimpleModule.Cli.Infrastructure;
using SimpleModule.Cli.Templates;
using Spectre.Console;
using Spectre.Console.Cli;
```

- [ ] **Step 6: Build to verify**

```
dotnet build cli/SimpleModule.Cli/
```

Expected: Build succeeds.

- [ ] **Step 7: Run all CLI tests**

```
dotnet test tests/SimpleModule.Cli.Tests/
```

Expected: All PASS.

- [ ] **Step 8: Commit**

```
git add cli/SimpleModule.Cli/Commands/New/NewFeatureSettings.cs cli/SimpleModule.Cli/Commands/New/NewFeatureCommand.cs tests/SimpleModule.Cli.Tests/NewFeatureViewScaffoldingTests.cs
git commit -m "feat(cli): scaffold React view + Pages/index.ts in sm new feature"
```

---

## Task 12: Dry-run + tree output for NewModuleCommand

**Files:**
- Modify: `cli/SimpleModule.Cli/Commands/New/NewModuleSettings.cs`
- Modify: `cli/SimpleModule.Cli/Commands/New/NewModuleCommand.cs`

- [ ] **Step 1: Add --dry-run to NewModuleSettings**

In `cli/SimpleModule.Cli/Commands/New/NewModuleSettings.cs`, add:

```csharp
[CommandOption("--dry-run")]
[Description("Show what would be created without writing any files")]
public bool DryRun { get; set; }
```

- [ ] **Step 2: Add spinner, tree output, and dry-run to NewModuleCommand**

Replace the `Execute` method body in `cli/SimpleModule.Cli/Commands/New/NewModuleCommand.cs`:

```csharp
public override int Execute(CommandContext context, NewModuleSettings settings)
{
    var moduleName = settings.ResolveName();
    var singularName = ModuleTemplates.GetSingularName(moduleName);

    var solution = SolutionContext.Discover();
    if (solution is null)
    {
        AnsiConsole.MarkupLine("[red]No .slnx file found. Run this command from inside a SimpleModule project.[/]");
        return 1;
    }

    if (solution.ExistingModules.Contains(moduleName, StringComparer.OrdinalIgnoreCase))
    {
        AnsiConsole.MarkupLine($"[red]Module '{moduleName}' already exists. Available modules: {string.Join(", ", solution.ExistingModules)}[/]");
        return 1;
    }

    var templates = new ModuleTemplates(solution);
    var ops = new List<(string Path, FileAction Action)>();

    var contractsDir = solution.GetModuleContractsPath(moduleName);
    var moduleDir = solution.GetModuleProjectPath(moduleName);
    var eventsDir = Path.Combine(contractsDir, "Events");
    var endpointsDir = Path.Combine(moduleDir, "Endpoints", moduleName);
    var testDir = solution.GetTestProjectPath(moduleName);

    // Collect all ops
    void Plan(string path) => ops.Add((path, FileAction.Create));
    Plan(Path.Combine(contractsDir, $"{moduleName}.Contracts.csproj"));
    Plan(Path.Combine(contractsDir, $"I{singularName}Contracts.cs"));
    Plan(Path.Combine(contractsDir, $"{singularName}.cs"));
    Plan(Path.Combine(eventsDir, $"{singularName}CreatedEvent.cs"));
    Plan(Path.Combine(moduleDir, $"{moduleName}.csproj"));
    Plan(Path.Combine(moduleDir, $"{moduleName}Module.cs"));
    Plan(Path.Combine(moduleDir, $"{moduleName}Constants.cs"));
    Plan(Path.Combine(moduleDir, $"{moduleName}DbContext.cs"));
    Plan(Path.Combine(moduleDir, $"{singularName}Service.cs"));
    Plan(Path.Combine(endpointsDir, "GetAllEndpoint.cs"));
    Plan(Path.Combine(testDir, $"{moduleName}.Tests.csproj"));
    Plan(Path.Combine(testDir, "GlobalUsings.cs"));
    Plan(Path.Combine(testDir, "Unit", $"{singularName}ServiceTests.cs"));
    Plan(Path.Combine(testDir, "Integration", $"{moduleName}EndpointTests.cs"));
    ops.Add((solution.SlnxPath, FileAction.Modify));
    ops.Add((solution.ApiCsprojPath, FileAction.Modify));

    if (settings.DryRun)
    {
        RenderDryRunTree(moduleName, ops);
        return 0;
    }

    AnsiConsole.Status()
        .Spinner(Spinner.Known.Dots)
        .Start($"Creating module '{moduleName}'...", ctx =>
        {
            Directory.CreateDirectory(eventsDir);
            Directory.CreateDirectory(endpointsDir);
            Directory.CreateDirectory(Path.Combine(testDir, "Unit"));
            Directory.CreateDirectory(Path.Combine(testDir, "Integration"));

            File.WriteAllText(Path.Combine(contractsDir, $"{moduleName}.Contracts.csproj"), templates.ContractsCsproj(moduleName));
            File.WriteAllText(Path.Combine(contractsDir, $"I{singularName}Contracts.cs"), templates.ContractsInterface(moduleName, singularName));
            File.WriteAllText(Path.Combine(contractsDir, $"{singularName}.cs"), templates.DtoClass(moduleName, singularName));
            File.WriteAllText(Path.Combine(eventsDir, $"{singularName}CreatedEvent.cs"), templates.EventClass(moduleName, singularName));

            File.WriteAllText(Path.Combine(moduleDir, $"{moduleName}.csproj"), templates.ModuleCsproj(moduleName));
            File.WriteAllText(Path.Combine(moduleDir, $"{moduleName}Module.cs"), templates.ModuleClass(moduleName, singularName));
            File.WriteAllText(Path.Combine(moduleDir, $"{moduleName}Constants.cs"), templates.ConstantsClass(moduleName, singularName));
            File.WriteAllText(Path.Combine(moduleDir, $"{moduleName}DbContext.cs"), templates.DbContextClass(moduleName, singularName));
            File.WriteAllText(Path.Combine(moduleDir, $"{singularName}Service.cs"), templates.ServiceClass(moduleName, singularName));
            File.WriteAllText(Path.Combine(endpointsDir, "GetAllEndpoint.cs"), templates.GetAllEndpoint(moduleName, singularName));

            File.WriteAllText(Path.Combine(testDir, $"{moduleName}.Tests.csproj"), templates.TestCsproj(moduleName));
            File.WriteAllText(Path.Combine(testDir, "GlobalUsings.cs"), templates.GlobalUsings());
            File.WriteAllText(Path.Combine(testDir, "Unit", $"{singularName}ServiceTests.cs"), templates.UnitTestSkeleton(moduleName, singularName));
            File.WriteAllText(Path.Combine(testDir, "Integration", $"{moduleName}EndpointTests.cs"), templates.IntegrationTestSkeleton(moduleName, singularName));

            ctx.Status("Updating solution files...");
            SlnxManipulator.AddModuleEntries(solution.SlnxPath, moduleName);
            ProjectManipulator.AddProjectReference(
                solution.ApiCsprojPath,
                $@"..\modules\{moduleName}\src\{moduleName}\{moduleName}.csproj"
            );
        });

    RenderCreatedTree(moduleName, ops);

    AnsiConsole.MarkupLine($"\n[green]Module '{moduleName}' created![/]");
    AnsiConsole.MarkupLine("[dim]Next steps:[/]");
    AnsiConsole.MarkupLine($"[dim]  sm new feature <FeatureName> --module {moduleName}[/]");
    AnsiConsole.MarkupLine("[dim]  dotnet build[/]");
    return 0;
}

private static void RenderDryRunTree(string moduleName, List<(string Path, FileAction Action)> ops)
{
    AnsiConsole.MarkupLine("[dim]Dry run — no files written[/]\n");
    var tree = new Tree($"[blue]{moduleName}[/]");
    foreach (var (path, action) in ops)
    {
        var label = action == FileAction.Modify
            ? $"[yellow]{Markup.Escape(Path.GetFileName(path))}[/] [dim](modify)[/]"
            : $"[green]{Markup.Escape(Path.GetFileName(path))}[/] [dim](create)[/]";
        tree.AddNode(label);
    }
    AnsiConsole.Write(tree);
}

private static void RenderCreatedTree(string moduleName, List<(string Path, FileAction Action)> ops)
{
    AnsiConsole.MarkupLine("");
    var tree = new Tree($"[blue]{moduleName}[/]");
    foreach (var (path, action) in ops)
    {
        var label = action == FileAction.Modify
            ? $"[yellow]{Markup.Escape(Path.GetFileName(path))}[/] [dim](modified)[/]"
            : $"[green]{Markup.Escape(Path.GetFileName(path))}[/]";
        tree.AddNode(label);
    }
    AnsiConsole.Write(tree);
}
```

- [ ] **Step 3: Build to verify**

```
dotnet build cli/SimpleModule.Cli/
```

Expected: Build succeeds.

- [ ] **Step 4: Run all CLI tests**

```
dotnet test tests/SimpleModule.Cli.Tests/
```

Expected: All PASS.

- [ ] **Step 5: Commit**

```
git add cli/SimpleModule.Cli/Commands/New/NewModuleSettings.cs cli/SimpleModule.Cli/Commands/New/NewModuleCommand.cs
git commit -m "feat(cli): add spinner, tree output, and dry-run to sm new module"
```

---

## Task 13: Dry-run + tree output for NewProjectCommand

**Files:**
- Modify: `cli/SimpleModule.Cli/Commands/New/NewProjectSettings.cs`
- Modify: `cli/SimpleModule.Cli/Commands/New/NewProjectCommand.cs`

- [ ] **Step 1: Add --dry-run to NewProjectSettings**

In `cli/SimpleModule.Cli/Commands/New/NewProjectSettings.cs`, add:

```csharp
[CommandOption("--dry-run")]
[Description("Show what would be created without writing any files")]
public bool DryRun { get; set; }
```

- [ ] **Step 2: Add dry-run + tree output to NewProjectCommand**

In `cli/SimpleModule.Cli/Commands/New/NewProjectCommand.cs`, replace `Execute`:

```csharp
public override int Execute(CommandContext context, NewProjectSettings settings)
{
    var projectName = settings.ResolveName();
    var outputDir = settings.ResolveOutputDir();
    var rootDir = Path.Combine(outputDir, projectName);

    if (!settings.DryRun && Directory.Exists(rootDir) && Directory.GetFileSystemEntries(rootDir).Length > 0)
    {
        AnsiConsole.MarkupLine($"[red]Directory '{rootDir}' already exists and is not empty.[/]");
        return 1;
    }

    var solution = SolutionContext.Discover();
    var templates = new ProjectTemplates(solution);

    var srcDir = Path.Combine(rootDir, "src");
    var apiDir = Path.Combine(srcDir, $"{projectName}.Api");
    var coreDir = Path.Combine(srcDir, $"{projectName}.Core");
    var databaseDir = Path.Combine(srcDir, $"{projectName}.Database");
    var generatorDir = Path.Combine(srcDir, $"{projectName}.Generator");
    var testsSharedDir = Path.Combine(rootDir, "tests", $"{projectName}.Tests.Shared");

    var ops = new List<(string Path, FileAction Action)>
    {
        (Path.Combine(rootDir, $"{projectName}.slnx"), FileAction.Create),
        (Path.Combine(rootDir, "Directory.Build.props"), FileAction.Create),
        (Path.Combine(rootDir, "Directory.Packages.props"), FileAction.Create),
        (Path.Combine(rootDir, "global.json"), FileAction.Create),
        (Path.Combine(apiDir, $"{projectName}.Api.csproj"), FileAction.Create),
        (Path.Combine(apiDir, "Program.cs"), FileAction.Create),
        (Path.Combine(coreDir, $"{projectName}.Core.csproj"), FileAction.Create),
        (Path.Combine(databaseDir, $"{projectName}.Database.csproj"), FileAction.Create),
        (Path.Combine(generatorDir, $"{projectName}.Generator.csproj"), FileAction.Create),
        (Path.Combine(testsSharedDir, $"{projectName}.Tests.Shared.csproj"), FileAction.Create),
    };

    if (settings.DryRun)
    {
        AnsiConsole.MarkupLine("[dim]Dry run — no files written[/]\n");
        var tree = new Tree($"[blue]{projectName}/[/]");
        foreach (var (path, _) in ops)
            tree.AddNode($"[green]{Markup.Escape(Path.GetFileName(path))}[/]");
        AnsiConsole.Write(tree);
        return 0;
    }

    AnsiConsole.MarkupLine($"[blue]Creating project '{projectName}'...[/]");

    Directory.CreateDirectory(apiDir);
    Directory.CreateDirectory(coreDir);
    Directory.CreateDirectory(databaseDir);
    Directory.CreateDirectory(generatorDir);
    Directory.CreateDirectory(Path.Combine(rootDir, "src", "modules"));
    Directory.CreateDirectory(testsSharedDir);
    Directory.CreateDirectory(Path.Combine(rootDir, "tests", "modules"));

    WriteFile(Path.Combine(rootDir, $"{projectName}.slnx"), templates.Slnx(projectName));
    WriteFile(Path.Combine(rootDir, "Directory.Build.props"), templates.DirectoryBuildProps());
    WriteFile(Path.Combine(rootDir, "Directory.Packages.props"), templates.DirectoryPackagesProps());
    WriteFile(Path.Combine(rootDir, "global.json"), templates.GlobalJson());
    WriteFile(Path.Combine(apiDir, $"{projectName}.Api.csproj"), templates.ApiCsproj(projectName));
    WriteFile(Path.Combine(apiDir, "Program.cs"), ProjectTemplates.ApiProgram());
    WriteFile(Path.Combine(coreDir, $"{projectName}.Core.csproj"), templates.CoreCsproj(projectName));
    WriteFile(Path.Combine(databaseDir, $"{projectName}.Database.csproj"), templates.DatabaseCsproj(projectName));
    WriteFile(Path.Combine(generatorDir, $"{projectName}.Generator.csproj"), templates.GeneratorCsproj());
    WriteFile(Path.Combine(testsSharedDir, $"{projectName}.Tests.Shared.csproj"), templates.TestsSharedCsproj(projectName));

    AnsiConsole.MarkupLine($"\n[green]Project '{projectName}' created![/]");
    AnsiConsole.MarkupLine("[dim]Next steps:[/]");
    AnsiConsole.MarkupLine($"[dim]  cd {projectName}[/]");
    AnsiConsole.MarkupLine("[dim]  sm new module <ModuleName>[/]");
    AnsiConsole.MarkupLine("[dim]  dotnet build[/]");
    return 0;
}

private static void WriteFile(string path, string content)
{
    File.WriteAllText(path, content);
    AnsiConsole.MarkupLine($"[green]  + {Path.GetFileName(path)}[/]");
}
```

- [ ] **Step 3: Build to verify**

```
dotnet build cli/SimpleModule.Cli/
```

Expected: Build succeeds.

- [ ] **Step 4: Run all CLI tests**

```
dotnet test tests/SimpleModule.Cli.Tests/
```

Expected: All PASS.

- [ ] **Step 5: Commit**

```
git add cli/SimpleModule.Cli/Commands/New/NewProjectSettings.cs cli/SimpleModule.Cli/Commands/New/NewProjectCommand.cs
git commit -m "feat(cli): add dry-run and tree output to sm new project"
```

---

## Task 14: Final build + full test run

- [ ] **Step 1: Full build**

```
dotnet build
```

Expected: Build succeeds with 0 errors.

- [ ] **Step 2: Full test suite**

```
dotnet test
```

Expected: All tests PASS.

- [ ] **Step 3: Commit (if any fixups needed)**

```
git add -u
git commit -m "fix(cli): address build warnings from full solution build"
```

---

## Self-Review

**Spec coverage check:**

| Spec requirement | Task |
|---|---|
| PagesRegistryCheck (FAIL, auto-fix) | Task 5, 9 |
| ViteConfigCheck (WARN) | Task 6, 9 |
| PackageJsonCheck (WARN) | Task 7, 9 |
| NpmWorkspaceCheck (FAIL, auto-fix) | Task 8, 9 |
| ModuleAttributeCheck (FAIL) | Task 3, 9 |
| ViewEndpointNamingCheck (WARN) | Task 4, 9 |
| ContractsIsolationCheck (FAIL) | Task 2, 9 |
| Views/FeatureName.tsx generated by sm new feature | Task 10, 11 |
| Pages/index.ts updated by sm new feature | Task 5, 11 |
| --no-view flag | Task 11 |
| Dry-run on sm new feature | Task 11 |
| Dry-run on sm new module | Task 12 |
| Dry-run on sm new project | Task 13 |
| Spinner on sm new module | Task 12 |
| Tree output on sm new feature | Task 11 |
| Tree output on sm new module | Task 12 |
| Tree output on sm new project | Task 13 |
| FileAction enum | Task 1 |
| SolutionContext GetModuleViewsPath/GetModulePagesIndexPath | Task 1 |

All requirements covered. No placeholders. Method names are consistent across tasks.
