namespace SimpleModule.Tenants.Contracts;

public interface ITenantContracts
{
    Task<IEnumerable<Tenant>> GetAllTenantsAsync();
    Task<Tenant?> GetTenantByIdAsync(TenantId id);
    Task<Tenant?> GetTenantBySlugAsync(string slug);
    Task<Tenant?> GetTenantByHostNameAsync(string hostName);
    Task<Tenant> CreateTenantAsync(CreateTenantRequest request);
    Task<Tenant> UpdateTenantAsync(TenantId id, UpdateTenantRequest request);
    Task DeleteTenantAsync(TenantId id);
    Task<Tenant> ChangeStatusAsync(TenantId id, TenantStatus status);
    Task<TenantHost> AddHostAsync(TenantId tenantId, AddTenantHostRequest request);
    Task RemoveHostAsync(TenantId tenantId, TenantHostId hostId);
}
