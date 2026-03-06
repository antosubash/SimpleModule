# Module Structure Improvements Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Improve SimpleModule across six areas: Core SDK fix, `IModule` default implementations, explicit `[Dto]` attribute, per-module contracts projects, feature-based internal layout, and generator accuracy.

**Architecture:** Core gets a `[Dto]` attribute and virtual default methods on `IModule`. Each module splits into an implementation project and a `.Contracts` project; sibling modules reference only `.Contracts`. The generator replaces heuristic DTO discovery with `[Dto]` attribute lookup and skips generating calls for methods a module doesn't override.

**Tech Stack:** .NET 10, C# 13, Roslyn incremental source generators (netstandard2.0), ASP.NET Core minimal APIs, AOT publishing.

---

## Task 1: Fix Core SDK and add `[Dto]` attribute and update `IModule`

Three small, related changes to `SimpleModule.Core` — all additive, nothing breaks.

**Files:**
- Modify: `src/SimpleModule.Core/SimpleModule.Core.csproj`
- Modify: `src/SimpleModule.Core/IModule.cs`
- Create: `src/SimpleModule.Core/DtoAttribute.cs`

**Step 1: Fix the csproj SDK**

Replace the entire content of `src/SimpleModule.Core/SimpleModule.Core.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <OutputType>Library</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>
</Project>
```

**Step 2: Add virtual default implementations to `IModule`**

Replace `src/SimpleModule.Core/IModule.cs`:
```csharp
namespace SimpleModule.Core;

public interface IModule
{
    virtual void ConfigureServices(IServiceCollection services) { }
    virtual void ConfigureEndpoints(IEndpointRouteBuilder endpoints) { }
}
```

**Step 3: Create `DtoAttribute.cs`**

Create `src/SimpleModule.Core/DtoAttribute.cs`:
```csharp
using System;

namespace SimpleModule.Core;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class DtoAttribute : Attribute { }
```

**Step 4: Build Core to verify**
```bash
dotnet build src/SimpleModule.Core/SimpleModule.Core.csproj
```
Expected: Build succeeded, 0 errors.

**Step 5: Commit**
```bash
git add src/SimpleModule.Core/
git commit -m "feat(core): fix SDK, add [Dto] attribute, add IModule default implementations"
```

---

## Task 2: Update the source generator

Replace heuristic DTO detection with `[Dto]` attribute lookup. Add override-checking so no-op calls aren't emitted for modules that rely on the default.

**Files:**
- Modify: `src/SimpleModule.Generator/ModuleDiscovererGenerator.cs`

**Step 1: Replace the generator**

