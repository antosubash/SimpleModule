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
[Collection("Integration")]
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
