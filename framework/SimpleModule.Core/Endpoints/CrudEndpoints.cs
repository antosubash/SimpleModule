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
        return TypedResults.Created(new Uri(locationFactory(entity), UriKind.RelativeOrAbsolute), entity);
    }

    public static async Task<IResult> Update<T>(Func<Task<T>> update)
        where T : class =>
        TypedResults.Ok(await update());

    public static async Task<IResult> Delete(Func<Task> delete)
    {
        await delete();
        return TypedResults.NoContent();
    }
}
