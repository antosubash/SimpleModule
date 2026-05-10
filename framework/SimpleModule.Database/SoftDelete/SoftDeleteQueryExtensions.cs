using Microsoft.EntityFrameworkCore;
using SimpleModule.Core.Entities;

namespace SimpleModule.Database.SoftDelete;

/// <summary>
/// Query extensions for working with soft-deleted (<see cref="ISoftDelete"/>) entities.
/// Provides a fluent surface that mirrors Laravel's <c>withTrashed()</c> / <c>onlyTrashed()</c>
/// helpers so callers do not have to reach for <see cref="EntityFrameworkQueryableExtensions.IgnoreQueryFilters{T}"/>
/// directly and leak the soft-delete abstraction.
/// </summary>
public static class SoftDeleteQueryExtensions
{
    /// <summary>
    /// Includes soft-deleted rows in the query result alongside live rows by ignoring
    /// the soft-delete query filter.
    /// </summary>
    public static IQueryable<T> WithTrashed<T>(this IQueryable<T> query)
        where T : class, ISoftDelete
    {
        ArgumentNullException.ThrowIfNull(query);
        return query.IgnoreQueryFilters([DatabaseConstants.SoftDeleteQueryFilterKey]);
    }

    /// <summary>
    /// Returns only soft-deleted rows. Other query filters (e.g. multi-tenant) remain active.
    /// </summary>
    public static IQueryable<T> OnlyTrashed<T>(this IQueryable<T> query)
        where T : class, ISoftDelete
    {
        ArgumentNullException.ThrowIfNull(query);
        return query
            .IgnoreQueryFilters([DatabaseConstants.SoftDeleteQueryFilterKey])
            .Where(e => e.IsDeleted);
    }
}