Replace the entire content of `src/SimpleModule.Generator/ModuleDiscovererGenerator.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace SimpleModule.Generator;

[Generator]
public class ModuleDiscovererGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var compilationProvider = context.CompilationProvider;

        context.RegisterSourceOutput(
            compilationProvider,
            static (spc, compilation) =>
            {
                var moduleAttributeSymbol = compilation.GetTypeByMetadataName(
                    "SimpleModule.Core.ModuleAttribute"
                );
                var dtoAttributeSymbol = compilation.GetTypeByMetadataName(
                    "SimpleModule.Core.DtoAttribute"
                );
                if (moduleAttributeSymbol is null)
                    return;

                var modules = new List<ModuleInfo>();

                foreach (var reference in compilation.References)
                {
                    if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assemblySymbol)
                        continue;

                    FindModuleTypes(assemblySymbol.GlobalNamespace, moduleAttributeSymbol, modules);
                }

                FindModuleTypes(compilation.Assembly.GlobalNamespace, moduleAttributeSymbol, modules);

                if (modules.Count == 0)
                    return;

                var dtoTypes = new List<DtoTypeInfo>();
                if (dtoAttributeSymbol is not null)
                {
                    foreach (var reference in compilation.References)
                    {
                        if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assemblySymbol)
                            continue;

                        FindDtoTypes(assemblySymbol.GlobalNamespace, dtoAttributeSymbol, dtoTypes);
                    }

                    FindDtoTypes(compilation.Assembly.GlobalNamespace, dtoAttributeSymbol, dtoTypes);
                }

                GenerateModuleExtensions(spc, modules, dtoTypes.Count > 0);
                GenerateEndpointExtensions(spc, modules);

                if (dtoTypes.Count > 0)
                    GenerateJsonResolver(spc, dtoTypes);
            }
        );
    }

    private static void FindModuleTypes(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol moduleAttributeSymbol,
        List<ModuleInfo> modules
    )
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            if (member is INamespaceSymbol childNamespace)
            {
                FindModuleTypes(childNamespace, moduleAttributeSymbol, modules);
            }
            else if (member is INamedTypeSymbol typeSymbol)
            {
                foreach (var attr in typeSymbol.GetAttributes())
                {
                    if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, moduleAttributeSymbol))
                    {
                        modules.Add(new ModuleInfo
                        {
                            FullyQualifiedName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                            HasConfigureServices = DeclaresMethod(typeSymbol, "ConfigureServices"),
                            HasConfigureEndpoints = DeclaresMethod(typeSymbol, "ConfigureEndpoints"),
                        });
                        break;
                    }
                }
            }
        }
    }

    private static bool DeclaresMethod(INamedTypeSymbol typeSymbol, string methodName)
    {
        foreach (var member in typeSymbol.GetMembers(methodName))
        {
            if (member is IMethodSymbol)
                return true;
        }
        return false;
    }

    private static void FindDtoTypes(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol dtoAttributeSymbol,
        List<DtoTypeInfo> dtoTypes
    )
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            if (member is INamespaceSymbol childNamespace)
            {
                FindDtoTypes(childNamespace, dtoAttributeSymbol, dtoTypes);
            }
            else if (member is INamedTypeSymbol typeSymbol)
            {
                foreach (var attr in typeSymbol.GetAttributes())
                {
                    if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, dtoAttributeSymbol))
                    {
                        var fqn = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        var safeName = fqn.Replace("global::", "").Replace(".", "_");

                        var properties = new List<DtoPropertyInfo>();
                        foreach (var m in typeSymbol.GetMembers())
                        {
                            if (m is IPropertySymbol prop
                                && prop.DeclaredAccessibility == Accessibility.Public
                                && !prop.IsStatic
                                && !prop.IsIndexer
                                && prop.GetMethod is not null)
                            {
                                properties.Add(new DtoPropertyInfo
                                {
                                    Name = prop.Name,
                                    TypeFqn = prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                                    HasSetter = prop.SetMethod is not null
                                        && prop.SetMethod.DeclaredAccessibility == Accessibility.Public,
                                });
                            }
                        }

                        dtoTypes.Add(new DtoTypeInfo
                        {
                            FullyQualifiedName = fqn,
                            SafeName = safeName,
                            Properties = properties,
                        });
                        break;
                    }
                }
            }
        }
    }

    private static void GenerateModuleExtensions(
        SourceProductionContext context,
        List<ModuleInfo> modules,
        bool hasDtoTypes
    )
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#pragma warning disable IL2026");
        sb.AppendLine("#pragma warning disable IL3050");
        sb.AppendLine("using Microsoft.AspNetCore.Http.Json;");
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine();
        sb.AppendLine("namespace SimpleModule.Core;");
        sb.AppendLine();
        sb.AppendLine("public static class ModuleExtensions");
        sb.AppendLine("{");
        sb.AppendLine("    public static IServiceCollection AddModules(this IServiceCollection services)");
        sb.AppendLine("    {");

        foreach (var module in modules.Where(m => m.HasConfigureServices))
        {
            sb.AppendLine($"        new {module.FullyQualifiedName}().ConfigureServices(services);");
        }

        if (hasDtoTypes)
        {
            sb.AppendLine();
            sb.AppendLine("        services.ConfigureHttpJsonOptions(options =>");
            sb.AppendLine("        {");
            sb.AppendLine("            options.SerializerOptions.TypeInfoResolver = System.Text.Json.Serialization.Metadata.JsonTypeInfoResolver.Combine(");
            sb.AppendLine("                ModulesJsonResolver.Instance,");
            sb.AppendLine("                new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver());");
            sb.AppendLine("        });");
        }

        sb.AppendLine();
        sb.AppendLine("        return services;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource("ModuleExtensions.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static void GenerateEndpointExtensions(
        SourceProductionContext context,
        List<ModuleInfo> modules
    )
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("using Microsoft.AspNetCore.Builder;");
        sb.AppendLine();
        sb.AppendLine("namespace SimpleModule.Core;");
        sb.AppendLine();
        sb.AppendLine("public static class EndpointExtensions");
        sb.AppendLine("{");
        sb.AppendLine("    public static WebApplication MapModuleEndpoints(this WebApplication app)");
        sb.AppendLine("    {");

        foreach (var module in modules.Where(m => m.HasConfigureEndpoints))
        {
            sb.AppendLine($"        new {module.FullyQualifiedName}().ConfigureEndpoints(app);");
        }

        sb.AppendLine("        return app;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource("EndpointExtensions.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static void GenerateJsonResolver(
        SourceProductionContext context,
        List<DtoTypeInfo> dtoTypes
    )
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("#pragma warning disable IL2026");
        sb.AppendLine("#pragma warning disable IL3050");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Text.Json;");
        sb.AppendLine("using System.Text.Json.Serialization.Metadata;");
        sb.AppendLine();
        sb.AppendLine("namespace SimpleModule.Core;");
        sb.AppendLine();
        sb.AppendLine("public sealed class ModulesJsonResolver : IJsonTypeInfoResolver");
        sb.AppendLine("{");
        sb.AppendLine("    public static readonly ModulesJsonResolver Instance = new();");
        sb.AppendLine();
        sb.AppendLine("    public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)");
        sb.AppendLine("    {");

        foreach (var dto in dtoTypes)
        {
            sb.AppendLine($"        if (type == typeof({dto.FullyQualifiedName}))");
            sb.AppendLine($"            return Create_{dto.SafeName}(options);");
        }

        sb.AppendLine("        return null;");
        sb.AppendLine("    }");

        foreach (var dto in dtoTypes)
        {
            sb.AppendLine();
            sb.AppendLine($"    private static JsonTypeInfo Create_{dto.SafeName}(JsonSerializerOptions options)");
            sb.AppendLine("    {");
            sb.AppendLine($"        var info = JsonTypeInfo.CreateJsonTypeInfo<{dto.FullyQualifiedName}>(options);");
            sb.AppendLine($"        info.CreateObject = static () => new {dto.FullyQualifiedName}();");

            foreach (var prop in dto.Properties)
            {
                sb.AppendLine($"        var prop_{prop.Name} = info.CreateJsonPropertyInfo(typeof({prop.TypeFqn}), \"{prop.Name}\");");
                sb.AppendLine($"        prop_{prop.Name}.Get = static obj => (({dto.FullyQualifiedName})obj).{prop.Name};");

                if (prop.HasSetter)
                {
                    sb.AppendLine($"        prop_{prop.Name}.Set = static (obj, val) => (({dto.FullyQualifiedName})obj).{prop.Name} = ({prop.TypeFqn})val!;");
                }

                sb.AppendLine($"        info.Properties.Add(prop_{prop.Name});");
            }

            sb.AppendLine("        return info;");
            sb.AppendLine("    }");
        }

        sb.AppendLine("}");

        context.AddSource("ModulesJsonResolver.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private class ModuleInfo
    {
        public string FullyQualifiedName { get; set; } = "";
        public bool HasConfigureServices { get; set; }
        public bool HasConfigureEndpoints { get; set; }
    }

    private class DtoTypeInfo
    {
        public string FullyQualifiedName { get; set; } = "";
        public string SafeName { get; set; } = "";
        public List<DtoPropertyInfo> Properties { get; set; } = new();
    }

    private class DtoPropertyInfo
    {
        public string Name { get; set; } = "";
        public string TypeFqn { get; set; } = "";
        public bool HasSetter { get; set; }
    }
}
```

