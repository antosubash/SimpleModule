using Microsoft.Extensions.DependencyInjection;

namespace SimpleModule.Core;

/// <summary>
/// Specifies the DI service lifetime for an auto-discovered contract implementation.
/// When absent, the default lifetime is <see cref="ServiceLifetime.Scoped"/>.
/// </summary>
/// <example>
/// <code>
/// [ContractLifetime(ServiceLifetime.Singleton)]
/// public sealed class MyCacheService : ICacheContracts { ... }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ContractLifetimeAttribute(ServiceLifetime lifetime) : Attribute
{
    public ServiceLifetime Lifetime { get; } = lifetime;
}
