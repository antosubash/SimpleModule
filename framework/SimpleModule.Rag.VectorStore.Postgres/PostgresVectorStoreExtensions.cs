using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel.Connectors.Postgres;
using Npgsql;

namespace SimpleModule.Rag.VectorStore.Postgres;

public static class PostgresVectorStoreExtensions
{
    public static IServiceCollection AddPostgresVectorStore(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<PostgresVectorStoreOptions>(
            configuration.GetSection("Rag:VectorStore:Postgres")
        );

        services.AddSingleton(
            typeof(Microsoft.Extensions.VectorData.VectorStore),
            sp =>
            {
                var opts =
                    configuration
                        .GetSection("Rag:VectorStore:Postgres")
                        .Get<PostgresVectorStoreOptions>()
                    ?? new PostgresVectorStoreOptions();
                var dataSource = NpgsqlDataSource.Create(opts.ConnectionString);
                return new PostgresVectorStore(dataSource);
            }
        );

        return services;
    }
}
