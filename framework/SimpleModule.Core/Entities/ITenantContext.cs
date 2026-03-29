namespace SimpleModule.Core.Entities;

/// <summary>
/// Provides the current tenant ID for multi-tenant entity filtering and assignment.
/// Resolved from the current request context (e.g., HTTP header, claim, or subdomain).
/// </summary>
public interface ITenantContext
{
    string? TenantId { get; }
}