**Step 2: Build generator to verify**
```bash
dotnet build src/SimpleModule.Generator/SimpleModule.Generator.csproj
```
Expected: Build succeeded, 0 errors.

**Step 3: Commit**
```bash
git add src/SimpleModule.Generator/ModuleDiscovererGenerator.cs
git commit -m "feat(generator): replace heuristic DTO detection with [Dto] attribute; skip no-op module calls"
```

---

## Task 3: Create `Users.Contracts` and refactor `Users` module

Create the new folder structure under `src/modules/Users/`. The old flat `src/modules/Users/` directory becomes `src/modules/Users/Users/` (implementation) plus `src/modules/Users/Users.Contracts/` (contracts).

**Files to create:**
- `src/modules/Users/Users.Contracts/Users.Contracts.csproj`
- `src/modules/Users/Users.Contracts/IUserContracts.cs`
- `src/modules/Users/Users.Contracts/User.cs`
- `src/modules/Users/Users/Users.csproj`
- `src/modules/Users/Users/UsersModule.cs`
- `src/modules/Users/Users/Features/GetAllUsers/GetAllUsersEndpoint.cs`
- `src/modules/Users/Users/Features/GetAllUsers/GetAllUsersHandler.cs`
- `src/modules/Users/Users/Features/GetUserById/GetUserByIdEndpoint.cs`
- `src/modules/Users/Users/Features/GetUserById/GetUserByIdHandler.cs`
- `src/modules/Users/Users/UserService.cs`

**Files to delete later** (after Api references are updated in Task 6):
- `src/modules/Users/Users.csproj` (old flat location)
- `src/modules/Users/UsersModule.cs` (old flat location)

**Step 1: Create `Users.Contracts.csproj`**
```xml
<!-- src/modules/Users/Users.Contracts/Users.Contracts.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <OutputType>Library</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <ProjectReference Include="..\..\..\SimpleModule.Core\SimpleModule.Core.csproj" />
  </ItemGroup>
</Project>
```

**Step 2: Create `IUserContracts.cs`**
```csharp
// src/modules/Users/Users.Contracts/IUserContracts.cs
namespace SimpleModule.Users.Contracts;

public interface IUserContracts
{
    Task<IEnumerable<User>> GetAllUsersAsync();
    Task<User?> GetUserByIdAsync(int id);
}
```

**Step 3: Create `User.cs` (with `[Dto]`)**
```csharp
// src/modules/Users/Users.Contracts/User.cs
using SimpleModule.Core;

namespace SimpleModule.Users.Contracts;

[Dto]
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
```

**Step 4: Create the new `Users.csproj`**
```xml
<!-- src/modules/Users/Users/Users.csproj -->
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <OutputType>Library</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\..\..\SimpleModule.Core\SimpleModule.Core.csproj" />
    <ProjectReference Include="..\Users.Contracts\Users.Contracts.csproj" />
  </ItemGroup>
</Project>
```

**Step 5: Create `GetAllUsersHandler.cs`**
```csharp
// src/modules/Users/Users/Features/GetAllUsers/GetAllUsersHandler.cs
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Features.GetAllUsers;

public class GetAllUsersHandler(IUserContracts userContracts)
{
    public async Task<IEnumerable<User>> HandleAsync() =>
        await userContracts.GetAllUsersAsync();
}
```

