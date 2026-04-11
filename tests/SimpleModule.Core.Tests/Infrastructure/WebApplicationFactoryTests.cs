using System.Net;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;
using SimpleModule.Host;
using SimpleModule.Tests.Shared.Fixtures;

namespace SimpleModule.Core.Tests.Infrastructure;

/// <summary>
/// Guards the test infrastructure itself — catches circular dependencies,
/// missing DbContext replacements, and service resolution deadlocks that
/// would silently hang every integration test.
/// </summary>
[Collection(TestCollections.Integration)]
public class WebApplicationFactoryTests
{
    private readonly SimpleModuleWebApplicationFactory _factory;

    public WebApplicationFactoryTests(SimpleModuleWebApplicationFactory factory) =>
        _factory = factory;

    // ── Host startup ────────────────────────────────────────────────

    [Fact]
    public void Host_Starts_Successfully()
    {
        // Accessing Services triggers host build + start.
        // If this hangs, a hosted service or startup code deadlocks.
        var services = _factory.Services;
        services.Should().NotBeNull();
    }

    [Fact]
    public async Task Host_CanServeHealthEndpoint()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/health/live");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── DbContext resolution (catches circular dependency deadlocks) ─

    [Theory]
    [MemberData(nameof(AllDbContextTypes))]
    public void DbContext_CanBeResolved(Type dbContextType)
    {
        // This is the exact scenario that deadlocked before the fix:
        // resolving any DbContext whose options factory resolved
        // ISaveChangesInterceptor, which resolved ISettingsContracts,
        // which resolved SettingsDbContext — circular.
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService(dbContextType);
        context.Should().NotBeNull();
        context.Should().BeAssignableTo<DbContext>();
    }

    [Theory]
    [MemberData(nameof(AllDbContextTypes))]
    public void DbContext_UsesInMemorySqlite(Type dbContextType)
    {
        // Every module DbContext must be replaced with in-memory SQLite
        // in the test factory. If a context still points at a real DB,
        // tests will either fail with connection errors or corrupt
        // a local database file.
        using var scope = _factory.Services.CreateScope();
        var context = (DbContext)scope.ServiceProvider.GetRequiredService(dbContextType);
        var conn = context.Database.GetDbConnection();
        conn.Should().BeOfType<SqliteConnection>();
        conn.ConnectionString.Should().Contain(":memory:");
    }

    [Theory]
    [MemberData(nameof(AllDbContextTypes))]
    public void DbContext_CanCreateSchema(Type dbContextType)
    {
        // EnsureCreated must succeed for every context.
        // Catches model mismatches, missing configurations, etc.
        using var scope = _factory.Services.CreateScope();
        var context = (DbContext)scope.ServiceProvider.GetRequiredService(dbContextType);
        var created = context.Database.EnsureCreated();
        // created may be true or false depending on shared connection;
        // the point is it doesn't throw.
    }

    // ── Interceptor resolution (the exact deadlock scenario) ────────

    [Fact]
    public void SaveChangesInterceptors_CanBeResolved()
    {
        // The AuditSaveChangesInterceptor caused a circular dependency
        // deadlock when resolved inside a DbContextOptions factory.
        // This test verifies interceptors resolve cleanly from a scope.
        using var scope = _factory.Services.CreateScope();
        var interceptors = scope.ServiceProvider.GetServices<ISaveChangesInterceptor>().ToList();
        interceptors.Should().NotBeNull();
    }

    [Theory]
    [MemberData(nameof(AllDbContextTypes))]
    public void DbContext_DoesNotWireInterceptorsFromDI(Type dbContextType)
    {
        // Test DbContexts must NOT auto-resolve interceptors from DI.
        // The production AddModuleDbContext wires them via
        // sp.GetServices<ISaveChangesInterceptor>(), but the test
        // replacement must skip this to avoid circular dependencies.
        using var scope = _factory.Services.CreateScope();
        var context = (DbContext)scope.ServiceProvider.GetRequiredService(dbContextType);

        // Access the internal service provider that EF Core uses for this context.
        // If UseApplicationServiceProvider was called (as AddModuleDbContext does),
        // EF Core can auto-discover interceptors from DI — exactly the pattern that
        // caused the circular dependency deadlock.
        var internalSp = ((IInfrastructure<IServiceProvider>)context).Instance;
        var interceptors = internalSp.GetServices<ISaveChangesInterceptor>().ToList();

        var hasAuditInterceptor = interceptors.Any(i =>
            i.GetType().FullName?.Contains("AuditSaveChangesInterceptor", StringComparison.Ordinal)
            ?? false
        );
        hasAuditInterceptor
            .Should()
            .BeFalse(
                "test DbContexts must not wire AuditSaveChangesInterceptor — "
                    + "it causes a circular dependency deadlock"
            );
    }

    // ── Contract / service resolution ───────────────────────────────

    [Fact]
    public void SettingsContracts_CanBeResolved()
    {
        // ISettingsContracts was the middle link in the circular chain.
        // Verify it resolves independently.
        using var scope = _factory.Services.CreateScope();
        var svc =
            scope.ServiceProvider.GetService<SimpleModule.Settings.Contracts.ISettingsContracts>();
        svc.Should().NotBeNull();
    }

