# Settings Module Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build a centralized Settings module with key-value storage, cascade resolution (User > App > Code Default), admin/user UI, and module-extensible settings registration via `ConfigureSettings` on `IModule`.

**Architecture:** Key-value store in a single `Settings` table with scope + userId columns. Core framework provides `ISettingsBuilder`/`ISettingsDefinitionRegistry` (mirrors menu pattern). Source generator creates `CollectModuleSettings()`. Settings.Contracts exposes `ISettingsContracts` for cross-module reads/writes. React frontend with admin (tabbed System/App) and user preference pages.

**Tech Stack:** .NET 10, EF Core, Roslyn IIncrementalGenerator (netstandard2.0), React 19, Inertia.js, Vite, Radix UI, Tailwind CSS, xUnit.v3, FluentAssertions, Playwright.

---

### Task 1: Add Core Settings Types

**Files:**
- Create: `framework/SimpleModule.Core/Settings/SettingScope.cs`
- Create: `framework/SimpleModule.Core/Settings/SettingType.cs`
- Create: `framework/SimpleModule.Core/Settings/SettingDefinition.cs`
- Create: `framework/SimpleModule.Core/Settings/ISettingsBuilder.cs`
- Create: `framework/SimpleModule.Core/Settings/SettingsBuilder.cs`
- Create: `framework/SimpleModule.Core/Settings/ISettingsDefinitionRegistry.cs`
- Create: `framework/SimpleModule.Core/Settings/SettingsDefinitionRegistry.cs`
- Modify: `framework/SimpleModule.Core/IModule.cs`
- Test: `tests/SimpleModule.Core.Tests/Settings/SettingsBuilderTests.cs`
- Test: `tests/SimpleModule.Core.Tests/Settings/SettingsDefinitionRegistryTests.cs`

**Step 1: Write failing tests for SettingsBuilder**

```csharp
// tests/SimpleModule.Core.Tests/Settings/SettingsBuilderTests.cs
using FluentAssertions;
using SimpleModule.Core.Settings;

namespace SimpleModule.Core.Tests.Settings;

public class SettingsBuilderTests
{
    [Fact]
    public void Add_SingleDefinition_ReturnsInList()
    {
        var builder = new SettingsBuilder();
        var def = new SettingDefinition
        {
            Key = "app.theme",
            DisplayName = "Theme",
            Scope = SettingScope.Application,
            DefaultValue = "\"light\"",
            Type = SettingType.String,
        };

        builder.Add(def);

        builder.ToList().Should().ContainSingle().Which.Key.Should().Be("app.theme");
    }

    [Fact]
    public void Add_MultipleDefinitions_ReturnsAll()
    {
        var builder = new SettingsBuilder();
        builder.Add(new SettingDefinition { Key = "a", DisplayName = "A" });
        builder.Add(new SettingDefinition { Key = "b", DisplayName = "B" });

        builder.ToList().Should().HaveCount(2);
    }

    [Fact]
    public void Add_ReturnsSelf_ForChaining()
    {
        var builder = new SettingsBuilder();
        var result = builder.Add(new SettingDefinition { Key = "a", DisplayName = "A" });

        result.Should().BeSameAs(builder);
    }
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/SimpleModule.Core.Tests --filter "FullyQualifiedName~SettingsBuilderTests" -v minimal`
Expected: FAIL — types don't exist yet

**Step 3: Write failing tests for SettingsDefinitionRegistry**

```csharp
// tests/SimpleModule.Core.Tests/Settings/SettingsDefinitionRegistryTests.cs
using FluentAssertions;
using SimpleModule.Core.Settings;

namespace SimpleModule.Core.Tests.Settings;

public class SettingsDefinitionRegistryTests
{
    [Fact]
    public void GetDefinitions_NoFilter_ReturnsAll()
    {
        var registry = CreateRegistry();
        registry.GetDefinitions().Should().HaveCount(3);
    }

    [Fact]
    public void GetDefinitions_FilterByScope_ReturnsMatching()
    {
        var registry = CreateRegistry();
        registry.GetDefinitions(SettingScope.System).Should().ContainSingle();
    }

    [Fact]
    public void GetDefinitions_NoMatchingScope_ReturnsEmpty()
    {
        var registry = new SettingsDefinitionRegistry([]);
        registry.GetDefinitions(SettingScope.System).Should().BeEmpty();
    }

    [Fact]
    public void GetDefinition_ExistingKey_ReturnsDefinition()
    {
        var registry = CreateRegistry();
        var def = registry.GetDefinition("smtp.host");
        def.Should().NotBeNull();
        def!.DisplayName.Should().Be("SMTP Host");
    }

    [Fact]
    public void GetDefinition_UnknownKey_ReturnsNull()
    {
        var registry = CreateRegistry();
        registry.GetDefinition("nonexistent").Should().BeNull();
    }

    private static SettingsDefinitionRegistry CreateRegistry() =>
        new(
        [
            new SettingDefinition
            {
                Key = "smtp.host",
                DisplayName = "SMTP Host",
                Group = "Email",
                Scope = SettingScope.System,
                Type = SettingType.String,
            },
            new SettingDefinition
            {
                Key = "app.title",
                DisplayName = "App Title",
                Group = "General",
                Scope = SettingScope.Application,
                DefaultValue = "\"SimpleModule\"",
                Type = SettingType.String,
            },
            new SettingDefinition
            {
                Key = "user.theme",
                DisplayName = "Theme",
                Group = "Appearance",
                Scope = SettingScope.User,
                DefaultValue = "\"light\"",
                Type = SettingType.String,
            },
        ]);
}
```

**Step 4: Implement Core Settings types**

```csharp
// framework/SimpleModule.Core/Settings/SettingScope.cs
namespace SimpleModule.Core.Settings;

public enum SettingScope
{
    System = 0,
    Application = 1,
    User = 2,
}
```

```csharp
// framework/SimpleModule.Core/Settings/SettingType.cs
namespace SimpleModule.Core.Settings;

public enum SettingType
{
    String = 0,
    Number = 1,
    Boolean = 2,
    Json = 3,
}
```

```csharp
// framework/SimpleModule.Core/Settings/SettingDefinition.cs
namespace SimpleModule.Core.Settings;

public class SettingDefinition
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Group { get; set; }
    public SettingScope Scope { get; set; }
    public string? DefaultValue { get; set; }
    public SettingType Type { get; set; }
}
```

```csharp
// framework/SimpleModule.Core/Settings/ISettingsBuilder.cs
namespace SimpleModule.Core.Settings;

public interface ISettingsBuilder
{
    ISettingsBuilder Add(SettingDefinition definition);
}
```

