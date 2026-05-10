using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SimpleModule.Core.Entities;

namespace SimpleModule.Database.SoftDelete;

/// <summary>
/// Extensions that bypass the soft-delete interceptor and issue a real <c>DELETE</c>
/// against the database. Required for GDPR purges, test cleanup, and recovery flows
/// where retaining the row is undesirable.
/// </summary>
/// <remarks>
/// The marker is tracked per-entity in a <see cref="ConditionalWeakTable{TKey, TValue}"/>
/// keyed by <see cref="DbContext"/>, then consumed (and removed) by
/// <c>EntityInterceptor</c> when it inspects each entry. Markers do not survive past
/// the next <c>SaveChanges</c> on that context.
/// <para>
/// If <c>SaveChanges</c> never runs after a <c>ForceDelete</c> call (e.g. an exception
/// aborts the unit of work, or the entity is detached via <c>ChangeTracker.Clear()</c>
/// before save), the marker remains until the <see cref="DbContext"/> itself is collected.
/// This is bounded — scoped contexts are short-lived — but callers using long-lived
/// contexts should not stage force-deletes they don't intend to commit.
/// </para>
/// </remarks>
public static class ForceDeleteExtensions
{
    private static readonly ConditionalWeakTable<DbContext, HashSet<object>> Markers = new();

    /// <summary>
    /// Marks <paramref name="entity"/> for hard delete and removes it from the context.
    /// The soft-delete interceptor will leave this entry alone, so a real <c>DELETE</c>
    /// statement is issued on <c>SaveChanges</c>.
    /// </summary>
    public static EntityEntry<T> ForceDelete<T>(this DbContext context, T entity)
        where T : class, ISoftDelete
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(entity);

        Mark(context, entity);
        return context.Remove(entity);
    }

    /// <summary>
    /// Marks each entity for hard delete and removes them from the context.
    /// </summary>
    public static void ForceDeleteRange<T>(this DbContext context, IEnumerable<T> entities)
        where T : class, ISoftDelete
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(entities);

        foreach (var entity in entities)
        {
            Mark(context, entity);
            context.Remove(entity);
        }
    }

    internal static bool IsMarked(DbContext context, object entity) =>
        Markers.TryGetValue(context, out var set) && set.Contains(entity);

    /// <summary>
    /// Removes the marker for <paramref name="entity"/> after the interceptor has
    /// observed it. Called by <c>EntityInterceptor</c> per entry to keep the marker
    /// set bounded.
    /// </summary>
    internal static void Consume(DbContext context, object entity)
    {
        if (Markers.TryGetValue(context, out var set))
        {
            set.Remove(entity);
        }
    }

    private static void Mark(DbContext context, object entity)
    {
        var set = Markers.GetValue(context, _ => []);
        set.Add(entity);
    }
}
