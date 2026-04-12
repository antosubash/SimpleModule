using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SimpleModule.Core.Health;

/// <summary>
/// Aggregates health status from all discovered modules by calling
/// <see cref="IModule.CheckHealthAsync"/> on each one in parallel.
/// </summary>
public sealed class ModuleHealthCheck : IHealthCheck
{
    private readonly (IModule Module, string Name)[] _modules;

    public ModuleHealthCheck(IEnumerable<IModule> modules)
    {
        _modules = modules.Select(m => (m, GetModuleName(m))).ToArray();
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        var tasks = _modules.Select(entry =>
            CheckOneAsync(entry.Module, entry.Name, cancellationToken)
        );
        var results = await Task.WhenAll(tasks);

        var data = new Dictionary<string, object>(results.Length);
        var worstStatus = ModuleHealthStatus.Healthy;
        foreach (var (name, status, detail) in results)
        {
            data[name] = detail;
            if (status > worstStatus)
            {
                worstStatus = status;
            }
        }

        return worstStatus switch
        {
            ModuleHealthStatus.Healthy => HealthCheckResult.Healthy(
                "All modules are healthy.",
                data
            ),
            ModuleHealthStatus.Degraded => HealthCheckResult.Degraded(
                "One or more modules are degraded.",
                data: data
            ),
            _ => HealthCheckResult.Unhealthy("One or more modules are unhealthy.", data: data),
        };
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Health check must report failures, not throw"
    )]
    private static async Task<(
        string Name,
        ModuleHealthStatus Status,
        string Detail
    )> CheckOneAsync(IModule module, string name, CancellationToken cancellationToken)
    {
        try
        {
            var status = await module.CheckHealthAsync(cancellationToken);
            return (name, status, status.ToString());
        }
        catch (Exception ex)
        {
            return (name, ModuleHealthStatus.Unhealthy, $"Error: {ex.Message}");
        }
    }

    private static string GetModuleName(IModule module)
    {
        var type = module.GetType();
        var attribute = type.GetCustomAttribute<ModuleAttribute>();
        return attribute?.Name ?? type.Name;
    }
}
