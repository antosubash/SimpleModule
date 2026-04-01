using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SimpleModule.Admin;
using SimpleModule.AuditLogs;
using SimpleModule.FileStorage;
using SimpleModule.Orders;
using SimpleModule.PageBuilder;
using SimpleModule.Products;
using SimpleModule.Settings;
using SimpleModule.Tests.Shared.Fixtures;
using SimpleModule.Users;

namespace SimpleModule.Core.Tests.Infrastructure;

/// <summary>
/// Verifies that all module options are registered via IOptions&lt;T&gt; and can be
/// resolved with their default values. Also verifies that overrides from the host
/// app's Configure{Module}() calls are applied correctly.
/// </summary>
public class ModuleOptionsTests : IClassFixture<SimpleModuleWebApplicationFactory>
{
    private readonly SimpleModuleWebApplicationFactory _factory;

    public ModuleOptionsTests(SimpleModuleWebApplicationFactory factory) => _factory = factory;

    // ── Default values ─────────────────────────────────────────────────

    [Fact]
    public void ProductsModuleOptions_IsRegistered_WithDefaults()
    {
        using var scope = _factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<ProductsModuleOptions>>();

        options.Value.DefaultPageSize.Should().Be(10);
        options.Value.MaxPageSize.Should().Be(100);
    }

    [Fact]
    public void AuditLogsModuleOptions_IsRegistered_WithDefaults()
    {
        using var scope = _factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<AuditLogsModuleOptions>>();

        options.Value.WriterBatchSize.Should().Be(100);
        options.Value.WriterFlushInterval.Should().Be(TimeSpan.FromSeconds(2));
        options.Value.RetentionDays.Should().Be(90);
        options.Value.RetentionCheckInterval.Should().Be(TimeSpan.FromHours(24));
    }

    [Fact]
    public void AdminModuleOptions_IsRegistered_WithDefaults()
    {
        using var scope = _factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<AdminModuleOptions>>();

        options.Value.UsersPageSize.Should().Be(20);
    }

    [Fact]
    public void FileStorageModuleOptions_IsRegistered_WithDefaults()
    {
        using var scope = _factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<
            IOptions<FileStorageModuleOptions>
        >();

        options.Value.MaxFileSizeMb.Should().Be(50);
        options.Value.AllowedExtensions.Should().Contain(".jpg");
    }

    [Fact]
    public void PageBuilderModuleOptions_IsRegistered_WithDefaults()
    {
        using var scope = _factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<
            IOptions<PageBuilderModuleOptions>
        >();

        options.Value.MaxTitleLength.Should().Be(200);
        options.Value.MaxSlugLength.Should().Be(200);
    }

    [Fact]
    public void UsersModuleOptions_IsRegistered_WithDefaults()
    {
        using var scope = _factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<UsersModuleOptions>>();

        options.Value.PasswordMinLength.Should().Be(8);
        options.Value.PasswordRequireDigit.Should().BeTrue();
        options.Value.PasswordRequireUppercase.Should().BeTrue();
        options.Value.PasswordRequireLowercase.Should().BeTrue();
        options.Value.PasswordRequireNonAlphanumeric.Should().BeFalse();
        options.Value.MaxFailedAccessAttempts.Should().Be(5);
        options.Value.LockoutDuration.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void SettingsModuleOptions_IsRegistered_WithDefaults()
    {
        using var scope = _factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<SettingsModuleOptions>>();

        options.Value.CacheDuration.Should().Be(TimeSpan.FromSeconds(60));
    }

    [Fact]
    public void OrdersModuleOptions_IsRegistered_WithDefaults()
    {
        using var scope = _factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<OrdersModuleOptions>>();

        options.Value.DefaultPageSize.Should().Be(10);
        options.Value.MaxPageSize.Should().Be(100);
    }

    // ── Override test ──────────────────────────────────────────────────

    [Fact]
    public void ModuleOptions_CanBeOverridden_ViaServicesConfigure()
    {
        // Simulate what a host app would do: services.Configure<T>(...)
        // We test this by creating a custom factory with overrides
        var services = new ServiceCollection();

        // Register defaults (what the generator does)
        services.AddOptions<ProductsModuleOptions>();
        services.AddOptions<AuditLogsModuleOptions>();
        services.AddOptions<AdminModuleOptions>();
        services.AddOptions<OrdersModuleOptions>();
        services.AddOptions<UsersModuleOptions>();
        services.AddOptions<SettingsModuleOptions>();
        services.AddOptions<FileStorageModuleOptions>();
        services.AddOptions<PageBuilderModuleOptions>();

        // Override (what the host app does via ConfigureProducts, etc.)
        services.Configure<ProductsModuleOptions>(o =>
        {
            o.DefaultPageSize = 25;
            o.MaxPageSize = 50;
        });
        services.Configure<AuditLogsModuleOptions>(o =>
        {
            o.WriterBatchSize = 200;
            o.RetentionDays = 30;
        });
        services.Configure<AdminModuleOptions>(o => o.UsersPageSize = 50);
        services.Configure<OrdersModuleOptions>(o => o.DefaultPageSize = 20);
        services.Configure<UsersModuleOptions>(o =>
        {
            o.PasswordMinLength = 12;
            o.MaxFailedAccessAttempts = 3;
        });
        services.Configure<SettingsModuleOptions>(o => o.CacheDuration = TimeSpan.FromMinutes(5));
        services.Configure<FileStorageModuleOptions>(o =>
        {
            o.MaxFileSizeMb = 100;
            o.AllowedExtensions = ".pdf,.zip";
        });
        services.Configure<PageBuilderModuleOptions>(o =>
        {
            o.MaxTitleLength = 300;
            o.MaxSlugLength = 300;
        });

        using var sp = services.BuildServiceProvider();

        sp.GetRequiredService<IOptions<ProductsModuleOptions>>()
            .Value.DefaultPageSize.Should()
            .Be(25);
        sp.GetRequiredService<IOptions<ProductsModuleOptions>>().Value.MaxPageSize.Should().Be(50);

        sp.GetRequiredService<IOptions<AuditLogsModuleOptions>>()
            .Value.WriterBatchSize.Should()
            .Be(200);
        sp.GetRequiredService<IOptions<AuditLogsModuleOptions>>()
            .Value.RetentionDays.Should()
            .Be(30);

        sp.GetRequiredService<IOptions<AdminModuleOptions>>().Value.UsersPageSize.Should().Be(50);

        sp.GetRequiredService<IOptions<OrdersModuleOptions>>()
            .Value.DefaultPageSize.Should()
            .Be(20);

        sp.GetRequiredService<IOptions<UsersModuleOptions>>()
            .Value.PasswordMinLength.Should()
            .Be(12);
        sp.GetRequiredService<IOptions<UsersModuleOptions>>()
            .Value.MaxFailedAccessAttempts.Should()
            .Be(3);

        sp.GetRequiredService<IOptions<SettingsModuleOptions>>()
            .Value.CacheDuration.Should()
            .Be(TimeSpan.FromMinutes(5));

        sp.GetRequiredService<IOptions<FileStorageModuleOptions>>()
            .Value.MaxFileSizeMb.Should()
            .Be(100);
        sp.GetRequiredService<IOptions<FileStorageModuleOptions>>()
            .Value.AllowedExtensions.Should()
            .Be(".pdf,.zip");

        sp.GetRequiredService<IOptions<PageBuilderModuleOptions>>()
            .Value.MaxTitleLength.Should()
            .Be(300);
        sp.GetRequiredService<IOptions<PageBuilderModuleOptions>>()
            .Value.MaxSlugLength.Should()
            .Be(300);
    }
}
