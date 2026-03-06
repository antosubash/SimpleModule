using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SimpleModule.Database.Health;

public sealed class DatabaseHealthCheck(IServiceProvider serviceProvider) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var infos = scope.ServiceProvider.GetServices<ModuleDbContextInfo>();

            foreach (var info in infos)
            {
                var dbContext = (DbContext)
                    scope.ServiceProvider.GetRequiredService(info.DbContextType);
                if (!await dbContext.Database.CanConnectAsync(cancellationToken))
                {
                    return HealthCheckResult.Unhealthy(
                        $"Cannot connect to database for module '{info.ModuleName}'"
                    );
                }
            }

            return HealthCheckResult.Healthy("All module databases are reachable.");
        }
#pragma warning disable CA1031 // Do not catch general exception types - health check must report failures, not throw
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database health check failed.", ex);
        }
#pragma warning restore CA1031
    }
}
