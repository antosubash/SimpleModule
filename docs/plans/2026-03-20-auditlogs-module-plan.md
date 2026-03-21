# AuditLogs Module Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build a comprehensive audit logging module with three automatic capture streams (HTTP middleware, EventBus decorator, EF SaveChanges interceptor) that records all system activity with zero configuration from other modules.

**Architecture:** Dual-stream + interceptor design with `Channel<AuditEntry>` for async, non-blocking writes. A background `AuditWriterService` batch-inserts entries. All streams share a scoped `CorrelationId` to link related entries. Settings control what is captured at runtime.

**Tech Stack:** .NET 10, EF Core (SQLite/PostgreSQL), Vogen (strongly-typed IDs), System.Threading.Channels, React 19 + Inertia.js, @simplemodule/ui DataGrid, xUnit + FluentAssertions.

**Design doc:** `docs/plans/2026-03-20-auditlogs-module-design.md`

---

## Task 1: Core — Add `PagedResult<T>` to SimpleModule.Core

**Files:**
- Create: `framework/SimpleModule.Core/PagedResult.cs`
- Modify: No existing files modified

**Step 1: Create PagedResult<T>**

```csharp
// framework/SimpleModule.Core/PagedResult.cs
using SimpleModule.Core.TypeGeneration;

namespace SimpleModule.Core;

[Dto]
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
```

**Step 2: Verify build**

Run: `dotnet build framework/SimpleModule.Core/SimpleModule.Core.csproj`
Expected: Build succeeded

**Step 3: Commit**

```
feat(core): add PagedResult<T> for paginated query responses
```

---

## Task 2: Contracts — Create AuditLogs.Contracts project

**Files:**
- Create: `modules/AuditLogs/src/AuditLogs.Contracts/AuditLogs.Contracts.csproj`
- Create: `modules/AuditLogs/src/AuditLogs.Contracts/AuditEntryId.cs`
- Create: `modules/AuditLogs/src/AuditLogs.Contracts/AuditSource.cs`
- Create: `modules/AuditLogs/src/AuditLogs.Contracts/AuditAction.cs`
- Create: `modules/AuditLogs/src/AuditLogs.Contracts/AuditEntry.cs`
- Create: `modules/AuditLogs/src/AuditLogs.Contracts/AuditQueryRequest.cs`
- Create: `modules/AuditLogs/src/AuditLogs.Contracts/AuditExportRequest.cs`
- Create: `modules/AuditLogs/src/AuditLogs.Contracts/AuditStats.cs`
- Create: `modules/AuditLogs/src/AuditLogs.Contracts/IAuditLogContracts.cs`
- Create: `modules/AuditLogs/src/AuditLogs.Contracts/IAuditContext.cs`

**Step 1: Create the .csproj**

```xml
<!-- modules/AuditLogs/src/AuditLogs.Contracts/AuditLogs.Contracts.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Library</OutputType>
    <DefineConstants>$(DefineConstants);VOGEN_NO_VALIDATION</DefineConstants>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" />
    <PackageReference Include="Vogen" />
    <ProjectReference Include="..\..\..\..\framework\SimpleModule.Core\SimpleModule.Core.csproj" />
  </ItemGroup>
</Project>
```

**Step 2: Create AuditEntryId (Vogen strongly-typed ID)**

```csharp
// modules/AuditLogs/src/AuditLogs.Contracts/AuditEntryId.cs
using Vogen;

namespace SimpleModule.AuditLogs.Contracts;

[ValueObject<int>(conversions: Conversions.SystemTextJson | Conversions.EfCoreValueConverter)]
public readonly partial struct AuditEntryId;
```

**Step 3: Create AuditSource enum**

```csharp
// modules/AuditLogs/src/AuditLogs.Contracts/AuditSource.cs
namespace SimpleModule.AuditLogs.Contracts;

public enum AuditSource
{
    Http,
    Domain,
    ChangeTracker,
}
```

**Step 4: Create AuditAction enum**

```csharp
// modules/AuditLogs/src/AuditLogs.Contracts/AuditAction.cs
namespace SimpleModule.AuditLogs.Contracts;

public enum AuditAction
{
    Created,
    Updated,
    Deleted,
    Viewed,
    LoginSuccess,
    LoginFailed,
    PermissionGranted,
    PermissionRevoked,
    SettingChanged,
    Exported,
    Other,
}
```

**Step 5: Create AuditEntry DTO**

```csharp
// modules/AuditLogs/src/AuditLogs.Contracts/AuditEntry.cs
using SimpleModule.Core.TypeGeneration;

namespace SimpleModule.AuditLogs.Contracts;

[Dto]
public class AuditEntry
{
    public AuditEntryId Id { get; set; }
    public Guid CorrelationId { get; set; }
    public AuditSource Source { get; set; }
    public DateTimeOffset Timestamp { get; set; }

    // User context
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    // HTTP context
    public string? HttpMethod { get; set; }
    public string? Path { get; set; }
    public string? QueryString { get; set; }
    public int? StatusCode { get; set; }
    public long? DurationMs { get; set; }
    public string? RequestBody { get; set; }

    // Domain context
    public string? Module { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public AuditAction? Action { get; set; }
    public string? Changes { get; set; }
    public string? Metadata { get; set; }
}
```

**Step 6: Create AuditQueryRequest**

```csharp
// modules/AuditLogs/src/AuditLogs.Contracts/AuditQueryRequest.cs
using SimpleModule.Core.TypeGeneration;

namespace SimpleModule.AuditLogs.Contracts;

[Dto]
public class AuditQueryRequest
{
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public string? UserId { get; set; }
    public string? Module { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public AuditSource? Source { get; set; }
    public AuditAction? Action { get; set; }
    public int? StatusCode { get; set; }
    public string? SearchText { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string SortBy { get; set; } = "Timestamp";
    public bool SortDescending { get; set; } = true;
}
```

**Step 7: Create AuditExportRequest**

```csharp
// modules/AuditLogs/src/AuditLogs.Contracts/AuditExportRequest.cs
using SimpleModule.Core.TypeGeneration;

namespace SimpleModule.AuditLogs.Contracts;

[Dto]
public class AuditExportRequest : AuditQueryRequest
{
    public string Format { get; set; } = "csv";
}
```

**Step 8: Create AuditStats**

```csharp
// modules/AuditLogs/src/AuditLogs.Contracts/AuditStats.cs
using SimpleModule.Core.TypeGeneration;

namespace SimpleModule.AuditLogs.Contracts;

[Dto]
public class AuditStats
{
    public int TotalEntries { get; set; }
    public int UniqueUsers { get; set; }
    public Dictionary<string, int> ByModule { get; set; } = new();
    public Dictionary<string, int> ByAction { get; set; } = new();
    public Dictionary<string, int> ByStatusCode { get; set; } = new();
}
```

**Step 9: Create IAuditLogContracts**

```csharp
// modules/AuditLogs/src/AuditLogs.Contracts/IAuditLogContracts.cs
using SimpleModule.Core;

namespace SimpleModule.AuditLogs.Contracts;

public interface IAuditLogContracts
{
    Task<PagedResult<AuditEntry>> QueryAsync(AuditQueryRequest request);
    Task<AuditEntry?> GetByIdAsync(AuditEntryId id);
    Task<IReadOnlyList<AuditEntry>> GetByCorrelationIdAsync(Guid correlationId);
    Task<Stream> ExportAsync(AuditExportRequest request);
    Task<AuditStats> GetStatsAsync(DateTimeOffset from, DateTimeOffset to);
    Task WriteBatchAsync(IReadOnlyList<AuditEntry> entries);
    Task<int> PurgeOlderThanAsync(DateTimeOffset cutoff);
}
```

**Step 10: Create IAuditContext**

```csharp
// modules/AuditLogs/src/AuditLogs.Contracts/IAuditContext.cs
namespace SimpleModule.AuditLogs.Contracts;

public interface IAuditContext
{
    Guid CorrelationId { get; }
    string? UserId { get; set; }
    string? UserName { get; set; }
    string? IpAddress { get; set; }
}
```

**Step 11: Verify build**

Run: `dotnet build modules/AuditLogs/src/AuditLogs.Contracts/AuditLogs.Contracts.csproj`
Expected: Build succeeded

**Step 12: Commit**

```
feat(auditlogs): add AuditLogs.Contracts project with DTOs and interfaces
```

---

## Task 3: Module Shell — Create AuditLogs project with module class, DbContext, constants, permissions

