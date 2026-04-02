namespace SimpleModule.Core.Entities;

/// <summary>
/// Entity with full audit tracking, soft delete, versioning, multi-tenancy, and extra properties.
/// Use this for entities in multi-tenant modules that need maximum extensibility.
/// </summary>
public abstract class MultiTenantFullAuditableEntity<TId>
    : FullAuditableEntity<TId>,
        IMultiTenant,
        IHasExtraProperties
{
    public string TenantId { get; set; } = string.Empty;
    public Dictionary<string, object?> ExtraProperties { get; set; } = [];
}
