using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SimpleModule.Core.Health;

/// <summary>
/// Aggregates health status from all discovered modules by calling
/// <see cref="IModule.CheckHealthAsync"/> on each one.
/// </summary>
public sealed class ModuleHealthCheck(IEnumerable<IModule> modules) : IHealthCheck
{
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Health check must report failures, not throw"
    )]
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        var results = new Dictionary<string, object>();
        var worstStatus = ModuleHealthStatus.Healthy;

        foreach (var module in modules)
        {
            var moduleName = module.GetType().Name.Replace("Module", "", StringComparison.Ordinal);

            try
            {
                var status = module is IModuleLifecycle lifecycle
                    ? await lifecycle.CheckHealthAsync(cancellationToken)
                    : await module.CheckHealthAsync(cancellationToken);

                results[moduleName] = status.ToString();

                if (status > worstStatus)
                {
                    worstStatus = status;
                }
            }
            catch (Exception ex)
            {
                results[moduleName] = $"Error: {ex.Message}";
                worstStatus = ModuleHealthStatus.Unhealthy;
            }
        }

        return worstStatus switch
        {
            ModuleHealthStatus.Healthy => HealthCheckResult.Healthy(
                "All modules are healthy.",
                results
            ),
            ModuleHealthStatus.Degraded => HealthCheckResult.Degraded(
                "One or more modules are degraded.",
                data: results
            ),
            _ => HealthCheckResult.Unhealthy("One or more modules are unhealthy.", data: results),
        };
    }
}
