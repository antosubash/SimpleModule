using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimpleModule.Database;
using SimpleModule.Datasets;

namespace SimpleModule.Tests.Shared.Datasets;

/// <summary>
/// Test subclass of <see cref="DatasetsDbContext"/> backed by a private
/// in-memory SQLite connection. Use the static <see cref="Create"/> factory
/// to obtain a ready-to-use, self-owning context; disposing the context
/// closes the underlying connection.
/// </summary>
public sealed class TestDatasetsDbContext : DatasetsDbContext
{
    private readonly SqliteConnection _connection;

    private TestDatasetsDbContext(
        DbContextOptions<DatasetsDbContext> options,
        IOptions<DatabaseOptions> dbOptions,
        SqliteConnection connection
    )
        : base(options, dbOptions)
    {
        _connection = connection;
    }

    /// <summary>
    /// Creates a <see cref="TestDatasetsDbContext"/> with a fresh in-memory
    /// SQLite database. Dispose the returned context to release the
    /// connection.
    /// </summary>
    public static TestDatasetsDbContext Create()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<DatasetsDbContext>()
            .UseSqlite(connection)
            .Options;

        var dbOptions = Options.Create(
            new DatabaseOptions { DefaultConnection = "Data Source=:memory:", Provider = "Sqlite" }
        );

        var context = new TestDatasetsDbContext(options, dbOptions, connection);
        context.Database.EnsureCreated();
        return context;
    }

    public override void Dispose()
    {
        base.Dispose();
        _connection.Dispose();
    }
}
