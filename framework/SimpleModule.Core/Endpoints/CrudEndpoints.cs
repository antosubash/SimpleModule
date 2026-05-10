using Microsoft.AspNetCore.Http;

namespace SimpleModule.Core.Endpoints;

public static class CrudEndpoints
{
    public static async Task<IResult> GetAll<T>(Func<Task<IEnumerable<T>>> getAll) =>
        TypedResults.Ok(await getAll());

    public static async Task<IResult> GetById<T>(Func<Task<T?>> getById)
        where T : class
    {
        var entity = await getById();
        return entity is not null ? TypedResults.Ok(entity) : TypedResults.NotFound();
    }

    public static async Task<IResult> Create<T>(
        Func<Task<T>> create,
        Func<T, string> locationFactory
    )
    {
        var entity = await create();
        return TypedResults.Created(
            new Uri(locationFactory(entity), UriKind.RelativeOrAbsolute),
            entity
        );
    }

    public static async Task<IResult> Update<T>(Func<Task<T>> update)
        where T : class => TypedResults.Ok(await update());

    public static async Task<IResult> Delete(Func<Task> delete)
    {
        await delete();
        return TypedResults.NoContent();
    }

    /// <summary>
    /// Endpoint helper for restoring a soft-deleted row. The <paramref name="restore"/>
    /// delegate should call <c>ISoftDeleteService&lt;T&gt;.RestoreAsync(id)</c> and return
    /// the number of rows affected (0 → 404, 1 → 204).
    /// </summary>
    public static async Task<IResult> Restore(Func<Task<int>> restore)
    {
        var affected = await restore();
        return affected > 0 ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    /// <summary>
    /// Endpoint helper for force-deleting a row (bypassing soft delete). The
    /// <paramref name="forceDelete"/> delegate should call
    /// <c>ISoftDeleteService&lt;T&gt;.ForceDeleteAsync(id)</c>.
    /// </summary>
    public static async Task<IResult> ForceDelete(Func<Task<int>> forceDelete)
    {
        var affected = await forceDelete();
        return affected > 0 ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