**Step 6: Create `GetAllUsersEndpoint.cs`**
```csharp
// src/modules/Users/Users/Features/GetAllUsers/GetAllUsersEndpoint.cs
using Microsoft.AspNetCore.Routing;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Features.GetAllUsers;

public static class GetAllUsersEndpoint
{
    public static void Map(IEndpointRouteBuilder group)
    {
        group.MapGet("/", async (IUserContracts userContracts) =>
        {
            var users = await userContracts.GetAllUsersAsync();
            return Results.Ok(users);
        });
    }
}
```

**Step 7: Create `GetUserByIdHandler.cs`**
```csharp
// src/modules/Users/Users/Features/GetUserById/GetUserByIdHandler.cs
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Features.GetUserById;

public class GetUserByIdHandler(IUserContracts userContracts)
{
    public async Task<User?> HandleAsync(int id) =>
        await userContracts.GetUserByIdAsync(id);
}
```

**Step 8: Create `GetUserByIdEndpoint.cs`**
```csharp
// src/modules/Users/Users/Features/GetUserById/GetUserByIdEndpoint.cs
using Microsoft.AspNetCore.Routing;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Features.GetUserById;

public static class GetUserByIdEndpoint
{
    public static void Map(IEndpointRouteBuilder group)
    {
        group.MapGet("/{id}", async (int id, IUserContracts userContracts) =>
        {
            var user = await userContracts.GetUserByIdAsync(id);
            return user is not null ? Results.Ok(user) : Results.NotFound();
        });
    }
}
```

**Step 9: Create `UserService.cs`**
```csharp
// src/modules/Users/Users/UserService.cs
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users;

public class UserService : IUserContracts
{
    public Task<IEnumerable<User>> GetAllUsersAsync() =>
        Task.FromResult<IEnumerable<User>>(new[]
        {
            new User { Id = 1, Name = "John Doe" },
            new User { Id = 2, Name = "Jane Smith" },
        });

    public Task<User?> GetUserByIdAsync(int id) =>
        Task.FromResult<User?>(new User { Id = id, Name = $"User {id}" });
}
```

**Step 10: Create `UsersModule.cs`**
```csharp
// src/modules/Users/Users/UsersModule.cs
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;
using SimpleModule.Users.Contracts;
using SimpleModule.Users.Features.GetAllUsers;
using SimpleModule.Users.Features.GetUserById;

namespace SimpleModule.Users;

[Module("Users")]
public class UsersModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IUserContracts, UserService>();
    }

    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/users");
        GetAllUsersEndpoint.Map(group);
        GetUserByIdEndpoint.Map(group);
    }
}
```

**Step 11: Build the new Users project to verify**
```bash
dotnet build src/modules/Users/Users/Users.csproj
```
Expected: Build succeeded, 0 errors.

**Step 12: Commit new Users files (do NOT delete old files yet)**
```bash
git add src/modules/Users/Users/ src/modules/Users/Users.Contracts/
git commit -m "feat(users): split into Users/Users and Users/Users.Contracts with feature-based layout"
```

---

## Task 4: Create `Products.Contracts` and refactor `Products` module

Same pattern as Users. Old files deleted in Task 6.

**Files to create:**
- `src/modules/Products/Products.Contracts/Products.Contracts.csproj`
- `src/modules/Products/Products.Contracts/IProductContracts.cs`
- `src/modules/Products/Products.Contracts/Product.cs`
- `src/modules/Products/Products/Products.csproj`
- `src/modules/Products/Products/ProductsModule.cs`
- `src/modules/Products/Products/Features/GetAllProducts/GetAllProductsEndpoint.cs`
- `src/modules/Products/Products/Features/GetAllProducts/GetAllProductsHandler.cs`
- `src/modules/Products/Products/Features/GetProductById/GetProductByIdEndpoint.cs`
- `src/modules/Products/Products/Features/GetProductById/GetProductByIdHandler.cs`
- `src/modules/Products/Products/ProductService.cs`

**Step 1: Create `Products.Contracts.csproj`**
```xml
<!-- src/modules/Products/Products.Contracts/Products.Contracts.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <OutputType>Library</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <ProjectReference Include="..\..\..\SimpleModule.Core\SimpleModule.Core.csproj" />
  </ItemGroup>
</Project>
```

**Step 2: Create `IProductContracts.cs`**
```csharp
// src/modules/Products/Products.Contracts/IProductContracts.cs
namespace SimpleModule.Products.Contracts;

public interface IProductContracts
{
    Task<IEnumerable<Product>> GetAllProductsAsync();
    Task<Product?> GetProductByIdAsync(int id);
}
```

**Step 3: Create `Product.cs`**
```csharp
// src/modules/Products/Products.Contracts/Product.cs
using SimpleModule.Core;

namespace SimpleModule.Products.Contracts;

[Dto]
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
```

**Step 4: Create the new `Products.csproj`**
```xml
<!-- src/modules/Products/Products/Products.csproj -->
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <OutputType>Library</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\..\..\SimpleModule.Core\SimpleModule.Core.csproj" />
    <ProjectReference Include="..\Products.Contracts\Products.Contracts.csproj" />
  </ItemGroup>
</Project>
```