**Files:**
- Create: `modules/AuditLogs/src/AuditLogs/AuditLogs.csproj`
- Create: `modules/AuditLogs/src/AuditLogs/AuditLogsConstants.cs`
- Create: `modules/AuditLogs/src/AuditLogs/AuditLogsPermissions.cs`
- Create: `modules/AuditLogs/src/AuditLogs/AuditLogsDbContext.cs`
- Create: `modules/AuditLogs/src/AuditLogs/EntityConfigurations/AuditEntryConfiguration.cs`
- Create: `modules/AuditLogs/src/AuditLogs/AuditLogsModule.cs`
- Modify: `SimpleModule.slnx` — add AuditLogs folder with 3 projects
- Modify: `template/SimpleModule.Host/SimpleModule.Host.csproj` — add project reference

**Step 1: Create the .csproj**

```xml
<!-- modules/AuditLogs/src/AuditLogs/AuditLogs.csproj -->
<Project Sdk="Microsoft.NET.Sdk.Razor">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <ProjectReference Include="..\..\..\..\framework\SimpleModule.Core\SimpleModule.Core.csproj" />
    <ProjectReference Include="..\..\..\..\framework\SimpleModule.Database\SimpleModule.Database.csproj" />
    <ProjectReference Include="..\AuditLogs.Contracts\AuditLogs.Contracts.csproj" />
    <ProjectReference Include="..\..\..\..\modules\Settings\src\Settings.Contracts\Settings.Contracts.csproj" />
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

**Step 2: Create constants**

```csharp
// modules/AuditLogs/src/AuditLogs/AuditLogsConstants.cs
namespace SimpleModule.AuditLogs;

public static class AuditLogsConstants
{
    public const string ModuleName = "AuditLogs";
    public const string RoutePrefix = "/api/audit-logs";
}
```

**Step 3: Create permissions**

```csharp
// modules/AuditLogs/src/AuditLogs/AuditLogsPermissions.cs
namespace SimpleModule.AuditLogs;

public sealed class AuditLogsPermissions
{
    public const string View = "AuditLogs.View";
    public const string Export = "AuditLogs.Export";
}
```

**Step 4: Create EF entity configuration with indexes**

```csharp
// modules/AuditLogs/src/AuditLogs/EntityConfigurations/AuditEntryConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleModule.AuditLogs.Contracts;

namespace SimpleModule.AuditLogs.EntityConfigurations;

public class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        builder.Property(e => e.Timestamp).IsRequired();
        builder.Property(e => e.Source).IsRequired();
        builder.Property(e => e.CorrelationId).IsRequired();

        builder.Property(e => e.HttpMethod).HasMaxLength(10);
        builder.Property(e => e.Path).HasMaxLength(2048);
        builder.Property(e => e.IpAddress).HasMaxLength(45);
        builder.Property(e => e.UserId).HasMaxLength(256);
        builder.Property(e => e.UserName).HasMaxLength(256);
        builder.Property(e => e.Module).HasMaxLength(128);
        builder.Property(e => e.EntityType).HasMaxLength(256);
        builder.Property(e => e.EntityId).HasMaxLength(256);

        // Performance indexes
        builder.HasIndex(e => new { e.Timestamp }).IsDescending(true);
        builder.HasIndex(e => new { e.UserId, e.Timestamp }).IsDescending(false, true);
        builder.HasIndex(e => new { e.Module, e.Timestamp }).IsDescending(false, true);
        builder.HasIndex(e => e.CorrelationId);
        builder.HasIndex(e => new { e.EntityType, e.EntityId });
    }
}
```

**Step 5: Create DbContext**

```csharp
// modules/AuditLogs/src/AuditLogs/AuditLogsDbContext.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimpleModule.AuditLogs.Contracts;
using SimpleModule.AuditLogs.EntityConfigurations;
using SimpleModule.Database;

namespace SimpleModule.AuditLogs;

public class AuditLogsDbContext(
    DbContextOptions<AuditLogsDbContext> options,
    IOptions<DatabaseOptions> dbOptions
) : DbContext(options)
{
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AuditEntryConfiguration());
        modelBuilder.ApplyModuleSchema(AuditLogsConstants.ModuleName, dbOptions.Value);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<AuditEntryId>()
            .HaveConversion<AuditEntryId.EfCoreValueConverter, AuditEntryId.EfCoreValueComparer>();
    }
}
```

**Step 6: Create module class (shell — services will be added in later tasks)**

```csharp
// modules/AuditLogs/src/AuditLogs/AuditLogsModule.cs
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Menu;
using SimpleModule.Core.Settings;
using SimpleModule.Database;
using SimpleModule.AuditLogs.Contracts;

namespace SimpleModule.AuditLogs;

[Module(
    AuditLogsConstants.ModuleName,
    RoutePrefix = AuditLogsConstants.RoutePrefix,
    ViewPrefix = "/audit-logs"
)]
public class AuditLogsModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<AuditLogsDbContext>(configuration, AuditLogsConstants.ModuleName);
        services.AddScoped<IAuditLogContracts, AuditLogService>();
        services.AddScoped<IAuditContext, AuditContext>();
    }

    public void ConfigurePermissions(PermissionRegistryBuilder builder)
    {
        builder.AddPermissions<AuditLogsPermissions>();
    }

    public void ConfigureMenu(IMenuBuilder menus)
    {
        menus.Add(
            new MenuItem
            {
                Label = "Audit Logs",
                Url = "/audit-logs/browse",
                Icon = """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" d="M9 12h3.75M9 15h3.75M9 18h3.75m3 .75H18a2.25 2.25 0 002.25-2.25V6.108c0-1.135-.845-2.098-1.976-2.192a48.424 48.424 0 00-1.123-.08m-5.801 0c-.065.21-.1.433-.1.664 0 .414.336.75.75.75h4.5a.75.75 0 00.75-.75 2.25 2.25 0 00-.1-.664m-5.8 0A2.251 2.251 0 0113.5 2.25H15c1.012 0 1.867.668 2.15 1.586m-5.8 0c-.376.023-.75.05-1.124.08C9.095 4.01 8.25 4.973 8.25 6.108V8.25m0 0H4.875c-.621 0-1.125.504-1.125 1.125v11.25c0 .621.504 1.125 1.125 1.125h9.75c.621 0 1.125-.504 1.125-1.125V9.375c0-.621-.504-1.125-1.125-1.125H8.25zM6.75 12h.008v.008H6.75V12zm0 3h.008v.008H6.75V15zm0 3h.008v.008H6.75V18z"/></svg>""",
                Order = 95,
                Section = MenuSection.AdminSidebar,
            }
        );
    }

    public void ConfigureSettings(ISettingsBuilder settings)
    {
        settings
            .Add(new SettingDefinition
            {
                Key = "auditlogs.capture.http",
                DisplayName = "HTTP Request Capture",
                Description = "Capture all HTTP requests in audit log",
                Group = "Audit Logs",
                Scope = SettingScope.System,
                DefaultValue = "true",
                Type = SettingType.Bool,
            })
            .Add(new SettingDefinition
            {
                Key = "auditlogs.capture.domain",
                DisplayName = "Domain Event Capture",
                Description = "Capture domain events from the event bus",
                Group = "Audit Logs",
                Scope = SettingScope.System,
                DefaultValue = "true",
                Type = SettingType.Bool,
            })
            .Add(new SettingDefinition
            {
                Key = "auditlogs.capture.changes",
                DisplayName = "Entity Change Tracking",
                Description = "Capture EF Core SaveChanges with property diffs",
                Group = "Audit Logs",
                Scope = SettingScope.System,
                DefaultValue = "true",
                Type = SettingType.Bool,
            })
            .Add(new SettingDefinition
            {
                Key = "auditlogs.capture.requestbodies",
                DisplayName = "Request Body Capture",
                Description = "Store request bodies for POST/PUT/DELETE (redacted)",
                Group = "Audit Logs",
                Scope = SettingScope.System,
                DefaultValue = "true",
                Type = SettingType.Bool,
            })
            .Add(new SettingDefinition
            {
                Key = "auditlogs.capture.querystrings",
                DisplayName = "Query String Capture",
                Description = "Store URL query strings",
                Group = "Audit Logs",
                Scope = SettingScope.System,
                DefaultValue = "true",
                Type = SettingType.Bool,
            })
            .Add(new SettingDefinition
            {
                Key = "auditlogs.capture.useragent",
                DisplayName = "User Agent Capture",
                Description = "Store browser/client user agent strings",
                Group = "Audit Logs",
                Scope = SettingScope.System,
                DefaultValue = "false",
                Type = SettingType.Bool,
            })
            .Add(new SettingDefinition
            {
                Key = "auditlogs.retention.enabled",
                DisplayName = "Auto-Cleanup",
                Description = "Automatically delete old audit entries",
                Group = "Audit Logs",
                Scope = SettingScope.System,
                DefaultValue = "true",
                Type = SettingType.Bool,
            })
            .Add(new SettingDefinition
            {
                Key = "auditlogs.retention.days",
                DisplayName = "Retention Days",
                Description = "Number of days to keep audit entries",
                Group = "Audit Logs",
                Scope = SettingScope.System,
                DefaultValue = "90",
                Type = SettingType.Number,
            })
            .Add(new SettingDefinition
            {
                Key = "auditlogs.excluded.paths",
                DisplayName = "Excluded Paths",
                Description = "Comma-separated path prefixes to skip (e.g., /health,/metrics)",
                Group = "Audit Logs",
                Scope = SettingScope.System,
                DefaultValue = "\"/health,/metrics,/_content,/js/,/css/\"",
                Type = SettingType.Text,
            });
    }
}
```

**Step 7: Add to solution and host project**

Modify `SimpleModule.slnx` — add after the Settings folder:

```xml
<Folder Name="/modules/AuditLogs/">
    <Project Path="modules/AuditLogs/src/AuditLogs.Contracts/AuditLogs.Contracts.csproj" />
    <Project Path="modules/AuditLogs/src/AuditLogs/AuditLogs.csproj" />
    <Project Path="modules/AuditLogs/tests/AuditLogs.Tests/AuditLogs.Tests.csproj" />
