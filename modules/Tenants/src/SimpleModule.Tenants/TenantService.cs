using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SimpleModule.Core.Events;
using SimpleModule.Core.Exceptions;
using SimpleModule.Tenants.Contracts;
using SimpleModule.Tenants.Contracts.Events;
using SimpleModule.Tenants.Entities;

namespace SimpleModule.Tenants;

public sealed partial class TenantService(
    TenantsDbContext db,
    IEventBus eventBus,
    ILogger<TenantService> logger
) : ITenantContracts
{
    public async Task<IEnumerable<Tenant>> GetAllTenantsAsync() =>
        await db
            .Tenants.AsNoTracking()
            .Include(t => t.Hosts)
            .Select(t => MapToDto(t))
            .ToListAsync();

    public async Task<Tenant?> GetTenantByIdAsync(TenantId id)
    {
        var entity = await db
            .Tenants.AsNoTracking()
            .Include(t => t.Hosts)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (entity is null)
        {
            LogTenantNotFound(logger, id);
            return null;
        }

        return MapToDto(entity);
    }

    public async Task<Tenant?> GetTenantBySlugAsync(string slug)
    {
        var entity = await db
            .Tenants.AsNoTracking()
            .Include(t => t.Hosts)
            .FirstOrDefaultAsync(t => t.Slug == slug);

        return entity is null ? null : MapToDto(entity);
    }

    public async Task<Tenant?> GetTenantByHostNameAsync(string hostName)
    {
        var host = await db
            .TenantHosts.AsNoTracking()
            .FirstOrDefaultAsync(h => h.HostName == hostName && h.IsActive);

        if (host is null)
        {
            return null;
        }

        return await GetTenantByIdAsync(host.TenantId);
    }

    public async Task<Tenant> CreateTenantAsync(CreateTenantRequest request)
    {
        var entity = new TenantEntity
        {
            Name = request.Name,
            Slug = request.Slug,
            Status = TenantStatus.Active,
            AdminEmail = request.AdminEmail,
            EditionName = request.EditionName,
            ConnectionString = request.ConnectionString,
            ValidUpTo = request.ValidUpTo,
        };

        if (request.Hosts is { Count: > 0 })
        {
            foreach (var hostName in request.Hosts)
            {
                entity.Hosts.Add(new TenantHostEntity { HostName = hostName });
            }
        }

        db.Tenants.Add(entity);
        await db.SaveChangesAsync();

        LogTenantCreated(logger, entity.Id, entity.Name);
        await eventBus.PublishAsync(
            new TenantCreatedEvent(entity.Id, entity.Name, entity.Slug)
        );

        return MapToDto(entity);
    }

    public async Task<Tenant> UpdateTenantAsync(TenantId id, UpdateTenantRequest request)
    {
        var entity = await db.Tenants.Include(t => t.Hosts).FirstOrDefaultAsync(t => t.Id == id);
        if (entity is null)
        {
            throw new NotFoundException("Tenant", id);
        }

        entity.Name = request.Name;
        entity.AdminEmail = request.AdminEmail;
        entity.EditionName = request.EditionName;
        entity.ConnectionString = request.ConnectionString;
        entity.ValidUpTo = request.ValidUpTo;

        await db.SaveChangesAsync();

        LogTenantUpdated(logger, entity.Id, entity.Name);
        await eventBus.PublishAsync(new TenantUpdatedEvent(entity.Id, entity.Name));

        return MapToDto(entity);
    }

    public async Task DeleteTenantAsync(TenantId id)
    {
        var entity = await db.Tenants.FindAsync(id);
        if (entity is null)
        {
            throw new NotFoundException("Tenant", id);
        }

        db.Tenants.Remove(entity);
        await db.SaveChangesAsync();

        LogTenantDeleted(logger, id);
    }

    public async Task<Tenant> ChangeStatusAsync(TenantId id, TenantStatus status)
    {
        var entity = await db.Tenants.Include(t => t.Hosts).FirstOrDefaultAsync(t => t.Id == id);
        if (entity is null)
        {
            throw new NotFoundException("Tenant", id);
        }

        var oldStatus = entity.Status;
        entity.Status = status;
        await db.SaveChangesAsync();

        LogTenantStatusChanged(logger, id, oldStatus, status);
        await eventBus.PublishAsync(new TenantStatusChangedEvent(id, oldStatus, status));

        return MapToDto(entity);
    }

    public async Task<TenantHost> AddHostAsync(TenantId tenantId, AddTenantHostRequest request)
    {
        var tenant = await db.Tenants.FindAsync(tenantId);
        if (tenant is null)
        {
            throw new NotFoundException("Tenant", tenantId);
        }

        var hostEntity = new TenantHostEntity
        {
            TenantId = tenantId,
            HostName = request.HostName,
        };

        db.TenantHosts.Add(hostEntity);
        await db.SaveChangesAsync();

        LogHostAdded(logger, tenantId, request.HostName);
        await eventBus.PublishAsync(new TenantHostAddedEvent(tenantId, request.HostName));

        return MapHostToDto(hostEntity);
    }

    public async Task RemoveHostAsync(TenantId tenantId, TenantHostId hostId)
    {
        var host = await db
            .TenantHosts.FirstOrDefaultAsync(h => h.Id == hostId && h.TenantId == tenantId);
        if (host is null)
        {
            throw new NotFoundException("TenantHost", hostId);
        }

        var hostName = host.HostName;
        db.TenantHosts.Remove(host);
        await db.SaveChangesAsync();

        LogHostRemoved(logger, tenantId, hostName);
        await eventBus.PublishAsync(new TenantHostRemovedEvent(tenantId, hostName));
    }

    private static Tenant MapToDto(TenantEntity entity) =>
        new()
        {
            Id = entity.Id,
            Name = entity.Name,
            Slug = entity.Slug,
            Status = entity.Status,
            AdminEmail = entity.AdminEmail,
            EditionName = entity.EditionName,
            ConnectionString = entity.ConnectionString,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            ValidUpTo = entity.ValidUpTo,
            Hosts = entity.Hosts.Select(MapHostToDto).ToList(),
        };

    private static TenantHost MapHostToDto(TenantHostEntity entity) =>
        new()
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            HostName = entity.HostName,
            IsActive = entity.IsActive,
        };

    [LoggerMessage(Level = LogLevel.Warning, Message = "Tenant with ID {TenantId} not found")]
    private static partial void LogTenantNotFound(ILogger logger, TenantId tenantId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Tenant {TenantId} created: {TenantName}"
    )]
    private static partial void LogTenantCreated(
        ILogger logger,
        TenantId tenantId,
        string tenantName
    );

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Tenant {TenantId} updated: {TenantName}"
    )]
    private static partial void LogTenantUpdated(
        ILogger logger,
        TenantId tenantId,
        string tenantName
    );

    [LoggerMessage(Level = LogLevel.Information, Message = "Tenant {TenantId} deleted")]
    private static partial void LogTenantDeleted(ILogger logger, TenantId tenantId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Tenant {TenantId} status changed from {OldStatus} to {NewStatus}"
    )]
    private static partial void LogTenantStatusChanged(
        ILogger logger,
        TenantId tenantId,
        TenantStatus oldStatus,
        TenantStatus newStatus
    );

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Host {HostName} added to tenant {TenantId}"
    )]
    private static partial void LogHostAdded(
        ILogger logger,
        TenantId tenantId,
        string hostName
    );

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Host {HostName} removed from tenant {TenantId}"
    )]
    private static partial void LogHostRemoved(
        ILogger logger,
        TenantId tenantId,
        string hostName
    );
}
