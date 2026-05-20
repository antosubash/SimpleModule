using SimpleModule.Core.Entities;

namespace SimpleModule.Database.SoftDelete;

/// <summary>
/// Recovery operations for a soft-deletable entity. Mirrors the operations needed by
/// admin restore UIs, GDPR purge flows, and retention policies.
/// </summary>
/// <typeparam name="T">The <see cref="ISoftDelete"/> entity type managed by this service.</typeparam>
public interface ISoftDeleteService<T>
    where T : class, ISoftDelete
{
    /// <summary>
    /// Restores a single soft-deleted row by primary key. The row's <see cref="ISoftDelete.IsDeleted"/>,
    /// <see cref="ISoftDelete.DeletedAt"/>, and <see cref="ISoftDelete.DeletedBy"/> fields are cleared.
    /// </summary>
    /// <returns>1 if a row was restored; 0 if the id was not found among trashed rows.</returns>
    Task<int> RestoreAsync(object id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Issues a real <c>DELETE</c> for the row with the given key, bypassing the soft-delete
    /// interceptor. The row is removed from the database whether or not it was previously
    /// soft-deleted.
    /// </summary>
    /// <returns>1 if a row was deleted; 0 if the id was not found.</returns>
    Task<int> ForceDeleteAsync(object id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Issues a real <c>DELETE</c> for each row matching the given keys.
    /// </summary>
    /// <returns>The number of rows actually deleted.</returns>
    Task<int> ForceDeleteRangeAsync(
        IEnumerable<object> ids,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Permanently deletes every soft-deleted row whose <see cref="ISoftDelete.DeletedAt"/>
    /// is older than <paramref name="age"/>. Use this from a scheduled background job to
    /// implement retention policies.
    /// </summary>
    /// <returns>The number of rows purged.</returns>
    Task<int> PurgeOlderThanAsync(TimeSpan age, CancellationToken cancellationToken = default);
}