```csharp
// framework/SimpleModule.Core/Settings/SettingsBuilder.cs
namespace SimpleModule.Core.Settings;

public sealed class SettingsBuilder : ISettingsBuilder
{
    private readonly List<SettingDefinition> _definitions = [];

    public ISettingsBuilder Add(SettingDefinition definition)
    {
        _definitions.Add(definition);
        return this;
    }

    public List<SettingDefinition> ToList() => [.. _definitions];
}
```

```csharp
// framework/SimpleModule.Core/Settings/ISettingsDefinitionRegistry.cs
namespace SimpleModule.Core.Settings;

public interface ISettingsDefinitionRegistry
{
    IReadOnlyList<SettingDefinition> GetDefinitions(SettingScope? scope = null);
    SettingDefinition? GetDefinition(string key);
}
```

```csharp
// framework/SimpleModule.Core/Settings/SettingsDefinitionRegistry.cs
namespace SimpleModule.Core.Settings;

public sealed class SettingsDefinitionRegistry(List<SettingDefinition> definitions)
    : ISettingsDefinitionRegistry
{
    private readonly Dictionary<string, SettingDefinition> _byKey =
        definitions.ToDictionary(d => d.Key);

    public IReadOnlyList<SettingDefinition> GetDefinitions(SettingScope? scope = null)
    {
        if (scope is null)
            return definitions.AsReadOnly();

        return definitions.Where(d => d.Scope == scope.Value).ToList().AsReadOnly();
    }

    public SettingDefinition? GetDefinition(string key) =>
        _byKey.GetValueOrDefault(key);
}
```

**Step 5: Add ConfigureSettings to IModule**

Modify `framework/SimpleModule.Core/IModule.cs` — add after the `ConfigurePermissions` method:

```csharp
virtual void ConfigureSettings(ISettingsBuilder settings) { }
```

Add `using SimpleModule.Core.Settings;` to the file.

**Step 6: Run tests to verify they pass**

Run: `dotnet test tests/SimpleModule.Core.Tests --filter "FullyQualifiedName~Settings" -v minimal`
Expected: All 8 tests PASS

**Step 7: Commit**

```bash
git add framework/SimpleModule.Core/Settings/ framework/SimpleModule.Core/IModule.cs tests/SimpleModule.Core.Tests/Settings/
git commit -m "feat(settings): add core settings types, builder, registry, and IModule.ConfigureSettings"
```

---

### Task 2: Update Source Generator

**Files:**
- Modify: `framework/SimpleModule.Generator/Discovery/DiscoveryData.cs`
- Modify: `framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs`
- Create: `framework/SimpleModule.Generator/Emitters/SettingsExtensionsEmitter.cs`
- Modify: `framework/SimpleModule.Generator/ModuleDiscovererGenerator.cs`
- Test: `tests/SimpleModule.Generator.Tests/` (add settings generation test)

**Step 1: Write failing test for settings extension generation**

Check existing generator test patterns first — read `tests/SimpleModule.Generator.Tests/MenuExtensionsGenerationTests.cs` for the pattern, then write a parallel test for settings.

**Step 2: Update DiscoveryData.cs**

Add `HasConfigureSettings` to `ModuleInfoRecord`:

```csharp
// In the ModuleInfoRecord record struct, add parameter after HasConfigurePermissions:
bool HasConfigureSettings,
```

Update `Equals` method — add:
```csharp
&& HasConfigureSettings == other.HasConfigureSettings
```

Update `GetHashCode` — add:
```csharp
hash = hash * 31 + HasConfigureSettings.GetHashCode();
```

Add `HasConfigureSettings` to `ModuleInfo` class:
```csharp
public bool HasConfigureSettings { get; set; }
```

**Step 3: Update SymbolDiscovery.cs**

In the `FindModuleTypes` method, after the `HasConfigurePermissions` line (line ~275), add:

```csharp
HasConfigureSettings = DeclaresMethod(typeSymbol, "ConfigureSettings"),
```

In the `Extract` method where `ModuleInfoRecord` is constructed (around line ~162), add the field:

```csharp
m.HasConfigureSettings,
```

after `m.HasConfigurePermissions,`

**Step 4: Create SettingsExtensionsEmitter.cs**

```csharp
// framework/SimpleModule.Generator/Emitters/SettingsExtensionsEmitter.cs
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace SimpleModule.Generator;

internal sealed class SettingsExtensionsEmitter : IEmitter
{
    public void Emit(SourceProductionContext context, DiscoveryData data)
    {
        var modules = data.Modules;

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine("using SimpleModule.Core.Settings;");
        sb.AppendLine();
        sb.AppendLine("namespace SimpleModule.Core;");
        sb.AppendLine();
        sb.AppendLine("public static class SettingsExtensions");
        sb.AppendLine("{");
        sb.AppendLine(
            "    public static IServiceCollection CollectModuleSettings(this IServiceCollection services)"
        );
        sb.AppendLine("    {");
        sb.AppendLine("        var settings = new SettingsBuilder();");

        foreach (var module in modules.Where(m => m.HasConfigureSettings))
        {
            var fieldName = TypeMappingHelpers.GetModuleFieldName(module.FullyQualifiedName);
            sb.AppendLine($"        ModuleExtensions.{fieldName}.ConfigureSettings(settings);");
        }

        sb.AppendLine(
            "        services.AddSingleton<ISettingsDefinitionRegistry>(new SettingsDefinitionRegistry(settings.ToList()));"
        );
        sb.AppendLine("        return services;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource(
            "SettingsExtensions.g.cs",
            SourceText.From(sb.ToString(), Encoding.UTF8)
        );
    }
}
```

**Step 5: Register emitter in ModuleDiscovererGenerator.cs**

Add `new SettingsExtensionsEmitter(),` to the `Emitters` array after `new MenuExtensionsEmitter(),`

**Step 6: Run generator tests**

Run: `dotnet test tests/SimpleModule.Generator.Tests -v minimal`
Expected: PASS

**Step 7: Commit**

```bash
git add framework/SimpleModule.Generator/ tests/SimpleModule.Generator.Tests/
git commit -m "feat(settings): add source generator for CollectModuleSettings()"
```

---

### Task 3: Create Settings.Contracts Project

**Files:**
- Create: `modules/Settings/src/Settings.Contracts/Settings.Contracts.csproj`
- Create: `modules/Settings/src/Settings.Contracts/ISettingsContracts.cs`
- Create: `modules/Settings/src/Settings.Contracts/Setting.cs`
- Create: `modules/Settings/src/Settings.Contracts/UpdateSettingRequest.cs`
- Create: `modules/Settings/src/Settings.Contracts/SettingsFilter.cs`