    [Fact]
    public void AuditLogContracts_CanBeResolved()
    {
        using var scope = _factory.Services.CreateScope();
        var svc =
            scope.ServiceProvider.GetService<SimpleModule.AuditLogs.Contracts.IAuditLogContracts>();
        svc.Should().NotBeNull();
    }

    // ── Circular dependency detection ───────────────────────────────
    //
    // These tests defend against runtime circular dependencies that the source
    // generator cannot catch at compile time. The generator's SM0010 only sees
    // module-level dependencies from project references — it cannot analyze
    // factory lambdas in ConfigureServices (e.g. the AuditLogs module decorates
    // IEventBus with AuditingEventBus, which itself depends on ISettingsContracts).
    //
    // If a module adds IEventBus to a service whose contract is consumed by a
    // decorator, the cycle only surfaces at DI resolution time and manifests as
    // an infinite recursion / hang (not a clean InvalidOperationException, because
    // .NET DI cannot track re-entry through factory lambdas).
    //
    // The tests below use a timeout so any such cycle fails fast with a clear
    // error message instead of hanging the entire test run.

    private static readonly TimeSpan ResolutionTimeout = TimeSpan.FromSeconds(5);

    [Theory]
    [MemberData(nameof(AllContractTypes))]
    public async Task ContractInterface_CanBeResolved_WithoutHanging(Type contractType)
    {
        // Resolve the contract in a fresh scope with a hard timeout. A hang
        // here means a circular dependency was introduced through a decorator
        // factory (e.g. AuditingEventBus → ISettingsContracts → IEventBus).
        await AssertResolvesWithinTimeout(contractType);
    }

    [Fact]
    public async Task EventBus_CanBeResolved_WithoutHanging()
    {
        // IEventBus is decorated by AuditingEventBus, which takes ISettingsContracts
        // as a constructor parameter. If any contract implementation adds IEventBus
        // to its constructor, resolving IEventBus hangs because the factory lambda
        // re-enters itself via ISettingsContracts.
        await AssertResolvesWithinTimeout(typeof(SimpleModule.Core.Events.IEventBus));
    }

    [Fact]
    public async Task EventBus_CanPublishEvent_WithoutHanging()
    {
        // End-to-end check: resolve IEventBus, publish a test event, confirm the
        // pipeline completes. This catches cycles that only appear when the bus
        // dispatches to handlers (e.g. a handler whose constructor triggers the cycle).
        var rootProvider = _factory.Services;

        var publishTask = Task.Run(async () =>
        {
            using var scope = rootProvider.CreateScope();
            var bus =
                scope.ServiceProvider.GetRequiredService<SimpleModule.Core.Events.IEventBus>();
            await bus.PublishAsync(new NoopEvent(), CancellationToken.None);
        });

        var completed = await Task.WhenAny(publishTask, Task.Delay(ResolutionTimeout));
        if (completed != publishTask)
        {
            throw new InvalidOperationException(
                "IEventBus.PublishAsync hung for over "
                    + $"{ResolutionTimeout.TotalSeconds}s. A handler, decorator, or "
                    + "service in the resolution chain likely has a circular dependency."
            );
        }

        await publishTask;
    }

    private async Task AssertResolvesWithinTimeout(Type serviceType)
    {
        // Force the factory to build its host before we start timing — the first
        // access can take several seconds and we only care about the cost of
        // resolving the specific service, not test-fixture warmup.
        var rootProvider = _factory.Services;

        // Resolve on a thread-pool thread so a blocking / hanging resolution
        // can be observed and cancelled from the test thread.
        var resolveTask = Task.Run(() =>
        {
            using var scope = rootProvider.CreateScope();
            var svc = scope.ServiceProvider.GetService(serviceType);
            svc.Should().NotBeNull($"{serviceType.FullName} should be registered");
        });

        var completed = await Task.WhenAny(resolveTask, Task.Delay(ResolutionTimeout));
        if (completed != resolveTask)
        {
            throw new InvalidOperationException(
                $"Resolving '{serviceType.FullName}' hung for over "
                    + $"{ResolutionTimeout.TotalSeconds}s. Likely a circular dependency "
                    + "introduced through a decorator factory (see IEventBus → "
                    + "AuditingEventBus → ISettingsContracts pattern)."
            );
        }

        // Surface any exception from the resolve task (e.g. a clean circular-dep
        // error from the DI container).
        await resolveTask;
    }

    public static TheoryData<Type> AllContractTypes
    {
        get
        {
            var data = new TheoryData<Type>();
            foreach (var type in ModuleContractRegistry.All)
            {
                data.Add(type);
            }
            return data;
        }
    }

    private sealed record NoopEvent : SimpleModule.Core.Events.IEvent;

    // ── Authenticated client ────────────────────────────────────────

    [Fact]
    public async Task CreateAuthenticatedClient_CanMakeRequests()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync("/health/live");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Test data ───────────────────────────────────────────────────

    /// <summary>
    /// Uses the source-generated ModuleDbContextRegistry so this list
    /// auto-updates when a new module with a DbContext is added.
    /// Also includes HostDbContext which is not a module context.
    /// </summary>
    public static TheoryData<Type> AllDbContextTypes
    {
        get
        {
            var data = new TheoryData<Type> { typeof(HostDbContext) };
            foreach (var type in ModuleDbContextRegistry.All)
            {
                data.Add(type);
            }

            return data;
        }
    }
}
