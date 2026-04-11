using SimpleModule.Core;
using SimpleModule.Core.Entities;

namespace SimpleModule.Tenants.Contracts;

[NoDtoGeneration]
public sealed class TenantHostEntity : Entity<TenantHostId>
{
    public TenantId TenantId { get; set; }
    public string HostName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public TenantEntity Tenant { get; set; } = null!;
}