**Step 5: Create `GetAllProductsHandler.cs`**
```csharp
// src/modules/Products/Products/Features/GetAllProducts/GetAllProductsHandler.cs
using SimpleModule.Products.Contracts;

namespace SimpleModule.Products.Features.GetAllProducts;

public class GetAllProductsHandler(IProductContracts productContracts)
{
    public async Task<IEnumerable<Product>> HandleAsync() =>
        await productContracts.GetAllProductsAsync();
}
```

**Step 6: Create `GetAllProductsEndpoint.cs`**
```csharp
// src/modules/Products/Products/Features/GetAllProducts/GetAllProductsEndpoint.cs
using Microsoft.AspNetCore.Routing;
using SimpleModule.Products.Contracts;

namespace SimpleModule.Products.Features.GetAllProducts;

public static class GetAllProductsEndpoint
{
    public static void Map(IEndpointRouteBuilder group)
    {
        group.MapGet("/", async (IProductContracts productContracts) =>
        {
            var products = await productContracts.GetAllProductsAsync();
            return Results.Ok(products);
        });
    }
}
```

**Step 7: Create `GetProductByIdHandler.cs`**
```csharp
// src/modules/Products/Products/Features/GetProductById/GetProductByIdHandler.cs
using SimpleModule.Products.Contracts;

namespace SimpleModule.Products.Features.GetProductById;

public class GetProductByIdHandler(IProductContracts productContracts)
{
    public async Task<Product?> HandleAsync(int id) =>
        await productContracts.GetProductByIdAsync(id);
}
```

**Step 8: Create `GetProductByIdEndpoint.cs`**
```csharp
// src/modules/Products/Products/Features/GetProductById/GetProductByIdEndpoint.cs
using Microsoft.AspNetCore.Routing;
using SimpleModule.Products.Contracts;

namespace SimpleModule.Products.Features.GetProductById;

public static class GetProductByIdEndpoint
{
    public static void Map(IEndpointRouteBuilder group)
    {
        group.MapGet("/{id}", async (int id, IProductContracts productContracts) =>
        {
            var product = await productContracts.GetProductByIdAsync(id);
            return product is not null ? Results.Ok(product) : Results.NotFound();
        });
    }
}
```

**Step 9: Create `ProductService.cs`**
```csharp
// src/modules/Products/Products/ProductService.cs
using SimpleModule.Products.Contracts;

namespace SimpleModule.Products;

public class ProductService : IProductContracts
{
    public Task<IEnumerable<Product>> GetAllProductsAsync() =>
        Task.FromResult<IEnumerable<Product>>(new[]
        {
            new Product { Id = 1, Name = "Laptop", Price = 999.99m },
            new Product { Id = 2, Name = "Smartphone", Price = 699.99m },
        });

    public Task<Product?> GetProductByIdAsync(int id) =>
        Task.FromResult<Product?>(new Product { Id = id, Name = $"Product {id}", Price = 100.00m });
}
```

**Step 10: Create `ProductsModule.cs`**
```csharp
// src/modules/Products/Products/ProductsModule.cs
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;
using SimpleModule.Products.Contracts;
using SimpleModule.Products.Features.GetAllProducts;
using SimpleModule.Products.Features.GetProductById;

namespace SimpleModule.Products;

[Module("Products")]
public class ProductsModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IProductContracts, ProductService>();
    }

    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/products");
        GetAllProductsEndpoint.Map(group);
        GetProductByIdEndpoint.Map(group);
    }
}
```

**Step 11: Build**
```bash
dotnet build src/modules/Products/Products/Products.csproj
```
Expected: Build succeeded, 0 errors.

**Step 12: Commit**
```bash
git add src/modules/Products/Products/ src/modules/Products/Products.Contracts/
git commit -m "feat(products): split into Products/Products and Products/Products.Contracts with feature-based layout"
```

---

## Task 5: Create `Orders.Contracts` and refactor `Orders` module

Orders is more complex — it previously depended on `Users.csproj` and `Products.csproj` directly. Now it depends on `Users.Contracts` and `Products.Contracts` only.

**Files to create:**
- `src/modules/Orders/Orders.Contracts/Orders.Contracts.csproj`
- `src/modules/Orders/Orders.Contracts/IOrderContracts.cs`
- `src/modules/Orders/Orders.Contracts/Order.cs`
- `src/modules/Orders/Orders.Contracts/OrderItem.cs`
- `src/modules/Orders/Orders.Contracts/CreateOrderRequest.cs`
- `src/modules/Orders/Orders/Orders.csproj`
- `src/modules/Orders/Orders/OrdersModule.cs`
- `src/modules/Orders/Orders/Features/GetAllOrders/GetAllOrdersEndpoint.cs`
- `src/modules/Orders/Orders/Features/GetAllOrders/GetAllOrdersHandler.cs`
- `src/modules/Orders/Orders/Features/GetOrderById/GetOrderByIdEndpoint.cs`
- `src/modules/Orders/Orders/Features/GetOrderById/GetOrderByIdHandler.cs`
- `src/modules/Orders/Orders/Features/CreateOrder/CreateOrderEndpoint.cs`
- `src/modules/Orders/Orders/Features/CreateOrder/CreateOrderHandler.cs`
- `src/modules/Orders/Orders/OrderService.cs`