</Folder>
```

Modify `template/SimpleModule.Host/SimpleModule.Host.csproj` — add project reference alongside other modules:

```xml
<ProjectReference Include="..\..\modules\AuditLogs\src\AuditLogs\AuditLogs.csproj" />
```

**Step 8: Create stub AuditLogService and AuditContext (to satisfy DI registration — real implementation in later tasks)**

```csharp
// modules/AuditLogs/src/AuditLogs/AuditLogService.cs
using SimpleModule.AuditLogs.Contracts;
using SimpleModule.Core;

namespace SimpleModule.AuditLogs;

public class AuditLogService(AuditLogsDbContext db) : IAuditLogContracts
{
    public Task<PagedResult<AuditEntry>> QueryAsync(AuditQueryRequest request) =>
        throw new NotImplementedException();

    public Task<AuditEntry?> GetByIdAsync(AuditEntryId id) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<AuditEntry>> GetByCorrelationIdAsync(Guid correlationId) =>
        throw new NotImplementedException();

    public Task<Stream> ExportAsync(AuditExportRequest request) =>
        throw new NotImplementedException();

    public Task<AuditStats> GetStatsAsync(DateTimeOffset from, DateTimeOffset to) =>
        throw new NotImplementedException();

    public Task WriteBatchAsync(IReadOnlyList<AuditEntry> entries) =>
        throw new NotImplementedException();

    public Task<int> PurgeOlderThanAsync(DateTimeOffset cutoff) =>
        throw new NotImplementedException();
}
```

```csharp
// modules/AuditLogs/src/AuditLogs/AuditContext.cs
using SimpleModule.AuditLogs.Contracts;

namespace SimpleModule.AuditLogs;

public class AuditContext : IAuditContext
{
    public Guid CorrelationId { get; } = Guid.NewGuid();
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? IpAddress { get; set; }
}
```

**Step 9: Verify build**

Run: `dotnet build`
Expected: Full solution builds successfully

**Step 10: Commit**

```
feat(auditlogs): add AuditLogs module shell with DbContext, settings, permissions, and menu
```

---

## Task 4: Pipeline — AuditChannel and AuditWriterService

**Files:**
- Create: `modules/AuditLogs/src/AuditLogs/Pipeline/AuditChannel.cs`
- Create: `modules/AuditLogs/src/AuditLogs/Pipeline/AuditWriterService.cs`
- Modify: `modules/AuditLogs/src/AuditLogs/AuditLogsModule.cs` — register pipeline services

**Step 1: Create AuditChannel (Channel<AuditEntry> wrapper)**

```csharp
// modules/AuditLogs/src/AuditLogs/Pipeline/AuditChannel.cs
using System.Threading.Channels;
using SimpleModule.AuditLogs.Contracts;

namespace SimpleModule.AuditLogs.Pipeline;

public sealed class AuditChannel
{
    private readonly Channel<AuditEntry> _channel = Channel.CreateUnbounded<AuditEntry>(
        new UnboundedChannelOptions { SingleReader = true }
    );

    public ChannelReader<AuditEntry> Reader => _channel.Reader;

    public void Enqueue(AuditEntry entry)
    {
        _channel.Writer.TryWrite(entry);
    }
}
```

**Step 2: Create AuditWriterService (background batch writer)**

```csharp
// modules/AuditLogs/src/AuditLogs/Pipeline/AuditWriterService.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SimpleModule.AuditLogs.Contracts;

namespace SimpleModule.AuditLogs.Pipeline;

