using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Options;
using SimpleModule.AuditLogs;
using SimpleModule.Database;

namespace AuditLogs.Tests.Helpers;

/// <summary>
/// Test DbContext subclass that configures DateTimeOffset-to-string conversion
/// to work around SQLite's lack of DateTimeOffset support in ORDER BY clauses.
/// </summary>
public class TestAuditLogsDbContext(
    DbContextOptions<AuditLogsDbContext> options,
    IOptions<DatabaseOptions> dbOptions
) : AuditLogsDbContext(options, dbOptions)
{
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        configurationBuilder
            .Properties<DateTimeOffset>()
            .HaveConversion<DateTimeOffsetToStringConverter>();

        configurationBuilder
            .Properties<DateTimeOffset?>()
            .HaveConversion<DateTimeOffsetToStringConverter>();
    }
}

public sealed class TestDbContextFactory : IDisposable
{
    private readonly SqliteConnection _connection;

    public TestDbContextFactory()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    public AuditLogsDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AuditLogsDbContext>()
            .UseSqlite(_connection)
            .Options;

        var dbOptions = Options.Create(new DatabaseOptions());
        var context = new TestAuditLogsDbContext(options, dbOptions);
        context.Database.EnsureCreated();
        return context;
    }

    public void Dispose() => _connection.Dispose();
}