**Step 1: Create `Orders.Contracts.csproj`**
```xml
<!-- src/modules/Orders/Orders.Contracts/Orders.Contracts.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <OutputType>Library</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <ProjectReference Include="..\..\..\SimpleModule.Core\SimpleModule.Core.csproj" />
  </ItemGroup>
</Project>
```

**Step 2: Create `OrderItem.cs`**
```csharp
// src/modules/Orders/Orders.Contracts/OrderItem.cs
using SimpleModule.Core;

namespace SimpleModule.Orders.Contracts;

[Dto]
public class OrderItem
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}
```

**Step 3: Create `Order.cs`**
```csharp
// src/modules/Orders/Orders.Contracts/Order.cs
using SimpleModule.Core;

namespace SimpleModule.Orders.Contracts;

[Dto]
public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public List<OrderItem> Items { get; set; } = new();
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

**Step 4: Create `CreateOrderRequest.cs`**
```csharp
// src/modules/Orders/Orders.Contracts/CreateOrderRequest.cs
using SimpleModule.Core;

namespace SimpleModule.Orders.Contracts;

[Dto]
public class CreateOrderRequest
{
    public int UserId { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}
```

**Step 5: Create `IOrderContracts.cs`**
```csharp
// src/modules/Orders/Orders.Contracts/IOrderContracts.cs
namespace SimpleModule.Orders.Contracts;

public interface IOrderContracts
{
    Task<IEnumerable<Order>> GetAllOrdersAsync();
    Task<Order?> GetOrderByIdAsync(int id);
    Task<Order> CreateOrderAsync(CreateOrderRequest request);
}
```

**Step 6: Create the new `Orders.csproj`**

Note: Orders references `Users.Contracts` and `Products.Contracts` — NOT the Users or Products implementation projects.
```xml
<!-- src/modules/Orders/Orders/Orders.csproj -->
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <OutputType>Library</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\..\..\SimpleModule.Core\SimpleModule.Core.csproj" />
    <ProjectReference Include="..\Orders.Contracts\Orders.Contracts.csproj" />
    <ProjectReference Include="..\..\Users\Users.Contracts\Users.Contracts.csproj" />
    <ProjectReference Include="..\..\Products\Products.Contracts\Products.Contracts.csproj" />
  </ItemGroup>
</Project>
```

**Step 7: Create `OrderService.cs`**
```csharp
// src/modules/Orders/Orders/OrderService.cs
using SimpleModule.Orders.Contracts;
using SimpleModule.Products.Contracts;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Orders;

public class OrderService(IUserContracts users, IProductContracts products) : IOrderContracts
{
    private static int _nextId = 1;
    private static readonly List<Order> _orders = new();

    public Task<IEnumerable<Order>> GetAllOrdersAsync() =>
        Task.FromResult<IEnumerable<Order>>(_orders);

    public Task<Order?> GetOrderByIdAsync(int id) =>
        Task.FromResult(_orders.FirstOrDefault(o => o.Id == id));