public sealed partial class AuditWriterService(
    AuditChannel channel,
    IServiceScopeFactory scopeFactory,
    ILogger<AuditWriterService> logger
) : BackgroundService
{
    private const int BatchSize = 100;
    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogStarted(logger);
        var batch = new List<AuditEntry>(BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Wait for at least one entry
                if (await channel.Reader.WaitToReadAsync(stoppingToken))
                {
                    batch.Clear();
                    var deadline = DateTimeOffset.UtcNow.Add(FlushInterval);

                    // Read up to BatchSize or until FlushInterval elapses
                    while (batch.Count < BatchSize
                        && DateTimeOffset.UtcNow < deadline
                        && channel.Reader.TryRead(out var entry))
                    {
                        batch.Add(entry);
                    }

                    if (batch.Count > 0)
                    {
                        await FlushBatchAsync(batch, stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
#pragma warning disable CA1031 // Catch general exception — background service must not crash
            catch (Exception ex)
#pragma warning restore CA1031
            {
                LogFlushError(logger, ex);
                // Wait a bit before retrying to avoid tight error loops
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        // Drain remaining entries on shutdown
        batch.Clear();
        while (channel.Reader.TryRead(out var entry))
        {
            batch.Add(entry);
        }
        if (batch.Count > 0)
        {
            await FlushBatchAsync(batch, CancellationToken.None);
        }

        LogStopped(logger);
    }

    private async Task FlushBatchAsync(List<AuditEntry> batch, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var contracts = scope.ServiceProvider.GetRequiredService<IAuditLogContracts>();
        await contracts.WriteBatchAsync(batch);
        LogFlushed(logger, batch.Count);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "AuditWriterService started")]
    private static partial void LogStarted(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "AuditWriterService stopped")]
    private static partial void LogStopped(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Flushed {Count} audit entries")]
    private static partial void LogFlushed(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error flushing audit entries")]
    private static partial void LogFlushError(ILogger logger, Exception ex);
}
```

**Step 3: Register pipeline services in AuditLogsModule.ConfigureServices**

Add to `ConfigureServices` in `AuditLogsModule.cs`:

```csharp
// After existing registrations
services.AddSingleton<AuditChannel>();
services.AddHostedService<AuditWriterService>();
```

**Step 4: Verify build**

Run: `dotnet build`
Expected: Build succeeded

**Step 5: Commit**

```
feat(auditlogs): add Channel-based async audit pipeline with batch writer
```

---

## Task 5: Sensitive Field Redactor

**Files:**
- Create: `modules/AuditLogs/src/AuditLogs/Enrichment/SensitiveFieldRedactor.cs`

**Step 1: Create the redactor**

```csharp
// modules/AuditLogs/src/AuditLogs/Enrichment/SensitiveFieldRedactor.cs
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace SimpleModule.AuditLogs.Enrichment;

public static partial class SensitiveFieldRedactor
{
    private const string Redacted = "[REDACTED]";

    [GeneratedRegex(
        @"password|secret|token|key|authorization|credential|ssn|credit.?card|cvv",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    )]
    private static partial Regex SensitivePattern();

    public static string? Redact(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            var node = JsonNode.Parse(json);
            if (node is null)
                return null;

            RedactNode(node);
            return node.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void RedactNode(JsonNode node)
    {
        if (node is JsonObject obj)
        {
            var keys = obj.Select(p => p.Key).ToList();
            foreach (var key in keys)
            {
                if (SensitivePattern().IsMatch(key))
                {
                    obj[key] = Redacted;
                }
                else if (obj[key] is JsonNode child)
                {
                    RedactNode(child);
                }
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var item in array)
            {
                if (item is not null)
                {
                    RedactNode(item);
                }
            }
        }
    }
}
```

**Step 2: Verify build**

Run: `dotnet build modules/AuditLogs/src/AuditLogs/AuditLogs.csproj`
Expected: Build succeeded

**Step 3: Commit**

```
feat(auditlogs): add sensitive field redactor for request body sanitization
```

---

## Task 6: HTTP Audit Middleware

**Files:**
- Create: `modules/AuditLogs/src/AuditLogs/Middleware/AuditMiddleware.cs`
- Modify: `modules/AuditLogs/src/AuditLogs/AuditLogsModule.cs` — add `ConfigureEndpoints` to register middleware

**Step 1: Create AuditMiddleware**

```csharp
// modules/AuditLogs/src/AuditLogs/Middleware/AuditMiddleware.cs
using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.AuditLogs.Contracts;
using SimpleModule.AuditLogs.Enrichment;
using SimpleModule.AuditLogs.Pipeline;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.AuditLogs.Middleware;

public sealed class AuditMiddleware(RequestDelegate next)
{
    private static readonly string[] ExcludedMethodsForBody = ["GET", "HEAD", "OPTIONS"];

    public async Task InvokeAsync(HttpContext context)
    {
        var channel = context.RequestServices.GetService<AuditChannel>();
        if (channel is null)
        {
            await next(context);
            return;
        }

        // Check if HTTP capture is enabled
        var settings = context.RequestServices.GetService<ISettingsContracts>();
        if (settings is not null)
        {
            var enabled = await settings.GetSettingAsync<bool>(
                "auditlogs.capture.http",
                Core.Settings.SettingScope.System
            );
            if (enabled == false)
            {
                await next(context);
                return;
            }
        }

        // Check excluded paths
        var path = context.Request.Path.Value ?? "";
        if (IsExcludedPath(path, settings))
        {
            await next(context);
            return;
        }

        // Populate audit context with user info
        var auditContext = context.RequestServices.GetService<IAuditContext>();
        if (auditContext is not null)
        {
            auditContext.UserId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            auditContext.UserName = context.User.FindFirstValue(ClaimTypes.Name);
            auditContext.IpAddress = context.Connection.RemoteIpAddress?.ToString();
        }

        // Read request body if applicable
        string? requestBody = null;
        var captureBody = settings is null
            || await settings.GetSettingAsync<bool>(
                "auditlogs.capture.requestbodies",
                Core.Settings.SettingScope.System
            ) != false;

        if (captureBody && !ExcludedMethodsForBody.Contains(context.Request.Method, StringComparer.OrdinalIgnoreCase))
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
            requestBody = SensitiveFieldRedactor.Redact(requestBody);
        }

        var stopwatch = Stopwatch.StartNew();

        await next(context);

        stopwatch.Stop();

        // Capture query string setting
        string? queryString = null;
        var captureQs = settings is null
            || await settings.GetSettingAsync<bool>(
                "auditlogs.capture.querystrings",
                Core.Settings.SettingScope.System
            ) != false;
        if (captureQs)
        {
            queryString = context.Request.QueryString.Value;
        }

        // Capture user agent setting
        string? userAgent = null;
        if (settings is not null)
        {
            var captureUa = await settings.GetSettingAsync<bool>(
                "auditlogs.capture.useragent",
                Core.Settings.SettingScope.System
            );
            if (captureUa == true)
            {
                userAgent = context.Request.Headers.UserAgent.ToString();
            }
        }

        var entry = new AuditEntry
        {
            CorrelationId = auditContext?.CorrelationId ?? Guid.NewGuid(),
            Source = AuditSource.Http,
            Timestamp = DateTimeOffset.UtcNow,
            UserId = auditContext?.UserId ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier),
            UserName = auditContext?.UserName ?? context.User.FindFirstValue(ClaimTypes.Name),
            IpAddress = auditContext?.IpAddress ?? context.Connection.RemoteIpAddress?.ToString(),
            UserAgent = userAgent,
            HttpMethod = context.Request.Method,
            Path = path,
            QueryString = queryString,
            StatusCode = context.Response.StatusCode,
            DurationMs = stopwatch.ElapsedMilliseconds,
            RequestBody = requestBody,
        };

        channel.Enqueue(entry);
    }

    private static bool IsExcludedPath(string path, ISettingsContracts? settings)
    {
        // Default exclusions — settings-based exclusion is checked synchronously via cached values
        // In the future this could read from settings, but for now use compile-time defaults
        // to avoid async in a hot-path check
        ReadOnlySpan<string> defaults = ["/health", "/metrics", "/_content", "/js/", "/css/", "/favicon"];
        foreach (var prefix in defaults)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
```

**Step 2: Register middleware via ConfigureEndpoints on the module**

The middleware needs to be registered in `Program.cs` or via the module. Since `IModule.ConfigureEndpoints` gives access to `IEndpointRouteBuilder` (not `IApplicationBuilder`), we'll register the middleware in `Program.cs` by having the module provide a static method, or better — add it to `ConfigureServices` as a hosted startup filter.

Actually, the cleanest approach: modify `Program.cs` to add the middleware. Add this line in `template/SimpleModule.Host/Program.cs` after `app.UseAuthorization();` (line 200):

```csharp
// Audit logging middleware — captures all HTTP requests
app.UseMiddleware<SimpleModule.AuditLogs.Middleware.AuditMiddleware>();
```

**Step 3: Verify build**

Run: `dotnet build`
Expected: Build succeeded

**Step 4: Commit**

```
feat(auditlogs): add HTTP audit middleware with settings-gated capture
```

---

## Task 7: EventBus Decorator (AuditingEventBus)

**Files:**
- Create: `modules/AuditLogs/src/AuditLogs/Pipeline/AuditingEventBus.cs`
- Modify: `modules/AuditLogs/src/AuditLogs/AuditLogsModule.cs` — decorate IEventBus registration

**Step 1: Create AuditingEventBus**

```csharp
// modules/AuditLogs/src/AuditLogs/Pipeline/AuditingEventBus.cs
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using SimpleModule.AuditLogs.Contracts;
using SimpleModule.Core.Events;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.AuditLogs.Pipeline;

public sealed partial class AuditingEventBus(
    IEventBus inner,
    IAuditContext auditContext,
    AuditChannel channel,
    ISettingsContracts? settings = null
) : IEventBus
{
    [GeneratedRegex(
        @"^(?<entity>.+?)(?<action>Created|Updated|Deleted|Viewed|Exported|LoginSuccess|LoginFailed|PermissionGranted|PermissionRevoked|SettingChanged)Event$"
    )]
    private static partial Regex EventNamePattern();

    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default)
        where T : IEvent
    {
        // Check if domain capture is enabled
        var enabled = settings is null
            || await settings.GetSettingAsync<bool>(
                "auditlogs.capture.domain",
                Core.Settings.SettingScope.System
            ) != false;

        if (enabled)
        {
            var entry = ExtractAuditEntry(@event);
            channel.Enqueue(entry);
        }

        await inner.PublishAsync(@event, cancellationToken);
    }

    private AuditEntry ExtractAuditEntry<T>(T @event)
        where T : IEvent
    {
        var typeName = typeof(T).Name;
        var match = EventNamePattern().Match(typeName);

        string? module = null;
        AuditAction action = AuditAction.Other;
        string? entityType = null;
        string? entityId = null;
        Dictionary<string, object?>? metadata = null;

        if (match.Success)
        {
            entityType = match.Groups["entity"].Value;
            module = entityType;
            if (Enum.TryParse<AuditAction>(match.Groups["action"].Value, out var parsed))
            {
                action = parsed;
            }
        }

        // Extract properties via reflection
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in properties)
        {
            var value = prop.GetValue(@event);
            if (prop.Name.EndsWith("Id", StringComparison.Ordinal) && value is not null)
            {
                // First ID property becomes EntityId
                entityId ??= value.ToString();
                // Derive entity type from ID property name (e.g., "ProductId" → "Product")
                if (entityType is null && prop.Name.Length > 2)
                {
                    entityType = prop.Name[..^2];
                    module ??= entityType;
                }
            }
            else if (value is not null)
            {
                metadata ??= [];
                metadata[prop.Name] = value;
            }
        }

        return new AuditEntry
        {
            CorrelationId = auditContext.CorrelationId,
            Source = AuditSource.Domain,
            Timestamp = DateTimeOffset.UtcNow,
            UserId = auditContext.UserId,
            UserName = auditContext.UserName,
            IpAddress = auditContext.IpAddress,
            Module = module,
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            Metadata = metadata is not null
                ? JsonSerializer.Serialize(metadata)
                : null,
        };
    }
}
```

**Step 2: Register as decorator in AuditLogsModule.ConfigureServices**

The EventBus is registered in `Program.cs` as `AddScoped<IEventBus, EventBus>()`. We need to decorate it. Add to `ConfigureServices`:

```csharp
// Decorate IEventBus with auditing wrapper
services.Decorate<IEventBus, AuditingEventBus>();
```

Note: This requires `Scrutor` or manual decoration. Since the project may not use Scrutor, we'll use manual decoration instead. Modify `ConfigureServices`:

```csharp
// Decorate IEventBus with auditing
services.AddScoped<IEventBus>(sp =>
{
    // Resolve the underlying EventBus directly to avoid circular resolution
    var innerBus = ActivatorUtilities.CreateInstance<Core.Events.EventBus>(sp);
    var auditContext = sp.GetRequiredService<IAuditContext>();
    var auditChannel = sp.GetRequiredService<AuditChannel>();
    var settingsContracts = sp.GetService<ISettingsContracts>();
    return new AuditingEventBus(innerBus, auditContext, auditChannel, settingsContracts);
});
```

And remove the `AddScoped<IEventBus, EventBus>()` from `Program.cs` since the module now handles it — or better, keep `Program.cs` unchanged and override in the module's registration. The module's `ConfigureServices` runs after `Program.cs` registrations, so a new `AddScoped<IEventBus>` will replace the previous one.

**Step 3: Verify build**

Run: `dotnet build`
Expected: Build succeeded

**Step 4: Commit**

```
feat(auditlogs): add EventBus decorator for automatic domain event auditing
```

---

## Task 8: SaveChanges Interceptor

**Files:**
- Create: `modules/AuditLogs/src/AuditLogs/Interceptors/AuditSaveChangesInterceptor.cs`
- Modify: `framework/SimpleModule.Database/ModuleDbContextOptionsBuilder.cs` — register interceptor

**Step 1: Create interceptor**

```csharp
// modules/AuditLogs/src/AuditLogs/Interceptors/AuditSaveChangesInterceptor.cs
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SimpleModule.AuditLogs.Contracts;
using SimpleModule.AuditLogs.Pipeline;

namespace SimpleModule.AuditLogs.Interceptors;

public sealed class AuditSaveChangesInterceptor(
    IAuditContext auditContext,
    AuditChannel channel
) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    )
    {
        if (eventData.Context is null)
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        var contextType = eventData.Context.GetType();

        // Don't audit our own DbContext to avoid infinite loops
        if (contextType == typeof(AuditLogsDbContext))
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        // Derive module name from DbContext type name (e.g., "ProductsDbContext" → "Products")
        var moduleName = contextType.Name.Replace("DbContext", "", StringComparison.Ordinal);

        foreach (var entry in eventData.Context.ChangeTracker.Entries())
        {
            if (entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            {
                var auditEntry = CreateAuditEntry(entry, moduleName);
                if (auditEntry is not null)
                {
                    channel.Enqueue(auditEntry);
                }
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private AuditEntry? CreateAuditEntry(EntityEntry entry, string moduleName)
    {
        var entityType = entry.Metadata.ClrType.Name;
        var action = entry.State switch
        {
            EntityState.Added => AuditAction.Created,
            EntityState.Modified => AuditAction.Updated,
            EntityState.Deleted => AuditAction.Deleted,
            _ => (AuditAction?)null,
        };

        if (action is null)
            return null;

        string? entityId = null;
        var primaryKey = entry.Metadata.FindPrimaryKey();
        if (primaryKey is not null)
        {
            var keyValues = primaryKey.Properties
                .Select(p => entry.CurrentValues[p]?.ToString())
                .Where(v => v is not null);
            entityId = string.Join(",", keyValues);
        }

        string? changes = null;
        if (entry.State == EntityState.Modified)
        {
            var changedProps = entry.Properties
                .Where(p => p.IsModified)
                .Select(p => new
                {
                    field = p.Metadata.Name,
                    old = p.OriginalValue?.ToString(),
                    @new = p.CurrentValue?.ToString(),
                })
                .ToList();

            if (changedProps.Count > 0)
            {
                changes = JsonSerializer.Serialize(changedProps);
            }
        }
        else if (entry.State == EntityState.Added)
        {
            var props = entry.Properties
                .Where(p => p.CurrentValue is not null)
                .Select(p => new
                {
                    field = p.Metadata.Name,
                    value = p.CurrentValue?.ToString(),
                })
                .ToList();

            if (props.Count > 0)
            {
                changes = JsonSerializer.Serialize(props);
            }
        }

        return new AuditEntry
        {
            CorrelationId = auditContext.CorrelationId,
            Source = AuditSource.ChangeTracker,
            Timestamp = DateTimeOffset.UtcNow,
            UserId = auditContext.UserId,
            UserName = auditContext.UserName,
            IpAddress = auditContext.IpAddress,
            Module = moduleName,
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            Changes = changes,
        };
    }
}
```

**Step 2: Register interceptor in AddModuleDbContext**

Modify `framework/SimpleModule.Database/ModuleDbContextOptionsBuilder.cs` — add interceptor resolution inside the `AddDbContext<TContext>` options lambda. The interceptor is optional (resolved from DI), so it won't fail if the AuditLogs module isn't referenced:

```csharp
// Inside the AddDbContext lambda, after configureOptions?.Invoke(options):
// Add audit interceptor if registered
var sp = services.BuildServiceProvider();
var interceptors = sp.GetServices<ISaveChangesInterceptor>();
foreach (var interceptor in interceptors)
{
    options.AddInterceptors(interceptor);
}
```

Actually, a better approach to avoid `BuildServiceProvider()` — register the interceptor in `AuditLogsModule.ConfigureServices` and use `DbContextOptionsBuilder.AddInterceptors` by registering a factory. The cleanest pattern: register `AuditSaveChangesInterceptor` as a scoped service, then use `IDbContextOptionsExtension` or `IDbContextOptionsConfiguration`.

Simplest approach: Register the interceptor as a service, and have `AddModuleDbContext` use `AddInterceptors` with a provider-based approach.

Modify `ModuleDbContextOptionsBuilder.cs` to accept interceptors from DI:

After `configureOptions?.Invoke(options);` add:

```csharp
// Register interceptors from DI — allows modules like AuditLogs to add SaveChangesInterceptor
options.AddInterceptors(
    services.BuildServiceProvider().GetServices<ISaveChangesInterceptor>().ToArray()
);
```

Note: This `BuildServiceProvider()` call is a known pattern for infrastructure bootstrapping and happens only at startup, not per-request. However, if this causes issues, an alternative is to register the interceptor in each module's `AddModuleDbContext` call via the `configureOptions` callback.

**Alternative (preferred):** Instead of modifying Core infrastructure, register from `AuditLogsModule.ConfigureServices`:

```csharp
services.AddScoped<AuditSaveChangesInterceptor>();
```

And modify `ModuleDbContextOptionsBuilder.cs` line 33 to:

```csharp
services.AddDbContext<TContext>((sp, options) =>
```

Then after the switch statement and `configureOptions?.Invoke(options)`:

```csharp
var interceptors = sp.GetServices<ISaveChangesInterceptor>();
if (interceptors.Any())
{
    options.AddInterceptors(interceptors.ToArray());
}
```

This uses the `(IServiceProvider, DbContextOptionsBuilder)` overload so interceptors from DI are automatically wired up.

**Step 3: Verify build**

Run: `dotnet build`
Expected: Build succeeded

**Step 4: Commit**

```
feat(auditlogs): add SaveChanges interceptor for automatic entity change auditing
```

---

## Task 9: Implement AuditLogService (query, write, export, stats, purge)

**Files:**
- Modify: `modules/AuditLogs/src/AuditLogs/AuditLogService.cs` — replace stubs with real implementation

**Step 1: Implement the full service**

```csharp
// modules/AuditLogs/src/AuditLogs/AuditLogService.cs
using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SimpleModule.AuditLogs.Contracts;
using SimpleModule.Core;

namespace SimpleModule.AuditLogs;

public sealed partial class AuditLogService(
    AuditLogsDbContext db,
    ILogger<AuditLogService> logger
) : IAuditLogContracts
{
    public async Task<PagedResult<AuditEntry>> QueryAsync(AuditQueryRequest request)
    {
        var query = BuildQuery(request);

        var totalCount = await query.CountAsync();

        // Apply sorting
        query = request.SortBy switch
        {
            "UserId" => request.SortDescending
                ? query.OrderByDescending(e => e.UserId)
                : query.OrderBy(e => e.UserId),
            "Module" => request.SortDescending
                ? query.OrderByDescending(e => e.Module)
                : query.OrderBy(e => e.Module),
            "Path" => request.SortDescending
                ? query.OrderByDescending(e => e.Path)
                : query.OrderBy(e => e.Path),
            "StatusCode" => request.SortDescending
                ? query.OrderByDescending(e => e.StatusCode)
                : query.OrderBy(e => e.StatusCode),
            "DurationMs" => request.SortDescending
                ? query.OrderByDescending(e => e.DurationMs)
                : query.OrderBy(e => e.DurationMs),
            _ => request.SortDescending
                ? query.OrderByDescending(e => e.Timestamp)
                : query.OrderBy(e => e.Timestamp),
        };

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .AsNoTracking()
            .ToListAsync();

        return new PagedResult<AuditEntry>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
        };
    }

    public async Task<AuditEntry?> GetByIdAsync(AuditEntryId id) =>
        await db.AuditEntries.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);

    public async Task<IReadOnlyList<AuditEntry>> GetByCorrelationIdAsync(Guid correlationId) =>
        await db.AuditEntries
            .Where(e => e.CorrelationId == correlationId)
            .OrderBy(e => e.Timestamp)
            .AsNoTracking()
            .ToListAsync();

    public async Task<Stream> ExportAsync(AuditExportRequest request)
    {
        var query = BuildQuery(request);
        var entries = await query.OrderByDescending(e => e.Timestamp).AsNoTracking().ToListAsync();

        return request.Format.Equals("json", StringComparison.OrdinalIgnoreCase)
            ? ExportAsJson(entries)
            : ExportAsCsv(entries);
    }

    public async Task<AuditStats> GetStatsAsync(DateTimeOffset from, DateTimeOffset to)
    {
        var entries = db.AuditEntries
            .Where(e => e.Timestamp >= from && e.Timestamp <= to)
            .AsNoTracking();

        var totalEntries = await entries.CountAsync();
        var uniqueUsers = await entries.Where(e => e.UserId != null).Select(e => e.UserId).Distinct().CountAsync();

        var byModule = await entries
            .Where(e => e.Module != null)
            .GroupBy(e => e.Module!)
            .Select(g => new { Module = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Module, x => x.Count);

        var byAction = await entries
            .Where(e => e.Action != null)
            .GroupBy(e => e.Action!.Value)
            .Select(g => new { Action = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Action.ToString(), x => x.Count);

        var byStatusCode = await entries
            .Where(e => e.StatusCode != null)
            .GroupBy(e => e.StatusCode!.Value)
            .Select(g => new { StatusCode = g.Key, Count = g.Count() })
            .ToDictionaryAsync(
                x => x.StatusCode.ToString(CultureInfo.InvariantCulture),
                x => x.Count
            );

        return new AuditStats
        {
            TotalEntries = totalEntries,
            UniqueUsers = uniqueUsers,
            ByModule = byModule,
            ByAction = byAction,
            ByStatusCode = byStatusCode,
        };
    }

    public async Task WriteBatchAsync(IReadOnlyList<AuditEntry> entries)
    {
        db.AuditEntries.AddRange(entries);
        await db.SaveChangesAsync();
        LogBatchWritten(logger, entries.Count);
    }

    public async Task<int> PurgeOlderThanAsync(DateTimeOffset cutoff)
    {
        var count = await db.AuditEntries.Where(e => e.Timestamp < cutoff).ExecuteDeleteAsync();
        LogPurged(logger, count);
        return count;
    }

    private IQueryable<AuditEntry> BuildQuery(AuditQueryRequest request)
    {
        IQueryable<AuditEntry> query = db.AuditEntries;

        if (request.From.HasValue)
            query = query.Where(e => e.Timestamp >= request.From.Value);

        if (request.To.HasValue)
            query = query.Where(e => e.Timestamp <= request.To.Value);

        if (!string.IsNullOrWhiteSpace(request.UserId))
            query = query.Where(e => e.UserId == request.UserId);

        if (!string.IsNullOrWhiteSpace(request.Module))
            query = query.Where(e => e.Module == request.Module);

        if (!string.IsNullOrWhiteSpace(request.EntityType))
            query = query.Where(e => e.EntityType == request.EntityType);

        if (!string.IsNullOrWhiteSpace(request.EntityId))
            query = query.Where(e => e.EntityId == request.EntityId);

        if (request.Source.HasValue)
            query = query.Where(e => e.Source == request.Source.Value);

        if (request.Action.HasValue)
            query = query.Where(e => e.Action == request.Action.Value);

        if (request.StatusCode.HasValue)
            query = query.Where(e => e.StatusCode == request.StatusCode.Value);

        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            var search = request.SearchText;
            query = query.Where(e =>
                (e.Path != null && e.Path.Contains(search))
                || (e.Changes != null && e.Changes.Contains(search))
                || (e.Metadata != null && e.Metadata.Contains(search))
                || (e.UserName != null && e.UserName.Contains(search))
            );
        }

        return query;
    }

    private static MemoryStream ExportAsCsv(List<AuditEntry> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Timestamp,Source,UserId,UserName,HttpMethod,Path,StatusCode,DurationMs,Module,EntityType,EntityId,Action,Changes");
        foreach (var e in entries)
        {
            sb.Append(CultureInfo.InvariantCulture, $"{e.Timestamp:O},");
            sb.Append(CultureInfo.InvariantCulture, $"{e.Source},");
            sb.Append(CultureInfo.InvariantCulture, $"{CsvEscape(e.UserId)},");
            sb.Append(CultureInfo.InvariantCulture, $"{CsvEscape(e.UserName)},");
            sb.Append(CultureInfo.InvariantCulture, $"{e.HttpMethod},");
            sb.Append(CultureInfo.InvariantCulture, $"{CsvEscape(e.Path)},");
            sb.Append(CultureInfo.InvariantCulture, $"{e.StatusCode},");
            sb.Append(CultureInfo.InvariantCulture, $"{e.DurationMs},");
            sb.Append(CultureInfo.InvariantCulture, $"{CsvEscape(e.Module)},");
            sb.Append(CultureInfo.InvariantCulture, $"{CsvEscape(e.EntityType)},");
            sb.Append(CultureInfo.InvariantCulture, $"{CsvEscape(e.EntityId)},");
            sb.Append(CultureInfo.InvariantCulture, $"{e.Action},");
            sb.AppendLine(CsvEscape(e.Changes));
        }
        return new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString()));
    }

    private static MemoryStream ExportAsJson(List<AuditEntry> entries)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(entries, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });
        return new MemoryStream(json);
    }

    private static string CsvEscape(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";
        if (value.Contains('"', StringComparison.Ordinal) || value.Contains(',', StringComparison.Ordinal))
            return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        return value;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Wrote batch of {Count} audit entries")]
    private static partial void LogBatchWritten(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Purged {Count} old audit entries")]
    private static partial void LogPurged(ILogger logger, int count);
}
```

**Step 2: Verify build**

Run: `dotnet build modules/AuditLogs/src/AuditLogs/AuditLogs.csproj`
Expected: Build succeeded

**Step 3: Commit**

```
feat(auditlogs): implement AuditLogService with query, export, stats, and batch write
```

---

## Task 10: Retention Service

**Files:**
- Create: `modules/AuditLogs/src/AuditLogs/Retention/AuditRetentionService.cs`
- Modify: `modules/AuditLogs/src/AuditLogs/AuditLogsModule.cs` — register hosted service

**Step 1: Create AuditRetentionService**

```csharp
// modules/AuditLogs/src/AuditLogs/Retention/AuditRetentionService.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SimpleModule.AuditLogs.Contracts;
using SimpleModule.Core.Settings;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.AuditLogs.Retention;

public sealed partial class AuditRetentionService(
    IServiceScopeFactory scopeFactory,
    ILogger<AuditRetentionService> logger
) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Delay startup to let the application initialize
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
#pragma warning disable CA1031
            catch (Exception ex)
#pragma warning restore CA1031
            {
                LogError(logger, ex);
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task RunCleanupAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var settings = scope.ServiceProvider.GetService<ISettingsContracts>();
        var auditLogs = scope.ServiceProvider.GetRequiredService<IAuditLogContracts>();

        // Check if retention is enabled
        if (settings is not null)
        {
            var enabled = await settings.GetSettingAsync<bool>(
                "auditlogs.retention.enabled",
                SettingScope.System
            );
            if (enabled == false)
            {
                LogSkipped(logger);
                return;
            }
        }

        // Get retention days
        var retentionDays = 90;
        if (settings is not null)
        {
            var days = await settings.GetSettingAsync<int>(
                "auditlogs.retention.days",
                SettingScope.System
            );
            if (days > 0)
            {
                retentionDays = days;
            }
        }

        var cutoff = DateTimeOffset.UtcNow.AddDays(-retentionDays);
        var purged = await auditLogs.PurgeOlderThanAsync(cutoff);
        LogCompleted(logger, purged, retentionDays);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Retention cleanup purged {Count} entries older than {Days} days")]
    private static partial void LogCompleted(ILogger logger, int count, int days);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Retention cleanup skipped (disabled)")]
    private static partial void LogSkipped(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Retention cleanup failed")]
    private static partial void LogError(ILogger logger, Exception ex);
}
```

**Step 2: Register in ConfigureServices**

Add to `AuditLogsModule.ConfigureServices`:

```csharp
services.AddHostedService<AuditRetentionService>();
```

**Step 3: Verify build**

Run: `dotnet build`
Expected: Build succeeded

**Step 4: Commit**

```
feat(auditlogs): add configurable audit log retention cleanup service
```

---

## Task 11: API Endpoints

**Files:**
- Create: `modules/AuditLogs/src/AuditLogs/Endpoints/AuditLogs/GetAllEndpoint.cs`
- Create: `modules/AuditLogs/src/AuditLogs/Endpoints/AuditLogs/GetByIdEndpoint.cs`
- Create: `modules/AuditLogs/src/AuditLogs/Endpoints/AuditLogs/ExportEndpoint.cs`
- Create: `modules/AuditLogs/src/AuditLogs/Endpoints/AuditLogs/GetStatsEndpoint.cs`

**Step 1: Create GetAllEndpoint**

```csharp
// modules/AuditLogs/src/AuditLogs/Endpoints/AuditLogs/GetAllEndpoint.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.AuditLogs.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;

namespace SimpleModule.AuditLogs.Endpoints.AuditLogs;

public class GetAllEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/",
                async ([AsParameters] AuditQueryRequest request, IAuditLogContracts auditLogs) =>
                    TypedResults.Ok(await auditLogs.QueryAsync(request))
            )
            .RequirePermission(AuditLogsPermissions.View);
}
```

**Step 2: Create GetByIdEndpoint**

```csharp
// modules/AuditLogs/src/AuditLogs/Endpoints/AuditLogs/GetByIdEndpoint.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.AuditLogs.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;

namespace SimpleModule.AuditLogs.Endpoints.AuditLogs;

public class GetByIdEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/{id}",
                async (AuditEntryId id, IAuditLogContracts auditLogs) =>
                {
                    var entry = await auditLogs.GetByIdAsync(id);
                    return entry is not null ? TypedResults.Ok(entry) : TypedResults.NotFound();
                }
            )
            .RequirePermission(AuditLogsPermissions.View);
}
```

**Step 3: Create ExportEndpoint**

```csharp
// modules/AuditLogs/src/AuditLogs/Endpoints/AuditLogs/ExportEndpoint.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.AuditLogs.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;

