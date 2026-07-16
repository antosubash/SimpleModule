using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SimpleModule.Core.Entities;

namespace SimpleModule.Database.SoftDelete;

/// <summary>
/// Default <see cref="ISoftDeleteService{T}"/> implementation. Generic over the entity
/// type and its owning <see cref="DbContext"/>; register one per soft-deletable entity
/// via <see cref="SoftDeleteServiceCollectionExtensions.AddSoftDelete{T, TContext}"/>.
/// </summary>
public sealed class SoftDeleteService<T, TContext>(TContext context) : ISoftDeleteService<T>
    where T : class, ISoftDelete
    where TContext : DbContext
{
    private const int PurgeBatchSize = 1000;

    private static KeyMetadata? _keyMetadata;

    public async Task<int> RestoreAsync(object id, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);

        var entity = await FindTrashedAsync(id, cancellationToken).ConfigureAwait(false);
        if (entity is null)
            return 0;

        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.DeletedBy = null;
        return await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false) > 0 ? 1 : 0;
    }

    public async Task<int> ForceDeleteAsync(
        object id,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(id);

        var entity = await FindAnyAsync(id, cancellationToken).ConfigureAwait(false);
        if (entity is null)
            return 0;

        context.ForceDelete(entity);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return 1;
    }

    public async Task<int> ForceDeleteRangeAsync(
        IEnumerable<object> ids,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(ids);

        var meta = GetKeyMetadata();
        var typedIds = CoerceIds(ids, meta.KeyType);
        if (typedIds.Length == 0)
            return 0;

        var predicate = BuildKeyContainsPredicate(meta, typedIds);
        var entities = await context
            .Set<T>()
            .WithTrashed()
            .Where(predicate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (entities.Count == 0)
            return 0;

        context.ForceDeleteRange(entities);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entities.Count;
    }

    public async Task<int> PurgeOlderThanAsync(
        TimeSpan age,
        CancellationToken cancellationToken = default
    )
    {
        if (age < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(age), "Age must be non-negative.");

        var cutoff = DateTimeOffset.UtcNow - age;
        var meta = GetKeyMetadata();
        var totalPurged = 0;
        var skip = 0;

        // Page through trashed rows ordered by primary key. We can't filter or order on
        // DeletedAt server-side under SQLite (no DateTimeOffset translation), so each
        // page is filtered against the cutoff in memory. Chunk size bounds the change
        // tracker and keeps memory under control on large tables.
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var page = await meta.OrderByKey(context.Set<T>().OnlyTrashed())
                .Skip(skip)
                .Take(PurgeBatchSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            if (page.Count == 0)
                break;

            var stale = page.Where(e => e.DeletedAt is { } d && d < cutoff).ToList();
            if (stale.Count > 0)
            {
                context.ForceDeleteRange(stale);
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                totalPurged += stale.Count;
            }

            // Skipped rows shift left after their preceding peers are deleted.
            // Advance the cursor only past the rows we kept.
            skip += page.Count - stale.Count;

            if (page.Count < PurgeBatchSize)
                break;
        }

        return totalPurged;
    }

    private Task<T?> FindTrashedAsync(object id, CancellationToken cancellationToken)
    {
        var meta = GetKeyMetadata();
        var coerced = CoerceId(id, meta.KeyType);
        return context
            .Set<T>()
            .OnlyTrashed()
            .FirstOrDefaultAsync(meta.BuildKeyEqualsPredicate(coerced), cancellationToken);
    }

    private Task<T?> FindAnyAsync(object id, CancellationToken cancellationToken)
    {
        var meta = GetKeyMetadata();
        var coerced = CoerceId(id, meta.KeyType);
        return context
            .Set<T>()
            .WithTrashed()
            .FirstOrDefaultAsync(meta.BuildKeyEqualsPredicate(coerced), cancellationToken);
    }

    private KeyMetadata GetKeyMetadata()
    {
        if (_keyMetadata is not null)
            return _keyMetadata;

        var entityType =
            context.Model.FindEntityType(typeof(T))
            ?? throw new InvalidOperationException(
                $"Entity type {typeof(T).Name} is not registered with {typeof(TContext).Name}."
            );
        var key =
            entityType.FindPrimaryKey()
            ?? throw new InvalidOperationException(
                $"Entity type {typeof(T).Name} has no primary key."
            );
        if (key.Properties.Count != 1)
            throw new InvalidOperationException(
                $"SoftDeleteService requires a single-property primary key on {typeof(T).Name}."
            );

        var keyProperty = key.Properties[0];
        _keyMetadata = new KeyMetadata(keyProperty.Name, keyProperty.ClrType);
        return _keyMetadata;
    }

    private static Expression<Func<T, bool>> BuildKeyContainsPredicate(
        KeyMetadata meta,
        Array typedIds
    )
    {
        var parameter = Expression.Parameter(typeof(T), "e");
        var keyAccess = Expression.Property(parameter, meta.KeyName);
        var idsConstant = Expression.Constant(typedIds);
        var contains = ContainsMethodFor(meta.KeyType);
        var call = Expression.Call(contains, idsConstant, keyAccess);
        return Expression.Lambda<Func<T, bool>>(call, parameter);
    }

    private static System.Reflection.MethodInfo ContainsMethodFor(Type keyType) =>
        typeof(Enumerable)
            .GetMethods()
            .First(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2)
            .MakeGenericMethod(keyType);

    private static Array CoerceIds(IEnumerable<object> ids, Type keyType)
    {
        var list = ids.ToList();
        var typed = Array.CreateInstance(keyType, list.Count);
        for (var i = 0; i < list.Count; i++)
        {
            typed.SetValue(CoerceId(list[i], keyType), i);
        }
        return typed;
    }

    private static object CoerceId(object id, Type keyType)
    {
        ArgumentNullException.ThrowIfNull(id);

        if (keyType.IsInstanceOfType(id))
        {
            return id;
        }

        var targetType = Nullable.GetUnderlyingType(keyType) ?? keyType;
        var culture = System.Globalization.CultureInfo.InvariantCulture;

        // Guid does not implement IConvertible, so Convert.ChangeType throws for it —
        // the common "string route value for a Guid PK" case. Parse it explicitly.
        if (targetType == typeof(Guid))
        {
            return id is Guid guid ? guid : Guid.Parse(Convert.ToString(id, culture)!);
        }

        // Primitive/IConvertible keys (int, long, string, ...).
        if (id is IConvertible)
        {
            try
            {
                return Convert.ChangeType(id, targetType, culture);
            }
            catch (InvalidCastException)
            {
                // Fall through to a TypeConverter (covers Vogen value-object keys, which
                // Convert.ChangeType can't handle but usually expose a TypeConverter).
            }
        }

        var converter = System.ComponentModel.TypeDescriptor.GetConverter(targetType);
        if (converter.CanConvertFrom(id.GetType()))
        {
            return converter.ConvertFrom(null, culture, id)!;
        }

        throw new InvalidOperationException(
            $"Cannot convert id of type '{id.GetType()}' to key type '{keyType}'."
        );
    }

    private sealed record KeyMetadata(string KeyName, Type KeyType)
    {
        public Expression<Func<T, bool>> BuildKeyEqualsPredicate(object id)
        {
            var parameter = Expression.Parameter(typeof(T), "e");
            var keyAccess = Expression.Property(parameter, KeyName);
            var idConstant = Expression.Constant(id, KeyType);
            var equals = Expression.Equal(keyAccess, idConstant);
            return Expression.Lambda<Func<T, bool>>(equals, parameter);
        }

        public IOrderedQueryable<T> OrderByKey(IQueryable<T> source)
        {
            var parameter = Expression.Parameter(typeof(T), "e");
            var keyAccess = Expression.Property(parameter, KeyName);
            var lambda = Expression.Lambda(keyAccess, parameter);
            var orderBy = typeof(Queryable)
                .GetMethods()
                .First(m => m.Name == nameof(Queryable.OrderBy) && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(T), KeyType);
            return (IOrderedQueryable<T>)orderBy.Invoke(null, [source, lambda])!;
        }
    }
}
