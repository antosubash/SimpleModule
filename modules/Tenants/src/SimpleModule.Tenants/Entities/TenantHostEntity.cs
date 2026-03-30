using SimpleModule.Core.Entities;
using SimpleModule.Tenants.Contracts;

namespace SimpleModule.Tenants.Entities;

public sealed class TenantHostEntity : Entity<TenantHostId>
{
    public TenantId TenantId { get; set; }
    public string HostName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public TenantEntity Tenant { get; set; } = null!;
}
