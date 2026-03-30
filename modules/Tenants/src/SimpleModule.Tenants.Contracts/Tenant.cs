using SimpleModule.Core;

namespace SimpleModule.Tenants.Contracts;

[Dto]
public class Tenant
{
    public TenantId Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public TenantStatus Status { get; set; }
    public string? AdminEmail { get; set; }
    public string? EditionName { get; set; }
    public string? ConnectionString { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? ValidUpTo { get; set; }
    public List<TenantHost> Hosts { get; set; } = [];
}
