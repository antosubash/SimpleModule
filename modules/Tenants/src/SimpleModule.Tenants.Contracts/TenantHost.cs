using SimpleModule.Core;

namespace SimpleModule.Tenants.Contracts;

[Dto]
public class TenantHost
{
    public TenantHostId Id { get; set; }
    public TenantId TenantId { get; set; }
    public string HostName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
