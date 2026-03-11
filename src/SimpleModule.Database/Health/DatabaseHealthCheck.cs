using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SimpleModule.Core.Constants;

namespace SimpleModule.Database.Health;

public sealed class DatabaseHealthCheck(IServiceProvider serviceProvider) : IHealthCheck
{
    private static readonly CompositeFormat CannotConnectFormat = CompositeFormat.Parse(
        HealthCheckConstants.CannotConnectFormat
    );

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
                        string.Format(CultureInfo.InvariantCulture, CannotConnectFormat, info.ModuleName)
                    );
                }
            }

            return HealthCheckResult.Healthy(HealthCheckConstants.AllDatabasesReachable);
        }
#pragma warning disable CA1031 // Do not catch general exception types - health check must report failures, not throw
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(HealthCheckConstants.DatabaseHealthCheckFailed, ex);
        }
#pragma warning restore CA1031
    }
}
