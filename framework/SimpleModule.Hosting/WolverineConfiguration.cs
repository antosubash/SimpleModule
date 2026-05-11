using System.Reflection;
using SimpleModule.Core.Entities;
using SimpleModule.Database;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;
using Wolverine.Sqlite;
using Wolverine.SqlServer;

namespace SimpleModule.Hosting;

internal static class WolverineConfiguration
{
    internal const string SchemaName = "wolverine";

    /// <summary>
    /// Wires Wolverine for in-process durable messaging backed by the configured database.
    /// Handler discovery covers every module assembly; envelopes persist to the message
    /// store before dispatch, local listeners are gated by the durable inbox, and entity
    /// events flushed by <c>SaveChangesAndFlushMessagesAsync</c> commit atomically with
    /// the EF write.
    /// </summary>
    internal static void Configure(
        WolverineOptions opts,
        IReadOnlyList<Assembly> moduleAssemblies,
        DatabaseProvider provider,
        string connectionString
    )
    {
        foreach (var assembly in moduleAssemblies)
        {
            opts.Discovery.IncludeAssembly(assembly);
        }

        switch (provider)
        {
            case DatabaseProvider.PostgreSql:
                opts.PersistMessagesWithPostgresql(connectionString, SchemaName);
                break;
            case DatabaseProvider.SqlServer:
                opts.PersistMessagesWithSqlServer(connectionString, SchemaName);
                break;
            case DatabaseProvider.Sqlite:
                opts.PersistMessagesWithSqlite(connectionString);
                break;
            default:
                throw new InvalidOperationException(
                    $"Unsupported database provider for Wolverine durability: {provider}"
                );
        }

        opts.UseEntityFrameworkCoreTransactions();
        opts.PublishDomainEventsFromEntityFrameworkCore<IHasDomainEvents>(x => x.Events);
        opts.Policies.UseDurableLocalQueues();
        opts.Policies.UseDurableInboxOnAllListeners();
    }
}
