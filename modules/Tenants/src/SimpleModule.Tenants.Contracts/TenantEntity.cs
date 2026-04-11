using SimpleModule.Core;
using SimpleModule.Core.Entities;

namespace SimpleModule.Tenants.Contracts;

[NoDtoGeneration]
public sealed class TenantEntity : AuditableEntity<TenantId>
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public TenantStatus Status { get; set; }
    public string? AdminEmail { get; set; }
    public string? EditionName { get; set; }

    // Infrastructure-only: populated and read by the Tenants module's tenant
    // resolution pipeline. Other modules must not read this directly; use
    // ITenantsContracts to go through the authorization boundary.
    public string? ConnectionString { get; set; }

    public DateTimeOffset? ValidUpTo { get; set; }
    public ICollection<TenantHostEntity> Hosts { get; set; } = [];
}
