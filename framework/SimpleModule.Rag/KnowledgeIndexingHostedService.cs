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
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Yield to allow the web server to start accepting requests
        await Task.Yield();

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
        var tasks = sources.Select(async source =>
        {
            using var sourceScope = serviceProvider.CreateScope();
            var scopedStore = sourceScope.ServiceProvider.GetRequiredService<IKnowledgeStore>();
            await IndexSourceAsync(scopedStore, source, sourceScope.ServiceProvider, stoppingToken);
        });
        await Task.WhenAll(tasks);
    }

    private async Task IndexSourceAsync(
        IKnowledgeStore knowledgeStore,
        IKnowledgeSource source,
        IServiceProvider scopedProvider,
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

            // Preprocess documents into structured formats if a preprocessor is registered
            var preprocessor = scopedProvider.GetService<IKnowledgePreprocessor>();
            if (preprocessor is not null)
            {
                LogPreprocessing(logger, documents.Count, source.CollectionName);
                await preprocessor.PreprocessAsync(
                    source.CollectionName,
                    documents,
                    cancellationToken
                );
            }
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
        Level = LogLevel.Information,
        Message = "Preprocessing {Count} documents for collection '{CollectionName}'"
    )]
    private static partial void LogPreprocessing(ILogger logger, int count, string collectionName);

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
