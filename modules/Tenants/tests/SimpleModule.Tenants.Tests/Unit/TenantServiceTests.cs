using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using SimpleModule.Core.Exceptions;
using SimpleModule.Database;
using SimpleModule.Tenants;
using SimpleModule.Tenants.Contracts;
using Wolverine;

namespace Tenants.Tests.Unit;

public sealed class TenantServiceTests : IDisposable
{
    private readonly TenantsDbContext _db;
    private readonly TenantService _sut;
    private readonly IMessageBus _bus = Substitute.For<IMessageBus>();

    public TenantServiceTests()
    {
        var options = new DbContextOptionsBuilder<TenantsDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        var dbOptions = Options.Create(
            new DatabaseOptions
            {
                ModuleConnections = new Dictionary<string, string>
                {
                    ["Tenants"] = "Data Source=:memory:",
                },
            }
        );
        _db = new TenantsDbContext(options, dbOptions);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();
        _sut = new TenantService(_db, _bus, NullLogger<TenantService>.Instance);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetAllTenantsAsync_ReturnsSeedTenants()
    {
        var tenants = await _sut.GetAllTenantsAsync();

        tenants.Should().NotBeEmpty();
        tenants.Should().Contain(t => t.Slug == "acme");
        tenants.Should().Contain(t => t.Slug == "contoso");
    }

    [Fact]
    public async Task GetTenantByIdAsync_WithExistingId_ReturnsTenant()
    {
        var tenant = await _sut.GetTenantByIdAsync(TenantId.From(1));

        tenant.Should().NotBeNull();
        tenant!.Name.Should().Be("Acme Corporation");
        tenant.Hosts.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTenantByIdAsync_WithNonExistentId_ReturnsNull()
    {
        var tenant = await _sut.GetTenantByIdAsync(TenantId.From(99999));

        tenant.Should().BeNull();
    }

    [Fact]
    public async Task CreateTenantAsync_CreatesAndReturnsTenant()
    {
        var request = new CreateTenantRequest
        {
            Name = "New Tenant",
            Slug = "new-tenant",
            AdminEmail = "admin@new.com",
            Hosts = ["new.localhost"],
        };

        var tenant = await _sut.CreateTenantAsync(request);

        tenant.Should().NotBeNull();
        tenant.Name.Should().Be("New Tenant");
        tenant.Slug.Should().Be("new-tenant");
        tenant.Status.Should().Be(TenantStatus.Active);
        tenant.Hosts.Should().HaveCount(1);
        tenant.Hosts[0].HostName.Should().Be("new.localhost");
        await _bus.Received(1)
            .PublishAsync(
                Arg.Any<SimpleModule.Tenants.Contracts.Events.TenantCreatedEvent>(),
                Arg.Any<DeliveryOptions?>()
            );
    }

    [Fact]
    public async Task UpdateTenantAsync_WithValidData_UpdatesTenant()
    {
        var request = new UpdateTenantRequest { Name = "Updated Acme" };
        var updated = await _sut.UpdateTenantAsync(TenantId.From(1), request);

        updated.Name.Should().Be("Updated Acme");
        await _bus.Received()
            .PublishAsync(
                Arg.Any<SimpleModule.Tenants.Contracts.Events.TenantUpdatedEvent>(),
                Arg.Any<DeliveryOptions?>()
            );
    }

    [Fact]
    public async Task UpdateTenantAsync_WithNonExistentId_ThrowsNotFoundException()
    {
        var request = new UpdateTenantRequest { Name = "Test" };
        var act = () => _sut.UpdateTenantAsync(TenantId.From(99999), request);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteTenantAsync_WithExistingId_RemovesTenant()
    {
        var create = new CreateTenantRequest { Name = "ToDelete", Slug = "to-delete" };
        var created = await _sut.CreateTenantAsync(create);

        await _sut.DeleteTenantAsync(created.Id);

        var found = await _sut.GetTenantByIdAsync(created.Id);
        found.Should().BeNull();
    }

    [Fact]
    public async Task ChangeStatusAsync_ChangesStatusAndPublishesEvent()
    {
        var result = await _sut.ChangeStatusAsync(TenantId.From(1), TenantStatus.Suspended);

        result.Status.Should().Be(TenantStatus.Suspended);
        await _bus.Received()
            .PublishAsync(
                Arg.Any<SimpleModule.Tenants.Contracts.Events.TenantStatusChangedEvent>(),
                Arg.Any<DeliveryOptions?>()
            );
    }

    [Fact]
    public async Task AddHostAsync_AddsHostToTenant()
    {
        var host = await _sut.AddHostAsync(
            TenantId.From(2),
            new AddTenantHostRequest { HostName = "new.contoso.com" }
        );

        host.HostName.Should().Be("new.contoso.com");
        host.TenantId.Should().Be(TenantId.From(2));

        var tenant = await _sut.GetTenantByIdAsync(TenantId.From(2));
        tenant!.Hosts.Should().Contain(h => h.HostName == "new.contoso.com");
    }

    [Fact]
    public async Task RemoveHostAsync_RemovesHostFromTenant()
    {
        var host = await _sut.AddHostAsync(
            TenantId.From(2),
            new AddTenantHostRequest { HostName = "temp.contoso.com" }
        );

        await _sut.RemoveHostAsync(TenantId.From(2), host.Id);

        var tenant = await _sut.GetTenantByIdAsync(TenantId.From(2));
        tenant!.Hosts.Should().NotContain(h => h.HostName == "temp.contoso.com");
    }

    [Fact]
    public async Task GetTenantByHostNameAsync_ReturnsTenantForActiveHost()
    {
        var tenant = await _sut.GetTenantByHostNameAsync("acme.localhost");

        tenant.Should().NotBeNull();
        tenant!.Slug.Should().Be("acme");
    }

    [Fact]
    public async Task GetTenantByHostNameAsync_ReturnsNullForUnknownHost()
    {
        var tenant = await _sut.GetTenantByHostNameAsync("unknown.example.com");

        tenant.Should().BeNull();
    }
}
