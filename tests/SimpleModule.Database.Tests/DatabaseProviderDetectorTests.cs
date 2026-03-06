using FluentAssertions;
using SimpleModule.Database;

namespace SimpleModule.Database.Tests;

public sealed class DatabaseProviderDetectorTests
{
    [Theory]
    [InlineData("Data Source=app.db")]
    [InlineData("Data Source=:memory:")]
    [InlineData("Data Source=C:\\data\\myapp.db")]
    public void Detect_SqliteConnectionStrings_ReturnsSqlite(string connectionString)
    {
        DatabaseProviderDetector.Detect(connectionString).Should().Be(DatabaseProvider.Sqlite);
    }

    [Theory]
    [InlineData("Host=localhost;Database=mydb;Username=postgres;Password=secret")]
    [InlineData("Host=db.example.com;Database=prod;Username=app;Password=pass")]
    public void Detect_PostgreSqlConnectionStrings_ReturnsPostgreSql(string connectionString)
    {
        DatabaseProviderDetector.Detect(connectionString).Should().Be(DatabaseProvider.PostgreSql);
    }

    [Theory]
    [InlineData("Server=.\\SQLEXPRESS;Initial Catalog=mydb;Trusted_Connection=True")]
    [InlineData("Server=(localdb)\\mssqllocaldb;Initial Catalog=mydb;Trusted_Connection=True")]
    [InlineData("Server=localhost;Initial Catalog=mydb;User Id=sa;Password=pass")]
    public void Detect_SqlServerConnectionStrings_ReturnsSqlServer(string connectionString)
    {
        DatabaseProviderDetector.Detect(connectionString).Should().Be(DatabaseProvider.SqlServer);
    }

    [Fact]
    public void Detect_EmptyConnectionString_DefaultsToSqlite()
    {
        DatabaseProviderDetector.Detect(string.Empty).Should().Be(DatabaseProvider.Sqlite);
    }
}
