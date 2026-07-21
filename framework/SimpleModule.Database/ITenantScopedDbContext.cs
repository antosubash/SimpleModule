namespace SimpleModule.Database;

/// <summary>
/// Implemented by a module <c>DbContext</c> that applies multi-tenant query filters via
/// <see cref="ModuleModelBuilderExtensions.ApplyMultiTenantFilters{TContext}"/>.
/// <para>
/// The filter references this property on the <em>executing</em> context instance, which EF
/// Core re-evaluates on every query. Expose the request's current tenant here (e.g. from an
/// injected scoped <see cref="SimpleModule.Core.Entities.ITenantContext"/>) so each request is
/// filtered by its own tenant, rather than a value frozen into the cached model.
/// </para>
/// </summary>
public interface ITenantScopedDbContext
{
    /// <summary>The tenant id for the current request, or <c>null</c> when no tenant is set.</summary>
    string? CurrentTenantId { get; }
}
