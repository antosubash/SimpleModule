using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimpleModule.Core.Rag;

namespace SimpleModule.Rag;

public sealed partial class KnowledgeIndexingHostedService(
    IServiceProvider serviceProvider,
    IOptions<RagOptions> options,
    ILogger<KnowledgeIndexingHostedService> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.IndexOnStartup)
        {
            LogIndexingDisabled(logger);
            return;
        }

        using var scope = serviceProvider.CreateScope();
        var knowledgeStore = scope.ServiceProvider.GetService<IKnowledgeStore>();
        if (knowledgeStore is null)
        {
            LogNoKnowledgeStore(logger);
            return;
        }

        var sources = scope.ServiceProvider.GetServices<IKnowledgeSource>().ToList();
        var tasks = sources.Select(source =>
            IndexSourceAsync(knowledgeStore, source, cancellationToken)
        );
        await Task.WhenAll(tasks);
    }

    private async Task IndexSourceAsync(
        IKnowledgeStore knowledgeStore,
        IKnowledgeSource source,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var documents = await source.GetDocumentsAsync(cancellationToken);
            if (documents.Count == 0)
                return;

            LogIndexing(logger, documents.Count, source.CollectionName);
            await knowledgeStore.IndexDocumentsAsync(
                source.CollectionName,
                documents,
                cancellationToken
            );
        }
        catch (OperationCanceledException)
        {
            throw;
        }
#pragma warning disable CA1031 // Catch more specific exception - intentional isolation of knowledge source failures
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogIndexingFailed(logger, ex, source.CollectionName);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Knowledge indexing on startup is disabled"
    )]
    private static partial void LogIndexingDisabled(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "No IKnowledgeStore registered, skipping knowledge indexing"
    )]
    private static partial void LogNoKnowledgeStore(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Indexing {Count} documents for collection '{CollectionName}'"
    )]
    private static partial void LogIndexing(ILogger logger, int count, string collectionName);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to index knowledge for collection '{CollectionName}'"
    )]
    private static partial void LogIndexingFailed(
        ILogger logger,
        Exception ex,
        string collectionName
    );
}
