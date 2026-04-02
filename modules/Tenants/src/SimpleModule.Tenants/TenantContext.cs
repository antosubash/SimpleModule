using SimpleModule.Core.Entities;

namespace SimpleModule.Tenants;

public class TenantContext : ITenantContext
{
    public string? TenantId { get; set; }
}