**Step 1: Create project file**

```xml
<!-- modules/Settings/src/Settings.Contracts/Settings.Contracts.csproj -->
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

**Step 2: Create ISettingsContracts.cs**

```csharp
using SimpleModule.Core.Settings;

namespace SimpleModule.Settings.Contracts;

public interface ISettingsContracts
{
    Task<string?> GetSettingAsync(string key, SettingScope scope, string? userId = null);
    Task<T?> GetSettingAsync<T>(string key, SettingScope scope, string? userId = null);
    Task<string?> ResolveUserSettingAsync(string key, string userId);
    Task SetSettingAsync(string key, string value, SettingScope scope, string? userId = null);
    Task DeleteSettingAsync(string key, SettingScope scope, string? userId = null);
    Task<IEnumerable<Setting>> GetSettingsAsync(SettingsFilter? filter = null);
}
```

**Step 3: Create Setting.cs**

```csharp
using SimpleModule.Core;
using SimpleModule.Core.Settings;

namespace SimpleModule.Settings.Contracts;

[Dto]
public class Setting
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public SettingScope Scope { get; set; }
    public string? UserId { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
```

**Step 4: Create UpdateSettingRequest.cs**

```csharp
using SimpleModule.Core;
using SimpleModule.Core.Settings;

namespace SimpleModule.Settings.Contracts;

[Dto]
public class UpdateSettingRequest
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public SettingScope Scope { get; set; }
}
```

**Step 5: Create SettingsFilter.cs**

```csharp
using SimpleModule.Core;
using SimpleModule.Core.Settings;

namespace SimpleModule.Settings.Contracts;

[Dto]
public class SettingsFilter
{
    public SettingScope? Scope { get; set; }
    public string? Group { get; set; }
}
```

**Step 6: Add to solution file**

Add to `SimpleModule.slnx` in a new `/modules/Settings/` folder:
```xml
<Folder Name="/modules/Settings/">
    <Project Path="modules/Settings/src/Settings.Contracts/Settings.Contracts.csproj" />
</Folder>
```

**Step 7: Verify build**

Run: `dotnet build modules/Settings/src/Settings.Contracts`
Expected: Build succeeded

**Step 8: Commit**

```bash
git add modules/Settings/src/Settings.Contracts/ SimpleModule.slnx
git commit -m "feat(settings): create Settings.Contracts project with DTOs and interface"
```

---

### Task 4: Create Settings Module — Entity, DbContext, Service

**Files:**
- Create: `modules/Settings/src/Settings/Settings.csproj`
- Create: `modules/Settings/src/Settings/SettingsConstants.cs`
- Create: `modules/Settings/src/Settings/Entities/SettingEntity.cs`
- Create: `modules/Settings/src/Settings/EntityConfigurations/SettingEntityConfiguration.cs`
- Create: `modules/Settings/src/Settings/SettingsDbContext.cs`
- Create: `modules/Settings/src/Settings/SettingsService.cs`
- Create: `modules/Settings/src/Settings/SettingsModule.cs`

**Step 1: Create Settings.csproj**

```xml
<!-- modules/Settings/src/Settings/Settings.csproj -->
<Project Sdk="Microsoft.NET.Sdk.Razor">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <ProjectReference Include="..\..\..\..\framework\SimpleModule.Core\SimpleModule.Core.csproj" />
    <ProjectReference Include="..\..\..\..\framework\SimpleModule.Database\SimpleModule.Database.csproj" />
    <ProjectReference Include="..\Settings.Contracts\Settings.Contracts.csproj" />
  </ItemGroup>
  <ItemGroup>
    <None Update="Views\*.tsx">
      <DependentUpon>%(Filename)Endpoint.cs</DependentUpon>
    </None>
  </ItemGroup>
  <Target
    Name="JsBuild"
    BeforeTargets="Build"
    Condition="Exists('package.json') And (Exists('node_modules') Or Exists('$(RepoRoot)node_modules'))"
  >
    <Exec Command="npx vite build" WorkingDirectory="$(MSBuildProjectDirectory)" />
  </Target>
</Project>
```

**Step 2: Create SettingsConstants.cs**

```csharp
namespace SimpleModule.Settings;

public static class SettingsConstants
{
    public const string ModuleName = "Settings";
    public const string RoutePrefix = "/api/settings";
}
```

**Step 3: Create SettingEntity.cs**

```csharp
using SimpleModule.Core.Settings;

namespace SimpleModule.Settings.Entities;

public class SettingEntity
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public SettingScope Scope { get; set; }
    public string? UserId { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
```

**Step 4: Create SettingEntityConfiguration.cs**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleModule.Core.Settings;
using SimpleModule.Settings.Entities;

namespace SimpleModule.Settings.EntityConfigurations;

public class SettingEntityConfiguration : IEntityTypeConfiguration<SettingEntity>
{
    public void Configure(EntityTypeBuilder<SettingEntity> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedOnAdd();
        builder.Property(s => s.Key).IsRequired().HasMaxLength(256);
        builder.Property(s => s.Value).IsRequired(false);
        builder.Property(s => s.Scope).HasConversion<int>();
        builder.Property(s => s.UserId).IsRequired(false).HasMaxLength(450);
        builder.Property(s => s.UpdatedAt).IsRequired();

        builder.HasIndex(s => new { s.Key, s.Scope, s.UserId }).IsUnique();
    }
}
```

**Step 5: Create SettingsDbContext.cs**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimpleModule.Database;
using SimpleModule.Settings.Entities;

namespace SimpleModule.Settings;

public class SettingsDbContext(
    DbContextOptions<SettingsDbContext> options,
    IOptions<DatabaseOptions> dbOptions
) : DbContext(options)
{
    public DbSet<SettingEntity> Settings => Set<SettingEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SettingsDbContext).Assembly);
        modelBuilder.ApplyModuleSchema("Settings", dbOptions.Value);
    }
}
```

**Step 6: Create SettingsService.cs**

```csharp
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SimpleModule.Core.Settings;
using SimpleModule.Settings.Contracts;
using SimpleModule.Settings.Entities;

namespace SimpleModule.Settings;

