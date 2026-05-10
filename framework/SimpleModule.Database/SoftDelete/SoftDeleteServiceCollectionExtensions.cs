using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core.Entities;

namespace SimpleModule.Database.SoftDelete;

/// <summary>
/// Service registration helpers for <see cref="ISoftDeleteService{T}"/>.
/// </summary>
public static class SoftDeleteServiceCollectionExtensions
{
    /// <summary>
    /// Registers an <see cref="ISoftDeleteService{T}"/> backed by <typeparamref name="TContext"/>
    /// in DI. Call this once per soft-deletable entity from the owning module's
    /// <c>ConfigureServices</c>:
    /// <code>
    /// services.AddSoftDelete&lt;Customer, CustomersDbContext&gt;();
    /// </code>
    /// </summary>
    public static IServiceCollection AddSoftDelete<T, TContext>(this IServiceCollection services)
        where T : class, ISoftDelete
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<ISoftDeleteService<T>, SoftDeleteService<T, TContext>>();
        return services;
    }
}