    public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
    {
        var user = await users.GetUserByIdAsync(request.UserId);
        if (user is null)
            throw new InvalidOperationException($"User with ID {request.UserId} not found");

        decimal total = 0;
        foreach (var item in request.Items)
        {
            var product = await products.GetProductByIdAsync(item.ProductId);
            if (product is null)
                throw new InvalidOperationException($"Product with ID {item.ProductId} not found");

            total += product.Price * item.Quantity;
        }

        var order = new Order
        {
            Id = _nextId++,
            UserId = request.UserId,
            Items = request.Items,
            Total = total,
            CreatedAt = DateTime.UtcNow,
        };

        _orders.Add(order);
        return order;
    }
}
```

**Step 8: Create `GetAllOrdersHandler.cs`**
```csharp
// src/modules/Orders/Orders/Features/GetAllOrders/GetAllOrdersHandler.cs
using SimpleModule.Orders.Contracts;

namespace SimpleModule.Orders.Features.GetAllOrders;

public class GetAllOrdersHandler(IOrderContracts orderContracts)
{
    public async Task<IEnumerable<Order>> HandleAsync() =>
        await orderContracts.GetAllOrdersAsync();
}
```

**Step 9: Create `GetAllOrdersEndpoint.cs`**
```csharp
// src/modules/Orders/Orders/Features/GetAllOrders/GetAllOrdersEndpoint.cs
using Microsoft.AspNetCore.Routing;
using SimpleModule.Orders.Contracts;

namespace SimpleModule.Orders.Features.GetAllOrders;

public static class GetAllOrdersEndpoint
{
    public static void Map(IEndpointRouteBuilder group)
    {
        group.MapGet("/", async (IOrderContracts orderContracts) =>
        {
            var orders = await orderContracts.GetAllOrdersAsync();
            return Results.Ok(orders);
        });
    }
}
```

**Step 10: Create `GetOrderByIdHandler.cs`**
```csharp
// src/modules/Orders/Orders/Features/GetOrderById/GetOrderByIdHandler.cs
using SimpleModule.Orders.Contracts;

namespace SimpleModule.Orders.Features.GetOrderById;

public class GetOrderByIdHandler(IOrderContracts orderContracts)
{
    public async Task<Order?> HandleAsync(int id) =>
        await orderContracts.GetOrderByIdAsync(id);
}
```

**Step 11: Create `GetOrderByIdEndpoint.cs`**
```csharp
// src/modules/Orders/Orders/Features/GetOrderById/GetOrderByIdEndpoint.cs
using Microsoft.AspNetCore.Routing;
using SimpleModule.Orders.Contracts;

namespace SimpleModule.Orders.Features.GetOrderById;

public static class GetOrderByIdEndpoint
{
    public static void Map(IEndpointRouteBuilder group)
    {
        group.MapGet("/{id}", async (int id, IOrderContracts orderContracts) =>
        {
            var order = await orderContracts.GetOrderByIdAsync(id);
            return order is not null ? Results.Ok(order) : Results.NotFound();
        });
    }
}
```

**Step 12: Create `CreateOrderHandler.cs`**
```csharp
// src/modules/Orders/Orders/Features/CreateOrder/CreateOrderHandler.cs
using SimpleModule.Orders.Contracts;

namespace SimpleModule.Orders.Features.CreateOrder;

public class CreateOrderHandler(IOrderContracts orderContracts)
{
    public async Task<Order> HandleAsync(CreateOrderRequest request) =>
        await orderContracts.CreateOrderAsync(request);
}
```

**Step 13: Create `CreateOrderEndpoint.cs`**
```csharp
// src/modules/Orders/Orders/Features/CreateOrder/CreateOrderEndpoint.cs
using Microsoft.AspNetCore.Routing;
using SimpleModule.Orders.Contracts;

namespace SimpleModule.Orders.Features.CreateOrder;

public static class CreateOrderEndpoint
{
    public static void Map(IEndpointRouteBuilder group)
    {
        group.MapPost("/", async (CreateOrderRequest request, IOrderContracts orderContracts) =>
        {
            var order = await orderContracts.CreateOrderAsync(request);
            return Results.Created($"/api/orders/{order.Id}", order);
        });
    }
}
```

**Step 14: Create `OrdersModule.cs`**
```csharp
// src/modules/Orders/Orders/OrdersModule.cs
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;
using SimpleModule.Orders.Contracts;
using SimpleModule.Orders.Features.CreateOrder;
using SimpleModule.Orders.Features.GetAllOrders;
using SimpleModule.Orders.Features.GetOrderById;

namespace SimpleModule.Orders;

[Module("Orders")]
public class OrdersModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IOrderContracts, OrderService>();
    }

    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/orders");
        GetAllOrdersEndpoint.Map(group);
        GetOrderByIdEndpoint.Map(group);
        CreateOrderEndpoint.Map(group);
    }
}
```

**Step 15: Build**
```bash
dotnet build src/modules/Orders/Orders/Orders.csproj
```
Expected: Build succeeded, 0 errors.

**Step 16: Commit**
```bash
git add src/modules/Orders/Orders/ src/modules/Orders/Orders.Contracts/
git commit -m "feat(orders): split into Orders/Orders and Orders/Orders.Contracts; depend on Users.Contracts and Products.Contracts only"
```

---

## Task 6: Update `SimpleModule.Api`, solution file, delete old module files, verify full build

**Files:**
- Modify: `src/SimpleModule.Api/SimpleModule.Api.csproj`
- Modify: `SimpleModule.sln`
- Delete: `src/modules/Users/Users.csproj`, `src/modules/Users/UsersModule.cs`
- Delete: `src/modules/Products/Products.csproj`, `src/modules/Products/ProductsModule.cs`
- Delete: `src/modules/Orders/Orders.csproj`, `src/modules/Orders/OrdersModule.cs`

**Step 1: Update `SimpleModule.Api.csproj`**

Replace the module `ProjectReference` entries. The new paths are one level deeper:
```xml
<!-- src/SimpleModule.Api/SimpleModule.Api.csproj -->
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <InvariantGlobalization>true</InvariantGlobalization>
    <PublishAot>true</PublishAot>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.3" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="10.1.4" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\SimpleModule.Core\SimpleModule.Core.csproj" />
    <ProjectReference
      Include="..\SimpleModule.Generator\SimpleModule.Generator.csproj"
      OutputItemType="Analyzer"
      ReferenceOutputAssembly="false"
    />
    <ProjectReference Include="..\modules\Users\Users\Users.csproj" />
    <ProjectReference Include="..\modules\Products\Products\Products.csproj" />
    <ProjectReference Include="..\modules\Orders\Orders\Orders.csproj" />
  </ItemGroup>