public partial class SettingsService(
    SettingsDbContext db,
    ISettingsDefinitionRegistry definitions,
    ILogger<SettingsService> logger
) : ISettingsContracts
{
    public async Task<string?> GetSettingAsync(
        string key,
        SettingScope scope,
        string? userId = null
    )
    {
        var entity = await db.Settings.FirstOrDefaultAsync(s =>
            s.Key == key
            && s.Scope == scope
            && (scope == SettingScope.User ? s.UserId == userId : s.UserId == null)
        );
        return entity?.Value;
    }

    public async Task<T?> GetSettingAsync<T>(
        string key,
        SettingScope scope,
        string? userId = null
    )
    {
        var value = await GetSettingAsync(key, scope, userId);
        if (value is null)
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(value);
        }
        catch (JsonException ex)
        {
            LogDeserializationError(key, typeof(T).Name, ex.Message);
            return default;
        }
    }

    public async Task<string?> ResolveUserSettingAsync(string key, string userId)
    {
        var userValue = await GetSettingAsync(key, SettingScope.User, userId);
        if (userValue is not null)
            return userValue;

        var appValue = await GetSettingAsync(key, SettingScope.Application);
        if (appValue is not null)
            return appValue;

        var definition = definitions.GetDefinition(key);
        return definition?.DefaultValue;
    }

    public async Task SetSettingAsync(
        string key,
        string value,
        SettingScope scope,
        string? userId = null
    )
    {
        var existing = await db.Settings.FirstOrDefaultAsync(s =>
            s.Key == key
            && s.Scope == scope
            && (scope == SettingScope.User ? s.UserId == userId : s.UserId == null)
        );

        if (existing is not null)
        {
            existing.Value = value;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            db.Settings.Add(
                new SettingEntity
                {
                    Key = key,
                    Value = value,
                    Scope = scope,
                    UserId = scope == SettingScope.User ? userId : null,
                    UpdatedAt = DateTimeOffset.UtcNow,
                }
            );
        }

        await db.SaveChangesAsync();
        LogSettingUpdated(key, scope.ToString());
    }

    public async Task DeleteSettingAsync(string key, SettingScope scope, string? userId = null)
    {
        var entity = await db.Settings.FirstOrDefaultAsync(s =>
            s.Key == key
            && s.Scope == scope
            && (scope == SettingScope.User ? s.UserId == userId : s.UserId == null)
        );

        if (entity is not null)
        {
            db.Settings.Remove(entity);
            await db.SaveChangesAsync();
            LogSettingDeleted(key, scope.ToString());
        }
    }

    public async Task<IEnumerable<Setting>> GetSettingsAsync(SettingsFilter? filter = null)
    {
        var query = db.Settings.AsQueryable();

        if (filter?.Scope is not null)
            query = query.Where(s => s.Scope == filter.Scope.Value);

        var entities = await query.ToListAsync();
        return entities.Select(e => new Setting
        {
            Key = e.Key,
            Value = e.Value,
            Scope = e.Scope,
            UserId = e.UserId,
            UpdatedAt = e.UpdatedAt,
        });
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Setting {Key} updated in scope {Scope}")]
    private partial void LogSettingUpdated(string key, string scope);

    [LoggerMessage(Level = LogLevel.Information, Message = "Setting {Key} deleted from scope {Scope}")]
    private partial void LogSettingDeleted(string key, string scope);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to deserialize setting {Key} to type {Type}: {Error}"
    )]
    private partial void LogDeserializationError(string key, string type, string error);
}
```

**Step 7: Create SettingsModule.cs**

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;
using SimpleModule.Core.Menu;
using SimpleModule.Core.Settings;
using SimpleModule.Database;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.Settings;

[Module(
    SettingsConstants.ModuleName,
    RoutePrefix = SettingsConstants.RoutePrefix,
    ViewPrefix = "/settings"
)]
public class SettingsModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<SettingsDbContext>(configuration, SettingsConstants.ModuleName);
        services.AddScoped<ISettingsContracts, SettingsService>();
    }

    public void ConfigureMenu(IMenuBuilder menus)
    {
        menus.Add(
            new MenuItem
            {
                Label = "Settings",
                Url = "/settings",
                Icon = "Settings",
                Order = 90,
                Section = MenuSection.AppSidebar,
            }
        );
    }

    public void ConfigureSettings(ISettingsBuilder settings)
    {
        settings
            .Add(new SettingDefinition
            {
                Key = "app.title",
                DisplayName = "Application Title",
                Group = "General",
                Scope = SettingScope.Application,
                DefaultValue = "\"SimpleModule\"",
                Type = SettingType.String,
            })
            .Add(new SettingDefinition
            {
                Key = "app.theme",
                DisplayName = "Theme",
                Description = "Default color theme for the application",
                Group = "Appearance",
                Scope = SettingScope.User,
                DefaultValue = "\"light\"",
                Type = SettingType.String,
            })
            .Add(new SettingDefinition
            {
                Key = "app.language",
                DisplayName = "Language",
                Description = "Default language for the application",
                Group = "General",
                Scope = SettingScope.User,
                DefaultValue = "\"en\"",
                Type = SettingType.String,
            })
            .Add(new SettingDefinition
            {
                Key = "app.timezone",
                DisplayName = "Timezone",
                Group = "General",
                Scope = SettingScope.Application,
                DefaultValue = "\"UTC\"",
                Type = SettingType.String,
            })
            .Add(new SettingDefinition
            {
                Key = "system.maintenance_mode",
                DisplayName = "Maintenance Mode",
                Description = "When enabled, the application shows a maintenance page to non-admin users",
                Group = "System",
                Scope = SettingScope.System,
                DefaultValue = "false",
                Type = SettingType.Boolean,
            })
            .Add(new SettingDefinition
            {
                Key = "system.registration_enabled",
                DisplayName = "User Registration",
                Description = "Allow new users to register",
                Group = "System",
                Scope = SettingScope.System,
                DefaultValue = "true",
                Type = SettingType.Boolean,
            });
    }
}
```

**Step 8: Add to solution and Host**

Add `Settings.csproj` to `SimpleModule.slnx` under `/modules/Settings/`:
```xml
<Project Path="modules/Settings/src/Settings/Settings.csproj" />
```

Add to `template/SimpleModule.Host/SimpleModule.Host.csproj`:
```xml
<ProjectReference Include="..\..\modules\Settings\src\Settings\Settings.csproj" />
```

Add to `template/SimpleModule.Host/Program.cs` after `CollectModuleMenuItems()`:
```csharp
builder.Services.CollectModuleSettings();
```

**Step 9: Verify build**

Run: `dotnet build`
Expected: Build succeeded

**Step 10: Commit**

```bash
git add modules/Settings/src/Settings/ SimpleModule.slnx template/SimpleModule.Host/
git commit -m "feat(settings): create Settings module with entity, DbContext, service, and module class"
```

---

### Task 5: Create API Endpoints