namespace SimpleModule.AuditLogs.Endpoints.AuditLogs;

public class ExportEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/export",
                async ([AsParameters] AuditExportRequest request, IAuditLogContracts auditLogs) =>
                {
                    var stream = await auditLogs.ExportAsync(request);
                    var contentType = request.Format.Equals("json", StringComparison.OrdinalIgnoreCase)
                        ? "application/json"
                        : "text/csv";
                    var extension = request.Format.Equals("json", StringComparison.OrdinalIgnoreCase)
                        ? "json"
                        : "csv";
                    return TypedResults.File(
                        stream,
                        contentType,
                        $"audit-logs-{DateTimeOffset.UtcNow:yyyy-MM-dd}.{extension}"
                    );
                }
            )
            .RequirePermission(AuditLogsPermissions.Export);
}
```

**Step 4: Create GetStatsEndpoint**

```csharp
// modules/AuditLogs/src/AuditLogs/Endpoints/AuditLogs/GetStatsEndpoint.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.AuditLogs.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;

namespace SimpleModule.AuditLogs.Endpoints.AuditLogs;

public class GetStatsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/stats",
                async (DateTimeOffset from, DateTimeOffset to, IAuditLogContracts auditLogs) =>
                    TypedResults.Ok(await auditLogs.GetStatsAsync(from, to))
            )
            .RequirePermission(AuditLogsPermissions.View);
}
```

**Step 5: Verify build**

Run: `dotnet build`
Expected: Build succeeded

**Step 6: Commit**

```
feat(auditlogs): add API endpoints for query, detail, export, and stats
```

---

## Task 12: View Endpoints (Inertia SSR)

**Files:**
- Create: `modules/AuditLogs/src/AuditLogs/Views/BrowseEndpoint.cs`
- Create: `modules/AuditLogs/src/AuditLogs/Views/DetailEndpoint.cs`

**Step 1: Create BrowseEndpoint**

```csharp
// modules/AuditLogs/src/AuditLogs/Views/BrowseEndpoint.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.AuditLogs.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Inertia;

