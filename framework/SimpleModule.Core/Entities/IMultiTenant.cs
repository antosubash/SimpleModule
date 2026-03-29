namespace SimpleModule.Core.Entities;

/// <summary>
/// Entities implementing this interface are automatically assigned a tenant ID
/// from the current <see cref="ITenantContext"/> on insert. A global query filter
/// restricts queries to the current tenant.
/// </summary>
public interface IMultiTenant
{
    string TenantId { get; set; }
}