**Files:**
- Create: `modules/Settings/src/Settings/Endpoints/Settings/GetSettingsEndpoint.cs`
- Create: `modules/Settings/src/Settings/Endpoints/Settings/GetSettingEndpoint.cs`
- Create: `modules/Settings/src/Settings/Endpoints/Settings/UpdateSettingEndpoint.cs`
- Create: `modules/Settings/src/Settings/Endpoints/Settings/DeleteSettingEndpoint.cs`
- Create: `modules/Settings/src/Settings/Endpoints/Settings/GetDefinitionsEndpoint.cs`
- Create: `modules/Settings/src/Settings/Endpoints/UserSettings/GetMySettingsEndpoint.cs`
- Create: `modules/Settings/src/Settings/Endpoints/UserSettings/UpdateMySettingEndpoint.cs`

**Step 1: Create admin endpoints**

```csharp
// modules/Settings/src/Settings/Endpoints/Settings/GetSettingsEndpoint.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Settings;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.Settings.Endpoints.Settings;

public class GetSettingsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/",
                async (ISettingsContracts settings, SettingScope? scope) =>
                {
                    var filter = scope is not null ? new SettingsFilter { Scope = scope } : null;
                    var results = await settings.GetSettingsAsync(filter);
                    return Results.Ok(results);
                }
            )
            .RequireAuthorization();
}
```

```csharp
// modules/Settings/src/Settings/Endpoints/Settings/GetSettingEndpoint.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Settings;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.Settings.Endpoints.Settings;

public class GetSettingEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/{key}",
                async (string key, SettingScope scope, ISettingsContracts settings) =>
                {
                    var value = await settings.GetSettingAsync(key, scope);
                    return value is not null
                        ? Results.Ok(new { key, value, scope })
                        : Results.NotFound();
                }
            )
            .RequireAuthorization();
}
```

```csharp
// modules/Settings/src/Settings/Endpoints/Settings/UpdateSettingEndpoint.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.Settings.Endpoints.Settings;

public class UpdateSettingEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut(
                "/",
                async (UpdateSettingRequest request, ISettingsContracts settings) =>
                {
                    await settings.SetSettingAsync(
                        request.Key,
                        request.Value ?? string.Empty,
                        request.Scope
                    );
                    return Results.NoContent();
                }
            )
            .RequireAuthorization();
}
```

```csharp
// modules/Settings/src/Settings/Endpoints/Settings/DeleteSettingEndpoint.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Settings;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.Settings.Endpoints.Settings;

public class DeleteSettingEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(
                "/{key}",
                async (string key, SettingScope scope, ISettingsContracts settings) =>
                {
                    await settings.DeleteSettingAsync(key, scope);
                    return Results.NoContent();
                }
            )
            .RequireAuthorization();
}
```

```csharp
// modules/Settings/src/Settings/Endpoints/Settings/GetDefinitionsEndpoint.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Settings;

namespace SimpleModule.Settings.Endpoints.Settings;

public class GetDefinitionsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/definitions",
                (ISettingsDefinitionRegistry registry, SettingScope? scope) =>
                    Results.Ok(registry.GetDefinitions(scope))
            )
            .RequireAuthorization();
}
```

**Step 2: Create user settings endpoints**

```csharp
// modules/Settings/src/Settings/Endpoints/UserSettings/GetMySettingsEndpoint.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Settings;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.Settings.Endpoints.UserSettings;

public class GetMySettingsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/me",
                async (
                    ISettingsContracts settings,
                    ISettingsDefinitionRegistry registry,
                    ClaimsPrincipal principal
                ) =>
                {
                    var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (string.IsNullOrEmpty(userId))
                        return Results.Unauthorized();

                    var definitions = registry.GetDefinitions(SettingScope.User);
                    var results = new List<object>();

                    foreach (var def in definitions)
                    {
                        var resolved = await settings.ResolveUserSettingAsync(def.Key, userId);
                        var userValue = await settings.GetSettingAsync(
                            def.Key,
                            SettingScope.User,
                            userId
                        );
                        results.Add(
                            new
                            {
                                definition = def,
                                value = resolved,
                                isOverridden = userValue is not null,
                            }
                        );
                    }

                    return Results.Ok(results);
                }
            )
            .RequireAuthorization();
}
```

```csharp
// modules/Settings/src/Settings/Endpoints/UserSettings/UpdateMySettingEndpoint.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Settings;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.Settings.Endpoints.UserSettings;

public class UpdateMySettingEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut(
                "/me",
                async (
                    UpdateSettingRequest request,
                    ISettingsContracts settings,
                    ClaimsPrincipal principal
                ) =>
                {
                    var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (string.IsNullOrEmpty(userId))
                        return Results.Unauthorized();

                    await settings.SetSettingAsync(
                        request.Key,
                        request.Value ?? string.Empty,
                        SettingScope.User,
                        userId
                    );
                    return Results.NoContent();
                }
            )
            .RequireAuthorization();
}
```

**Step 3: Verify build**

Run: `dotnet build`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add modules/Settings/src/Settings/Endpoints/
git commit -m "feat(settings): add admin and user settings API endpoints"
```

---

### Task 6: Create View Endpoints

**Files:**
- Create: `modules/Settings/src/Settings/Views/AdminSettingsEndpoint.cs`
- Create: `modules/Settings/src/Settings/Views/UserSettingsEndpoint.cs`

**Step 1: Create admin view endpoint**

```csharp
// modules/Settings/src/Settings/Views/AdminSettingsEndpoint.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Core.Settings;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.Settings.Views;

public class AdminSettingsEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/",
                async (ISettingsContracts settings, ISettingsDefinitionRegistry registry) =>
                {
                    var definitions = registry.GetDefinitions();
                    var storedSettings = await settings.GetSettingsAsync();
                    return Inertia.Render(
                        "Settings/AdminSettings",
                        new { definitions, settings = storedSettings }
                    );
                }
            )
            .RequireAuthorization();
    }
}
```

**Step 2: Create user view endpoint**

```csharp
// modules/Settings/src/Settings/Views/UserSettingsEndpoint.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Core.Settings;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.Settings.Views;

public class UserSettingsEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/me",
                async (
                    ISettingsContracts settings,
                    ISettingsDefinitionRegistry registry,
                    ClaimsPrincipal principal
                ) =>
                {
                    var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
                    var definitions = registry.GetDefinitions(SettingScope.User);

                    var userSettings = new List<object>();
                    foreach (var def in definitions)
                    {
                        var resolved = await settings.ResolveUserSettingAsync(
                            def.Key,
                            userId ?? string.Empty
                        );
                        var userValue = await settings.GetSettingAsync(
                            def.Key,
                            SettingScope.User,
                            userId
                        );
                        userSettings.Add(
                            new
                            {
                                definition = def,
                                value = resolved,
                                isOverridden = userValue is not null,
                            }
                        );
                    }

                    return Inertia.Render("Settings/UserSettings", new { settings = userSettings });
                }
            )
            .RequireAuthorization();
    }
}
```

**Step 3: Verify build**

Run: `dotnet build`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add modules/Settings/src/Settings/Views/
git commit -m "feat(settings): add Inertia view endpoints for admin and user settings pages"
```

