using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace SimpleModule.LoadTests.Infrastructure;

/// <summary>
/// Sets SQLite pragmas on every connection open to handle concurrent access under load.
/// busy_timeout makes SQLite wait (instead of failing) when the database is locked.
/// </summary>
public sealed class SqliteBusyTimeoutInterceptor : DbConnectionInterceptor
{
    public static readonly SqliteBusyTimeoutInterceptor Instance = new();

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        SetPragmas(connection);
    }

    public override Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        SetPragmas(connection);
        return Task.CompletedTask;
    }

    private static void SetPragmas(DbConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA busy_timeout=30000; PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL;";
        cmd.ExecuteNonQuery();
    }
}