namespace SimpleModule.AuditLogs.Views;

public class BrowseEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/browse",
                async ([AsParameters] AuditQueryRequest request, IAuditLogContracts auditLogs) =>
                {
                    var result = await auditLogs.QueryAsync(request);
                    return Inertia.Render("AuditLogs/Browse", new { result, filters = request });
                }
            )
            .RequirePermission(AuditLogsPermissions.View);
}
```

**Step 2: Create DetailEndpoint**

```csharp
// modules/AuditLogs/src/AuditLogs/Views/DetailEndpoint.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.AuditLogs.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Inertia;

namespace SimpleModule.AuditLogs.Views;

public class DetailEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/{id}",
                async (AuditEntryId id, IAuditLogContracts auditLogs) =>
                {
                    var entry = await auditLogs.GetByIdAsync(id);
                    if (entry is null)
                        return Results.NotFound();

                    var correlated = await auditLogs.GetByCorrelationIdAsync(entry.CorrelationId);
                    return Inertia.Render("AuditLogs/Detail", new { entry, correlated });
                }
            )
            .RequirePermission(AuditLogsPermissions.View);
}
```

**Step 3: Verify build**

Run: `dotnet build`
Expected: Build succeeded

**Step 4: Commit**

```
feat(auditlogs): add Inertia view endpoints for Browse and Detail pages
```

---

## Task 13: Frontend — Package setup, types, Vite config, Pages registry

**Files:**
- Create: `modules/AuditLogs/src/AuditLogs/package.json`
- Create: `modules/AuditLogs/src/AuditLogs/vite.config.ts`
- Create: `modules/AuditLogs/src/AuditLogs/Pages/index.ts`
- Create: `modules/AuditLogs/src/AuditLogs/types.ts` (placeholder — generated by build)

**Step 1: Create package.json**

```json
{
  "private": true,
  "name": "@simplemodule/auditlogs",
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
// modules/AuditLogs/src/AuditLogs/vite.config.ts
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
      fileName: () => 'AuditLogs.pages.js',
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
// modules/AuditLogs/src/AuditLogs/Pages/index.ts
export const pages: Record<string, any> = {
  'AuditLogs/Browse': () => import('../Views/Browse'),
  'AuditLogs/Detail': () => import('../Views/Detail'),
};
```

**Step 4: Create placeholder types.ts**

```typescript
// modules/AuditLogs/src/AuditLogs/types.ts
// Auto-generated from [Dto] types — do not edit manually
// Will be populated by source generator on build

export interface AuditEntry {
  id: number;
  correlationId: string;
  source: number;
  timestamp: string;
  userId: string | null;
  userName: string | null;
  ipAddress: string | null;
  userAgent: string | null;
  httpMethod: string | null;
  path: string | null;
  queryString: string | null;
  statusCode: number | null;
  durationMs: number | null;
  requestBody: string | null;
  module: string | null;
  entityType: string | null;
  entityId: string | null;
  action: number | null;
  changes: string | null;
  metadata: string | null;
}

export interface AuditQueryRequest {
  from: string | null;
  to: string | null;
  userId: string | null;
  module: string | null;
  entityType: string | null;
  entityId: string | null;
  source: number | null;
  action: number | null;
  statusCode: number | null;
  searchText: string | null;
  page: number;
  pageSize: number;
  sortBy: string;
  sortDescending: boolean;
}

export interface AuditStats {
  totalEntries: number;
  uniqueUsers: number;
  byModule: Record<string, number>;
  byAction: Record<string, number>;
  byStatusCode: Record<string, number>;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}
```

**Step 5: Run npm install to register workspace**

Run: `npm install`
Expected: Workspace `@simplemodule/auditlogs` recognized

**Step 6: Commit**

```
feat(auditlogs): add frontend package setup with Vite config and type definitions
```

---

## Task 14: Frontend — Browse.tsx (main audit log table with filters)

**Files:**
- Create: `modules/AuditLogs/src/AuditLogs/Views/Browse.tsx`

**Step 1: Create Browse.tsx**

Build a full-featured audit log browse page with:
- DataGrid table with columns: Timestamp, Source, User, Action, Module, Path, Status, Duration
- Filter panel with date range inputs, Source/Action/Module dropdowns, text search
- Export buttons (CSV/JSON)
- Row click navigates to detail page
- Uses `@simplemodule/ui` components: DataGrid, Table, Button, Input, Select, PageHeader, Badge, Card
- Server-side pagination via Inertia router
- Color-coded badges for Source (Http=info, Domain=success, ChangeTracker=warning) and Status codes (2xx=success, 4xx=warning, 5xx=danger)

The component receives `{ result: PagedResult<AuditEntry>, filters: AuditQueryRequest }` as props from the BrowseEndpoint.

Implementation should follow the patterns in Products/Manage.tsx and Admin/Users.tsx.

**Step 2: Verify build**

Run: `cd modules/AuditLogs/src/AuditLogs && npx vite build`
Expected: Build succeeds, `wwwroot/AuditLogs.pages.js` generated

**Step 3: Commit**

```
feat(auditlogs): add Browse page with filterable DataGrid and export
```

---

## Task 15: Frontend — Detail.tsx (entry detail with correlated entries)

**Files:**
- Create: `modules/AuditLogs/src/AuditLogs/Views/Detail.tsx`

**Step 1: Create Detail.tsx**

Build a detail view page with:
- Full entry data in a structured card layout
- Request body displayed as formatted JSON (if present)
- Changes displayed as a table: Field | Old Value | New Value (parsed from JSON)
- Metadata displayed as formatted JSON (if present)
- Correlated entries listed in a table below
- Back button to return to Browse
- Breadcrumb: Audit Logs > Entry #id

The component receives `{ entry: AuditEntry, correlated: AuditEntry[] }` as props from DetailEndpoint.

Uses `@simplemodule/ui` components: Card, CardContent, CardHeader, CardTitle, Badge, Button, Table, PageHeader, Breadcrumb.

**Step 2: Verify build**

Run: `cd modules/AuditLogs/src/AuditLogs && npx vite build`
Expected: Build succeeds

**Step 3: Commit**

```
feat(auditlogs): add Detail page with correlated entries and change diffs
```

---

## Task 16: Tests — Unit tests for core services

**Files:**
- Create: `modules/AuditLogs/tests/AuditLogs.Tests/AuditLogs.Tests.csproj`
- Create: `modules/AuditLogs/tests/AuditLogs.Tests/Helpers/TestDbContextFactory.cs`
- Create: `modules/AuditLogs/tests/AuditLogs.Tests/Unit/AuditLogServiceTests.cs`
- Create: `modules/AuditLogs/tests/AuditLogs.Tests/Unit/SensitiveFieldRedactorTests.cs`
- Create: `modules/AuditLogs/tests/AuditLogs.Tests/Unit/AuditingEventBusTests.cs`
- Create: `modules/AuditLogs/tests/AuditLogs.Tests/Unit/AuditSaveChangesInterceptorTests.cs`

**Step 1: Create test .csproj**

```xml
<!-- modules/AuditLogs/tests/AuditLogs.Tests/AuditLogs.Tests.csproj -->
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
    <PackageReference Include="NSubstitute" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\AuditLogs\AuditLogs.csproj" />
    <ProjectReference Include="..\..\src\AuditLogs.Contracts\AuditLogs.Contracts.csproj" />
    <ProjectReference Include="..\..\..\..\tests\SimpleModule.Tests.Shared\SimpleModule.Tests.Shared.csproj" />
  </ItemGroup>
</Project>
```

**Step 2: Create TestDbContextFactory**

```csharp
// modules/AuditLogs/tests/AuditLogs.Tests/Helpers/TestDbContextFactory.cs
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimpleModule.Database;

namespace SimpleModule.AuditLogs.Tests.Helpers;

public sealed class TestDbContextFactory : IDisposable
{
    private readonly SqliteConnection _connection;

    public TestDbContextFactory()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    public AuditLogsDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AuditLogsDbContext>()
            .UseSqlite(_connection)
            .Options;

        var dbOptions = Options.Create(new DatabaseOptions());
        var context = new AuditLogsDbContext(options, dbOptions);
        context.Database.EnsureCreated();
        return context;
    }

    public void Dispose() => _connection.Dispose();
}
```

**Step 3: Create tests for SensitiveFieldRedactor**

Test cases:
- Redacts password fields
- Redacts nested sensitive fields
- Handles arrays
- Returns null for invalid JSON
- Returns null for null/empty input
- Leaves non-sensitive fields intact

**Step 4: Create tests for AuditLogService**

Test cases:
- QueryAsync returns filtered results
- QueryAsync paginates correctly
- GetByIdAsync returns entry
- GetByIdAsync returns null for unknown ID
- GetByCorrelationIdAsync returns correlated entries
- WriteBatchAsync persists entries
- PurgeOlderThanAsync deletes old entries and returns count

**Step 5: Create tests for AuditingEventBus**

Test cases:
- Extracts module and action from event name convention
- Extracts entity ID from properties
- Delegates to inner event bus
- Enqueues audit entry to channel
- Respects domain capture setting when disabled

**Step 6: Create tests for AuditSaveChangesInterceptor**

Test cases:
- Captures Added entity with all property values
- Captures Modified entity with old/new diffs
- Captures Deleted entity
- Skips AuditLogsDbContext (avoids infinite loop)
- Derives module name from DbContext type

**Step 7: Run all tests**

Run: `dotnet test modules/AuditLogs/tests/AuditLogs.Tests/`
Expected: All tests pass

**Step 8: Commit**

```
test(auditlogs): add unit tests for service, redactor, event bus, and interceptor
```

---

## Task 17: Integration — Wire everything together and verify

**Files:**
- Modify: `modules/AuditLogs/src/AuditLogs/AuditLogsModule.cs` — ensure all registrations are complete
- Modify: `template/SimpleModule.Host/Program.cs` — add middleware
- Modify: `tests/SimpleModule.Tests.Shared/SimpleModule.Tests.Shared.csproj` — add AuditLogs references

**Step 1: Verify final AuditLogsModule.ConfigureServices has all registrations**

Ensure it includes:
- `AddModuleDbContext<AuditLogsDbContext>`
- `AddScoped<IAuditLogContracts, AuditLogService>`
- `AddScoped<IAuditContext, AuditContext>`
- `AddSingleton<AuditChannel>`
- `AddHostedService<AuditWriterService>`
- `AddHostedService<AuditRetentionService>`
- `AddScoped<AuditSaveChangesInterceptor>` (for interceptor DI)
- IEventBus decoration

**Step 2: Verify Program.cs has audit middleware**

After `app.UseAuthorization();` (line 200):
```csharp
app.UseMiddleware<SimpleModule.AuditLogs.Middleware.AuditMiddleware>();
```

**Step 3: Add AuditLogs to Tests.Shared csproj**

```xml
<ProjectReference Include="..\..\modules\AuditLogs\src\AuditLogs\AuditLogs.csproj" />
<ProjectReference Include="..\..\modules\AuditLogs\src\AuditLogs.Contracts\AuditLogs.Contracts.csproj" />
```

**Step 4: Full solution build**

Run: `dotnet build`
Expected: Build succeeded with 0 errors

**Step 5: Run all tests**

Run: `dotnet test`
Expected: All tests pass (existing + new)

**Step 6: Commit**

```
feat(auditlogs): wire up all components and verify full integration
```

---

## Task 18: Frontend build and full verification

**Step 1: Install npm dependencies**

Run: `npm install`

**Step 2: Build frontend**

Run: `cd modules/AuditLogs/src/AuditLogs && npx vite build`
Expected: `wwwroot/AuditLogs.pages.js` generated

**Step 3: Lint check**

Run: `npm run check`
Expected: No lint errors in AuditLogs module

**Step 4: Full solution build**

Run: `dotnet build`
Expected: Build succeeded

**Step 5: Run all tests**

Run: `dotnet test`
Expected: All tests pass

**Step 6: Commit**

```
chore(auditlogs): verify frontend build and full solution integration
```