---

### Task 7: Create Frontend Pages

**Files:**
- Create: `modules/Settings/src/Settings/package.json`
- Create: `modules/Settings/src/Settings/vite.config.ts`
- Create: `modules/Settings/src/Settings/Pages/index.ts`
- Create: `modules/Settings/src/Settings/Views/AdminSettings.tsx`
- Create: `modules/Settings/src/Settings/Views/UserSettings.tsx`
- Create: `modules/Settings/src/Settings/components/SettingField.tsx`
- Create: `modules/Settings/src/Settings/components/SettingGroup.tsx`

**Step 1: Create package.json**

```json
{
  "private": true,
  "name": "@simplemodule/settings",
  "version": "0.0.0",
  "scripts": {
    "build": "vite build",
    "watch": "vite build --watch"
  },
  "peerDependencies": {
    "react": "^19.0.0",
    "react-dom": "^19.0.0"
  }
}
```

**Step 2: Create vite.config.ts**

```typescript
import { resolve } from 'node:path';
import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';

export default defineConfig({
  plugins: [react()],
  define: { 'process.env.NODE_ENV': JSON.stringify('production') },
  build: {
    lib: {
      entry: resolve(__dirname, 'Pages/index.ts'),
      formats: ['es'],
      fileName: () => 'Settings.pages.js',
    },
    outDir: 'wwwroot',
    emptyOutDir: false,
    rollupOptions: {
      external: ['react', 'react-dom', 'react/jsx-runtime', '@inertiajs/react'],
    },
  },
});
```

**Step 3: Create Pages/index.ts**

```typescript
export const pages: Record<string, any> = {
  'Settings/AdminSettings': () => import('../Views/AdminSettings'),
  'Settings/UserSettings': () => import('../Views/UserSettings'),
};
```

**Step 4: Create SettingField component**

```typescript
// modules/Settings/src/Settings/components/SettingField.tsx
import { useState } from 'react';
import { Button } from '@simplemodule/ui/components/button';
import { Input } from '@simplemodule/ui/components/input';
import { Switch } from '@simplemodule/ui/components/switch';
import { Textarea } from '@simplemodule/ui/components/textarea';

interface SettingDefinition {
  key: string;
  displayName: string;
  description?: string;
  group?: string;
  scope: number;
  defaultValue?: string;
  type: number;
}

interface SettingFieldProps {
  definition: SettingDefinition;
  currentValue?: string | null;
  onSave: (key: string, value: string, scope: number) => Promise<void>;
}

export default function SettingField({ definition, currentValue, onSave }: SettingFieldProps) {
  const initial = currentValue ?? definition.defaultValue ?? '';
  const [value, setValue] = useState(initial);
  const [saving, setSaving] = useState(false);
  const hasChanged = value !== initial;

  const handleSave = async () => {
    setSaving(true);
    try {
      await onSave(definition.key, value, definition.scope);
    } finally {
      setSaving(false);
    }
  };

  const renderInput = () => {
    switch (definition.type) {
      case 0: // String
        return (
          <Input
            value={value}
            onChange={(e) => setValue(e.target.value)}
          />
        );
      case 1: // Number
        return (
          <Input
            type="number"
            value={value}
            onChange={(e) => setValue(e.target.value)}
          />
        );
      case 2: // Boolean
        return (
          <Switch
            checked={value === 'true'}
            onCheckedChange={(checked) => {
              const newVal = String(checked);
              setValue(newVal);
              onSave(definition.key, newVal, definition.scope);
            }}
          />
        );
      case 3: // Json
        return (
          <Textarea
            value={value}
            onChange={(e) => setValue(e.target.value)}
            rows={4}
            className="font-mono text-sm"
          />
        );
      default:
        return null;
    }
  };

  return (
    <div className="space-y-2">
      {definition.description && (
        <p className="text-sm text-muted-foreground">{definition.description}</p>
      )}
      {renderInput()}
      {definition.type !== 2 && hasChanged && (
        <Button size="sm" onClick={handleSave} disabled={saving}>
          {saving ? 'Saving...' : 'Save'}
        </Button>
      )}
    </div>
  );
}

export type { SettingDefinition };
```

**Step 5: Create SettingGroup component**

```typescript
// modules/Settings/src/Settings/components/SettingGroup.tsx
import { Card, CardContent, CardHeader, CardTitle } from '@simplemodule/ui/components/card';
import SettingField from './SettingField';
import type { SettingDefinition } from './SettingField';

interface SettingGroupProps {
  group: string;
  definitions: SettingDefinition[];
  values: Record<string, string | null>;
  onSave: (key: string, value: string, scope: number) => Promise<void>;
}

export default function SettingGroup({ group, definitions, values, onSave }: SettingGroupProps) {
  return (
    <Card>
      <CardHeader>
        <CardTitle>{group}</CardTitle>
      </CardHeader>
      <CardContent className="space-y-6">
        {definitions.map((def) => (
          <div key={def.key}>
            <label className="text-sm font-medium">{def.displayName}</label>
            <SettingField
              definition={def}
              currentValue={values[def.key]}
              onSave={onSave}
            />
          </div>
        ))}
      </CardContent>
    </Card>
  );
}
```

**Step 6: Create AdminSettings page**

