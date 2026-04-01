using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Options;
using SimpleModule.BackgroundJobs;
using SimpleModule.Database;

namespace BackgroundJobs.Tests.Helpers;

public class TestBackgroundJobsDbContext(
    DbContextOptions<BackgroundJobsDbContext> options,
    IOptions<DatabaseOptions> dbOptions
) : BackgroundJobsDbContext(options, dbOptions)
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

    public BackgroundJobsDbContext Create()
    {
        var options = new DbContextOptionsBuilder<BackgroundJobsDbContext>()
            .UseSqlite(_connection)
            .Options;

        var dbOptions = Options.Create(
            new DatabaseOptions { DefaultConnection = "Data Source=:memory:" }
        );
        var context = new TestBackgroundJobsDbContext(options, dbOptions);
        context.Database.EnsureCreated();
        return context;
    }

    public void Dispose() => _connection.Dispose();
}
