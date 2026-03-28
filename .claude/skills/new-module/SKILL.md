---
name: new-module
description: Scaffold a new SimpleModule module with contracts, implementation, and test projects following the project's modular monolith conventions. Use when creating a new module from scratch.
---

# New Module Scaffolding

When the user asks to create a new module, follow these steps exactly.

## Required Input
- **Module name** (e.g., "Payments")

## Steps

### 1. Create Contracts Project

Create `modules/<Name>/src/SimpleModule.<Name>.Contracts/`:

**SimpleModule.<Name>.Contracts.csproj:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Library</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\..\..\framework\SimpleModule.Core\SimpleModule.Core.csproj" />
  </ItemGroup>
</Project>
```

**<Name>Constants.cs:**
```csharp
namespace SimpleModule.<Name>.Contracts;

public static class <Name>Constants
{
    public const string ModuleName = "<Name>";
    public const string RoutePrefix = "/api/<name-lowercase>";
}
```

**I<Name>Contracts.cs:**
```csharp
using System.Diagnostics.CodeAnalysis;

namespace SimpleModule.<Name>.Contracts;

[SuppressMessage("Design", "CA1040:Avoid empty interfaces", Justification = "Contracts placeholder")]
public interface I<Name>Contracts
{
}
```

### 2. Create Implementation Project

Create `modules/<Name>/src/SimpleModule.<Name>/`:

**SimpleModule.<Name>.csproj:**
```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <ProjectReference Include="..\..\..\..\framework\SimpleModule.Core\SimpleModule.Core.csproj" />
    <ProjectReference Include="..\SimpleModule.<Name>.Contracts\SimpleModule.<Name>.Contracts.csproj" />
  </ItemGroup>
</Project>
```

**<Name>Module.cs:**
```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;
using SimpleModule.Core.Menu;
using SimpleModule.<Name>.Contracts;

namespace SimpleModule.<Name>;

[Module(<Name>Constants.ModuleName, RoutePrefix = <Name>Constants.RoutePrefix, ViewPrefix = "/<name-lowercase>")]
public class <Name>Module : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<I<Name>Contracts, <Name>ContractsService>();
    }

    public void ConfigureMenu(IMenuBuilder menus)
    {
        menus.Add(
            new MenuItem
            {
                Label = "<Name>",
                Url = "/<name-lowercase>",
                Icon = """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M4 6h16M4 12h16M4 18h16"/></svg>""",
                Order = 50,
                Section = MenuSection.AppSidebar,
            }
        );
    }
}
```

**<Name>ContractsService.cs:**
```csharp
using SimpleModule.<Name>.Contracts;

namespace SimpleModule.<Name>;

public sealed class <Name>ContractsService : I<Name>Contracts
{
}
```

### 3. Register in Host

Add to `template/SimpleModule.Host/SimpleModule.Host.csproj`:
```xml
<ProjectReference Include="..\..\modules\<Name>\src\SimpleModule.<Name>\SimpleModule.<Name>.csproj" />
```

### 4. Add to Solution

Add to `SimpleModule.slnx` under the `/modules/` folder:
```xml
<Folder Name="/modules/<Name>/">
    <Project Path="modules/<Name>/src/SimpleModule.<Name>.Contracts/SimpleModule.<Name>.Contracts.csproj" />
    <Project Path="modules/<Name>/src/SimpleModule.<Name>/SimpleModule.<Name>.csproj" />
</Folder>
```

### 5. Verify

Run `dotnet build` to confirm the source generator discovers the new module.

## Important Constraints

- Contracts project uses `Microsoft.NET.Sdk` (plain library)
- Implementation project uses `Microsoft.NET.Sdk.Razor` (ASP.NET + Razor)
- Implementation MUST include `<FrameworkReference Include="Microsoft.AspNetCore.App" />`
- Module class MUST use `[Module]` attribute and implement `IModule`
- Constants go in the Contracts project (shared dependency)
- Use `ConfigureServices(IServiceCollection services, IConfiguration configuration)` signature (two params)
- Follow naming: `PascalCase` public, `_camelCase` private fields, file-scoped namespaces
