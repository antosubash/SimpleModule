using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SimpleModule.Core.FeatureFlags;
using SimpleModule.FeatureFlags.Contracts;

namespace SimpleModule.FeatureFlags;

public sealed partial class FeatureFlagSyncService(
    IServiceProvider serviceProvider,
    IFeatureFlagRegistry registry,
    ILogger<FeatureFlagSyncService> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FeatureFlagsDbContext>();

        var definitions = registry.GetAllDefinitions();
        var existingFlags = await db.FeatureFlags.ToListAsync(cancellationToken);
        var existingNames = existingFlags.ToDictionary(f => f.Name);

        var newCount = 0;
        var deprecatedCount = 0;

        // Create DB rows for new features
        foreach (var def in definitions)
        {
            if (!existingNames.ContainsKey(def.Name))
            {
                db.FeatureFlags.Add(
                    new FeatureFlagEntity { Name = def.Name, IsEnabled = def.DefaultEnabled }
                );
                newCount++;
            }
        }

        // Mark removed features as deprecated
        var registryNames = registry.GetAllFeatureNames();
        foreach (var entity in existingFlags)
        {
            if (!registryNames.Contains(entity.Name) && !entity.IsDeprecated)
            {
                entity.IsDeprecated = true;
                deprecatedCount++;
            }
        }

        if (newCount > 0 || deprecatedCount > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            LogSync(logger, newCount, deprecatedCount);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Feature flag sync: {NewCount} new, {DeprecatedCount} deprecated"
    )]
    private static partial void LogSync(ILogger logger, int newCount, int deprecatedCount);
}