```typescript
// modules/Settings/src/Settings/Views/AdminSettings.tsx
import { useState, useMemo } from 'react';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@simplemodule/ui/components/tabs';
import SettingGroup from '../components/SettingGroup';
import type { SettingDefinition } from '../components/SettingField';

interface Setting {
  key: string;
  value: string | null;
  scope: number;
}

interface AdminSettingsProps {
  definitions: SettingDefinition[];
  settings: Setting[];
}

export default function AdminSettings({ definitions, settings }: AdminSettingsProps) {
  const [settingsMap, setSettingsMap] = useState<Record<string, string | null>>(() => {
    const map: Record<string, string | null> = {};
    for (const s of settings) {
      map[s.key] = s.value;
    }
    return map;
  });

  const systemDefs = useMemo(
    () => definitions.filter((d) => d.scope === 0),
    [definitions],
  );
  const appDefs = useMemo(
    () => definitions.filter((d) => d.scope === 1),
    [definitions],
  );

  const groupBy = (defs: SettingDefinition[]) => {
    const groups: Record<string, SettingDefinition[]> = {};
    for (const def of defs) {
      const group = def.group ?? 'General';
      if (!groups[group]) groups[group] = [];
      groups[group].push(def);
    }
    return groups;
  };

  const handleSave = async (key: string, value: string, scope: number) => {
    await fetch('/api/settings', {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ key, value, scope }),
    });
    setSettingsMap((prev) => ({ ...prev, [key]: value }));
  };

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <h1 className="text-2xl font-bold tracking-tight">Settings</h1>
      <Tabs defaultValue="system">
        <TabsList>
          <TabsTrigger value="system">System</TabsTrigger>
          <TabsTrigger value="application">Application</TabsTrigger>
        </TabsList>
        <TabsContent value="system" className="space-y-4">
          {Object.entries(groupBy(systemDefs)).map(([group, defs]) => (
            <SettingGroup
              key={group}
              group={group}
              definitions={defs}
              values={settingsMap}
              onSave={handleSave}
            />
          ))}
        </TabsContent>
        <TabsContent value="application" className="space-y-4">
          {Object.entries(groupBy(appDefs)).map(([group, defs]) => (
            <SettingGroup
              key={group}
              group={group}
              definitions={defs}
              values={settingsMap}
              onSave={handleSave}
            />
          ))}
        </TabsContent>
      </Tabs>
    </div>
  );
}
```

**Step 7: Create UserSettings page**

```typescript
// modules/Settings/src/Settings/Views/UserSettings.tsx
import { useState } from 'react';
import { Button } from '@simplemodule/ui/components/button';
import { Card, CardContent, CardHeader, CardTitle } from '@simplemodule/ui/components/card';
import { Badge } from '@simplemodule/ui/components/badge';
import SettingField from '../components/SettingField';
import type { SettingDefinition } from '../components/SettingField';

interface UserSettingView {
  definition: SettingDefinition;
  value: string | null;
  isOverridden: boolean;
}

interface UserSettingsProps {
  settings: UserSettingView[];
}

export default function UserSettings({ settings: initial }: UserSettingsProps) {
  const [settings, setSettings] = useState(initial);

  const handleSave = async (key: string, value: string) => {
    await fetch('/api/settings/me', {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ key, value, scope: 2 }),
    });
    setSettings((prev) =>
      prev.map((s) =>
        s.definition.key === key ? { ...s, value, isOverridden: true } : s,
      ),
    );
  };

  const handleReset = async (key: string) => {
    await fetch(`/api/settings/${key}?scope=2`, { method: 'DELETE' });
    setSettings((prev) =>
      prev.map((s) =>
        s.definition.key === key
          ? { ...s, isOverridden: false, value: s.definition.defaultValue ?? null }
          : s,
      ),
    );
  };

  const groups: Record<string, UserSettingView[]> = {};
  for (const s of settings) {
    const group = s.definition.group ?? 'General';
    if (!groups[group]) groups[group] = [];
    groups[group].push(s);
  }

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <h1 className="text-2xl font-bold tracking-tight">My Settings</h1>
      {Object.entries(groups).map(([group, items]) => (
        <Card key={group}>
          <CardHeader>
            <CardTitle>{group}</CardTitle>
          </CardHeader>
          <CardContent className="space-y-6">
            {items.map((s) => (
              <div key={s.definition.key} className="space-y-1">
                <div className="flex items-center justify-between">
                  <label className="text-sm font-medium">{s.definition.displayName}</label>
                  <div className="flex items-center gap-2">
                    {s.isOverridden ? (
                      <>
                        <Badge variant="secondary">Overridden</Badge>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleReset(s.definition.key)}
                        >
                          Reset
                        </Button>
                      </>
                    ) : (
                      <Badge variant="outline">Default</Badge>
                    )}
                  </div>
                </div>
                <SettingField
                  definition={s.definition}
                  currentValue={s.value}
                  onSave={handleSave}
                />
              </div>
            ))}
          </CardContent>
        </Card>
      ))}
    </div>
  );
}
```

**Step 8: Install dependencies and build**

Run: `npm install`
Run: `npx vite build` from `modules/Settings/src/Settings/`

**Step 9: Commit**

```bash
git add modules/Settings/src/Settings/package.json modules/Settings/src/Settings/vite.config.ts modules/Settings/src/Settings/Pages/ modules/Settings/src/Settings/Views/*.tsx modules/Settings/src/Settings/components/
git commit -m "feat(settings): add React frontend pages for admin and user settings"
```

---

### Task 8: Create Test Project and Integration Tests

**Files:**
- Create: `modules/Settings/tests/Settings.Tests/Settings.Tests.csproj`
- Create: `modules/Settings/tests/Settings.Tests/Integration/SettingsEndpointTests.cs`
- Create: `modules/Settings/tests/Settings.Tests/Unit/SettingsServiceTests.cs`

**Step 1: Create test project**

```xml
<!-- modules/Settings/tests/Settings.Tests/Settings.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="xunit.v3" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Settings\Settings.csproj" />
    <ProjectReference Include="..\..\src\Settings.Contracts\Settings.Contracts.csproj" />
    <ProjectReference Include="..\..\..\..\tests\SimpleModule.Tests.Shared\SimpleModule.Tests.Shared.csproj" />
  </ItemGroup>
</Project>
```

**Step 2: Write unit tests for SettingsService cascade resolution**

