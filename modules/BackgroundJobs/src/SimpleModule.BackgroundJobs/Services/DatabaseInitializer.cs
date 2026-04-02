using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SimpleModule.BackgroundJobs.Services;

internal sealed partial class DatabaseInitializer(
    IServiceScopeFactory scopeFactory,
    ILogger<DatabaseInitializer> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BackgroundJobsDbContext>();

        if (!await db.Database.EnsureCreatedAsync(cancellationToken))
        {
            // DB already exists — create tables for this context if they don't exist yet
            try
            {
                var creator = db.GetService<IRelationalDatabaseCreator>();
                if (creator is not null)
                {
                    await creator.CreateTablesAsync(cancellationToken);
                }
            }
#pragma warning disable CA1031 // Tables may already exist
            catch (Exception ex)
#pragma warning restore CA1031
            {
                LogTablesExist(logger, ex);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(Level = LogLevel.Debug, Message = "BackgroundJobs tables already exist")]
    private static partial void LogTablesExist(ILogger logger, Exception ex);
}
