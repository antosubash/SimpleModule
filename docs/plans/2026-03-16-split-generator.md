# Split ModuleDiscovererGenerator Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Split the 1080-line `ModuleDiscovererGenerator.cs` into 3 partial-class files for maintainability.

**Architecture:** Use C# `partial class` to split by concern — orchestration, code emitters, and data models. All files stay in the same namespace and project. Pure refactoring, no behavior changes.

**Tech Stack:** C# / Roslyn source generators / netstandard2.0

---

### Task 1: Create ModuleDiscovererGenerator.Models.cs

**Files:**
- Create: `src/SimpleModule.Generator/ModuleDiscovererGenerator.Models.cs`
- Modify: `src/SimpleModule.Generator/ModuleDiscovererGenerator.cs` (remove moved code)

**Step 1: Create the Models file**

Create `src/SimpleModule.Generator/ModuleDiscovererGenerator.Models.cs` containing:
- The `using` statements needed: `System.Collections.Generic`, `System.Collections.Immutable`
- `partial class ModuleDiscovererGenerator` in namespace `SimpleModule.Generator`
- Move the entire `#region Equatable data model for incremental caching` section (lines 914–1033):
  - `DiscoveryData` record struct
  - `ModuleInfoRecord` record struct
  - `EndpointInfoRecord` record struct
  - `ViewInfoRecord` record struct
  - `DtoTypeInfoRecord` record struct
  - `DtoPropertyInfoRecord` record struct
- Move the entire `#region Mutable working types` section (lines 1037–1078):
  - `ModuleInfo` class
  - `EndpointInfo` class
  - `ViewInfo` class
  - `DtoTypeInfo` class
  - `DtoPropertyInfo` class

**Step 2: Remove moved code from original file**

In `ModuleDiscovererGenerator.cs`, delete everything from `#region Equatable data model` through the closing `#endregion` of Mutable working types (lines 914–1078).

**Step 3: Build to verify**

Run: `dotnet build src/SimpleModule.Generator/SimpleModule.Generator.csproj`
Expected: Build succeeds with no errors.

**Step 4: Run tests**

Run: `dotnet test tests/SimpleModule.Generator.Tests/`
Expected: All tests pass.

**Step 5: Commit**

```bash
git add src/SimpleModule.Generator/ModuleDiscovererGenerator.Models.cs src/SimpleModule.Generator/ModuleDiscovererGenerator.cs
git commit -m "refactor: extract data models from ModuleDiscovererGenerator into partial class"
```

---

### Task 2: Create ModuleDiscovererGenerator.Emitters.cs

**Files:**
- Create: `src/SimpleModule.Generator/ModuleDiscovererGenerator.Emitters.cs`
- Modify: `src/SimpleModule.Generator/ModuleDiscovererGenerator.cs` (remove moved code)

**Step 1: Create the Emitters file**

Create `src/SimpleModule.Generator/ModuleDiscovererGenerator.Emitters.cs` containing:
- The `using` statements needed: `System`, `System.Collections.Immutable`, `System.Linq`, `System.Text`, `Microsoft.CodeAnalysis`, `Microsoft.CodeAnalysis.Text`
- `partial class ModuleDiscovererGenerator` in namespace `SimpleModule.Generator`
- Move all `Generate*` methods:
  - `GenerateModuleExtensions` (lines 407–468)
  - `GenerateEndpointExtensions` (lines 470–569)
  - `GenerateMenuExtensions` (lines 571–605)
  - `GenerateJsonResolver` (lines 607–685)
  - `GenerateTypeScriptDefinitions` (lines 687–722)
  - `GenerateViewPages` (lines 724–776)
  - `GenerateRazorComponentExtensions` (lines 865–906)
- Move the utility methods used exclusively by emitters:
  - `MapCSharpTypeToTypeScript` (lines 778–830)
  - `GetModuleFieldName` (lines 908–912)

**Step 2: Remove moved code from original file**

In `ModuleDiscovererGenerator.cs`, delete all the `Generate*` methods, `MapCSharpTypeToTypeScript`, and `GetModuleFieldName`. The file should now contain only:
- `Initialize` method
- `ExtractDiscoveryData` method
- `FindModuleTypes`, `FindEndpointTypes`, `FindDtoTypes` methods
- `HasComponentBaseDescendant`, `InheritsFrom`, `ImplementsInterface`, `DeclaresMethod` helper methods

**Step 3: Build to verify**

Run: `dotnet build src/SimpleModule.Generator/SimpleModule.Generator.csproj`
Expected: Build succeeds with no errors.

**Step 4: Run tests**

Run: `dotnet test tests/SimpleModule.Generator.Tests/`
Expected: All tests pass.

**Step 5: Commit**

```bash
git add src/SimpleModule.Generator/ModuleDiscovererGenerator.Emitters.cs src/SimpleModule.Generator/ModuleDiscovererGenerator.cs
git commit -m "refactor: extract code emitters from ModuleDiscovererGenerator into partial class"
```

---

### Task 3: Final verification

**Step 1: Full solution build**

Run: `dotnet build`
Expected: Entire solution builds with no errors.

**Step 2: Full test suite**

Run: `dotnet test`
Expected: All tests pass.
