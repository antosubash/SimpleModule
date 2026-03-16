---
name: new-module
description: Scaffold a new SimpleModule module with csproj, module class, and service interface
---

# New Module Scaffolding

When the user asks to create a new module, follow these steps:

## Required Input
- **Module name** (e.g., "Payments")

## Steps

1. Create `src/modules/<Name>/` directory

2. Create `src/modules/<Name>/<Name>.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\SimpleModule.Core\SimpleModule.Core.csproj" />
  </ItemGroup>
</Project>
```

3. Create `src/modules/<Name>/<Name>Module.cs`:
```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;

namespace SimpleModule.<Name>;

[Module("<Name>")]
public class <Name>Module : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Register services for the <Name> module
    }

    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/<name-lowercase>");

        group.MapGet("/", () => Results.Ok());
    }
}
```

4. Add `<ProjectReference Include="..\modules\<Name>\<Name>.csproj" />` to `src/SimpleModule.Host/SimpleModule.Host.csproj`

5. Add the project to `SimpleModule.sln` using `dotnet sln add`

6. Run `dotnet build` to verify the source generator picks up the new module

## Important
- Do NOT add `PublishAot` or `EnableAotAnalyzer` to module class libraries
- Always include `<FrameworkReference Include="Microsoft.AspNetCore.App" />`
- Use `[Module("<Name>")]` attribute on the module class
- Use fully qualified ASP.NET Core usings (Builder, Http, Routing, DependencyInjection)