</Project>
```

**Step 2: Update `SimpleModule.sln`**

Update the three existing module project paths and add the three new `.Contracts` projects. Use new GUIDs for the contracts projects (generate with `[System.Guid]::NewGuid()` in PowerShell or just use the values below).

Replace the three old module `Project(...)` entries:
```
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Users", "src\modules\Users\Users\Users.csproj", "{F1G2H3I4-J5K6-52F3-A4B5-C6D7E8F9G0H1}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Users.Contracts", "src\modules\Users\Users.Contracts\Users.Contracts.csproj", "{A2B3C4D5-E6F7-4890-B1C2-D3E4F5A6B7C8}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Products", "src\modules\Products\Products\Products.csproj", "{G1H2I3J4-K5L6-53G4-B5C6-D7E8F9G0H1I2}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Products.Contracts", "src\modules\Products\Products.Contracts\Products.Contracts.csproj", "{B3C4D5E6-F7A8-4901-C2D3-E4F5A6B7C8D9}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Orders", "src\modules\Orders\Orders\Orders.csproj", "{H1I2J3K4-L5M6-54H5-C6D7-E8F9G0H1I2J3}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Orders.Contracts", "src\modules\Orders\Orders.Contracts\Orders.Contracts.csproj", "{C4D5E6F7-A8B9-4012-D3E4-F5A6B7C8D9E0}"
EndProject
```

Also add the three new contract project GUIDs to `GlobalSection(ProjectConfigurationPlatforms)`:
```
{A2B3C4D5-E6F7-4890-B1C2-D3E4F5A6B7C8}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
{A2B3C4D5-E6F7-4890-B1C2-D3E4F5A6B7C8}.Debug|Any CPU.Build.0 = Debug|Any CPU
{A2B3C4D5-E6F7-4890-B1C2-D3E4F5A6B7C8}.Release|Any CPU.ActiveCfg = Release|Any CPU
{A2B3C4D5-E6F7-4890-B1C2-D3E4F5A6B7C8}.Release|Any CPU.Build.0 = Release|Any CPU
{B3C4D5E6-F7A8-4901-C2D3-E4F5A6B7C8D9}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
{B3C4D5E6-F7A8-4901-C2D3-E4F5A6B7C8D9}.Debug|Any CPU.Build.0 = Debug|Any CPU
{B3C4D5E6-F7A8-4901-C2D3-E4F5A6B7C8D9}.Release|Any CPU.ActiveCfg = Release|Any CPU
{B3C4D5E6-F7A8-4901-C2D3-E4F5A6B7C8D9}.Release|Any CPU.Build.0 = Release|Any CPU
{C4D5E6F7-A8B9-4012-D3E4-F5A6B7C8D9E0}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
{C4D5E6F7-A8B9-4012-D3E4-F5A6B7C8D9E0}.Debug|Any CPU.Build.0 = Debug|Any CPU
{C4D5E6F7-A8B9-4012-D3E4-F5A6B7C8D9E0}.Release|Any CPU.ActiveCfg = Release|Any CPU
{C4D5E6F7-A8B9-4012-D3E4-F5A6B7C8D9E0}.Release|Any CPU.Build.0 = Release|Any CPU
```

And nest the contracts projects under the `modules` solution folder in `NestedProjects`:
```
{A2B3C4D5-E6F7-4890-B1C2-D3E4F5A6B7C8} = {B1C2D3E4-F5G6-48B9-C0D1-E2F3A4B5C6D7}
{B3C4D5E6-F7A8-4901-C2D3-E4F5A6B7C8D9} = {B1C2D3E4-F5G6-48B9-C0D1-E2F3A4B5C6D7}
{C4D5E6F7-A8B9-4012-D3E4-F5A6B7C8D9E0} = {B1C2D3E4-F5G6-48B9-C0D1-E2F3A4B5C6D7}
```

**Step 3: Build the full solution**
```bash
dotnet build SimpleModule.sln
```
Expected: Build succeeded, 0 errors, 0 warnings (or only AOT-related warnings which are pre-existing).

**Step 4: Delete old flat module files**
```bash
rm src/modules/Users/Users.csproj src/modules/Users/UsersModule.cs
rm src/modules/Products/Products.csproj src/modules/Products/ProductsModule.cs
rm src/modules/Orders/Orders.csproj src/modules/Orders/OrdersModule.cs
```

**Step 5: Build again to confirm deletions didn't break anything**
```bash
dotnet build SimpleModule.sln
```
Expected: Build succeeded, 0 errors.

**Step 6: Update CLAUDE.md — "Adding a New Module" section**

Update the steps in CLAUDE.md to reflect the new two-project-per-module structure:

```markdown
## Adding a New Module

1. Create folder `src/modules/<Name>/`
2. Create `src/modules/<Name>/<Name>.Contracts/` with:
   - `<Name>.Contracts.csproj` (references Core only, uses `Microsoft.NET.Sdk`)
   - `I<Name>Contracts.cs` — public interface for cross-module use
   - Shared DTO types marked with `[Dto]`
3. Create `src/modules/<Name>/<Name>/` with:
   - `<Name>.csproj` (references Core + `<Name>.Contracts`; uses `Microsoft.NET.Sdk.Web`)
   - `<Name>Module.cs` — implements `IModule` with `[Module("Name")]`
   - `Features/<FeatureName>/` folders containing endpoint and handler classes
   - Register the contract interface against implementation in `ConfigureServices`
4. Add `ProjectReference` to `src/SimpleModule.Api/SimpleModule.Api.csproj` pointing to `<Name>/<Name>.csproj`
5. Add both projects to `SimpleModule.sln`
```

**Step 7: Final commit**
```bash
git add src/SimpleModule.Api/SimpleModule.Api.csproj SimpleModule.sln CLAUDE.md
git add -u src/modules/  # stage deletions
git commit -m "feat: wire up new module structure in Api and sln; remove old flat module files; update CLAUDE.md"
```
