using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimpleModule.Database;
using SimpleModule.Permissions;

namespace Permissions.Tests.Helpers;

public sealed class TestDbContextFactory : IDisposable
{
    private readonly SqliteConnection _connection;

    public TestDbContextFactory()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    public PermissionsDbContext Create()
    {
        var options = new DbContextOptionsBuilder<PermissionsDbContext>()
            .UseSqlite(_connection)
            .Options;

        var dbOptions = Options.Create(new DatabaseOptions());
        var context = new PermissionsDbContext(options, dbOptions);
        context.Database.EnsureCreated();
        return context;
    }

    public void Dispose() => _connection.Dispose();
}