```csharp
// modules/Settings/tests/Settings.Tests/Unit/SettingsServiceTests.cs
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SimpleModule.Core.Settings;
using SimpleModule.Database;
using SimpleModule.Settings.Contracts;
using SimpleModule.Settings.Entities;

namespace Settings.Tests.Unit;

public class SettingsServiceTests : IDisposable
{
    private readonly SettingsDbContext _db;
    private readonly SettingsService _service;

    public SettingsServiceTests()
    {
        var options = new DbContextOptionsBuilder<SettingsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var dbOptions = Options.Create(new DatabaseOptions());
        _db = new SettingsDbContext(options, dbOptions);
        _db.Database.EnsureCreated();

        var registry = new SettingsDefinitionRegistry(
        [
            new SettingDefinition
            {
                Key = "theme",
                DisplayName = "Theme",
                Scope = SettingScope.User,
                DefaultValue = "\"light\"",
                Type = SettingType.String,
            },
        ]);

        _service = new SettingsService(
            _db,
            registry,
            NullLogger<SettingsService>.Instance
        );
    }

    [Fact]
    public async Task ResolveUserSettingAsync_ReturnsUserValue_WhenSet()
    {
        await _service.SetSettingAsync("theme", "\"dark\"", SettingScope.User, "user1");
        await _service.SetSettingAsync("theme", "\"system-default\"", SettingScope.Application);

        var result = await _service.ResolveUserSettingAsync("theme", "user1");

        result.Should().Be("\"dark\"");
    }

    [Fact]
    public async Task ResolveUserSettingAsync_FallsBackToApp_WhenNoUserValue()
    {
        await _service.SetSettingAsync("theme", "\"corporate\"", SettingScope.Application);

        var result = await _service.ResolveUserSettingAsync("theme", "user1");

        result.Should().Be("\"corporate\"");
    }

    [Fact]
    public async Task ResolveUserSettingAsync_FallsBackToCodeDefault_WhenNothingSet()
    {
        var result = await _service.ResolveUserSettingAsync("theme", "user1");

        result.Should().Be("\"light\"");
    }

    [Fact]
    public async Task SetSettingAsync_Upserts_WhenKeyAlreadyExists()
    {
        await _service.SetSettingAsync("theme", "\"dark\"", SettingScope.Application);
        await _service.SetSettingAsync("theme", "\"blue\"", SettingScope.Application);

        var value = await _service.GetSettingAsync("theme", SettingScope.Application);
        value.Should().Be("\"blue\"");

        var count = await _db.Settings.CountAsync(s =>
            s.Key == "theme" && s.Scope == SettingScope.Application
        );
        count.Should().Be(1);
    }

    [Fact]
    public async Task DeleteSettingAsync_RemovesSetting()
    {
        await _service.SetSettingAsync("theme", "\"dark\"", SettingScope.User, "user1");
        await _service.DeleteSettingAsync("theme", SettingScope.User, "user1");

        var value = await _service.GetSettingAsync("theme", SettingScope.User, "user1");
        value.Should().BeNull();
    }

    [Fact]
    public async Task GetSettingAsync_Generic_DeserializesCorrectly()
    {
        await _service.SetSettingAsync("count", "42", SettingScope.Application);

        var result = await _service.GetSettingAsync<int>("count", SettingScope.Application);
        result.Should().Be(42);
    }

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }
}
```

**Step 3: Write integration tests**

```csharp
// modules/Settings/tests/Settings.Tests/Integration/SettingsEndpointTests.cs
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SimpleModule.Core.Settings;
using SimpleModule.Settings.Contracts;
using SimpleModule.Tests.Shared.Fixtures;

namespace Settings.Tests.Integration;

public class SettingsEndpointTests(SimpleModuleWebApplicationFactory factory)
    : IClassFixture<SimpleModuleWebApplicationFactory>
{
    [Fact]
    public async Task GetDefinitions_Authenticated_Returns200()
    {
        var client = factory.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/settings/definitions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateSetting_Authenticated_Returns204()
    {
        var client = factory.CreateAuthenticatedClient();
        var request = new UpdateSettingRequest
        {
            Key = "test.key",
            Value = "\"test-value\"",
            Scope = SettingScope.Application,
        };

        var response = await client.PutAsJsonAsync("/api/settings", request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetSetting_AfterUpdate_ReturnsValue()
    {
        var client = factory.CreateAuthenticatedClient();
        await client.PutAsJsonAsync(
            "/api/settings",
            new UpdateSettingRequest
            {
                Key = "integration.test",
                Value = "\"hello\"",
                Scope = SettingScope.Application,
            }
        );

        var response = await client.GetAsync(
            "/api/settings/integration.test?scope=1"
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteSetting_Authenticated_Returns204()
    {
        var client = factory.CreateAuthenticatedClient();
        await client.PutAsJsonAsync(
            "/api/settings",
            new UpdateSettingRequest
            {
                Key = "delete.test",
                Value = "\"temp\"",
                Scope = SettingScope.Application,
            }
        );

        var response = await client.DeleteAsync(
            "/api/settings/delete.test?scope=1"
        );

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetMySettings_Authenticated_Returns200()
    {
        var client = factory.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/settings/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateMySetting_Authenticated_Returns204()
    {
        var client = factory.CreateAuthenticatedClient();
        var request = new UpdateSettingRequest
        {
            Key = "app.theme",
            Value = "\"dark\"",
            Scope = SettingScope.User,
        };

        var response = await client.PutAsJsonAsync("/api/settings/me", request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetSettings_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/settings");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

**Step 4: Add test project to solution**

Add to `SimpleModule.slnx` under `/modules/Settings/`:
```xml
<Project Path="modules/Settings/tests/Settings.Tests/Settings.Tests.csproj" />
```

**Step 5: Run all tests**

Run: `dotnet test modules/Settings/tests/Settings.Tests -v minimal`
Expected: All tests PASS

**Step 6: Commit**

```bash
git add modules/Settings/tests/ SimpleModule.slnx
git commit -m "feat(settings): add unit and integration tests for Settings module"
```

---

### Task 9: UI Tests (Playwright)

**Files:**
- Create: `modules/Settings/tests/Settings.Tests/UI/AdminSettingsUiTests.cs`
- Create: `modules/Settings/tests/Settings.Tests/UI/UserSettingsUiTests.cs`

**Step 1: Write admin settings UI tests**

Follow the existing Playwright test patterns in the project. Tests should:
- Navigate to `/settings` as admin
- Verify tabs (System, Application) are rendered
- Edit a setting value and save
- Verify the saved value persists after page reload
- Verify grouped display renders setting groups

**Step 2: Write user settings UI tests**

Tests should:
- Navigate to `/settings/me` as authenticated user
- Override a setting value
- Verify "Overridden" badge appears
- Click "Reset" and verify it returns to "Default" badge
- Verify type-specific inputs render correctly (toggle for boolean, text for string)

**Step 3: Write permission UI tests**

- Verify unauthenticated user is redirected from `/settings`
- Verify regular user can access `/settings/me`

**Step 4: Run UI tests**

Run: `dotnet test modules/Settings/tests/Settings.Tests --filter "FullyQualifiedName~UI" -v minimal`
Expected: All UI tests PASS

**Step 5: Commit**

```bash
git add modules/Settings/tests/Settings.Tests/UI/
git commit -m "test(settings): add Playwright UI tests for admin and user settings pages"
```

---

### Task 10: Final Verification and Cleanup

**Step 1: Run full build**

Run: `dotnet build`
Expected: Build succeeded with no warnings

**Step 2: Run all tests**

Run: `dotnet test`
Expected: All tests PASS

**Step 3: Run frontend lint**

Run: `npm run check`
Expected: No lint errors

**Step 4: Run the application**

Run: `dotnet run --project template/SimpleModule.Host`
Navigate to `https://localhost:5001/settings` — verify admin page loads
Navigate to `https://localhost:5001/settings/me` — verify user page loads

**Step 5: Final commit if any cleanup was needed**

```bash
git add -A
git commit -m "chore(settings): final cleanup and verification"
```
