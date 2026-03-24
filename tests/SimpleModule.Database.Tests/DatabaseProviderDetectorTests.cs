namespace SimpleModule.Database.Tests;

public sealed class DatabaseProviderDetectorTests
{
    #region Explicit Provider Tests

    [Theory]
    [InlineData("Sqlite")]
    [InlineData("sqlite")]
    [InlineData("SQLITE")]
    public void Detect_ExplicitSqliteProvider_ReturnsSqlite(string explicitProvider)
    {
        DatabaseProviderDetector
            .Detect("any connection string", explicitProvider)
            .Should()
            .Be(DatabaseProvider.Sqlite);
    }

    [Theory]
    [InlineData("PostgreSql")]
    [InlineData("postgresql")]
    [InlineData("POSTGRESQL")]
    public void Detect_ExplicitPostgreSqlProvider_ReturnsPostgreSql(string explicitProvider)
    {
        DatabaseProviderDetector
            .Detect("any connection string", explicitProvider)
            .Should()
            .Be(DatabaseProvider.PostgreSql);
    }

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("sqlserver")]
    public void Detect_ExplicitSqlServerProvider_ReturnsSqlServer(string explicitProvider)
    {
        DatabaseProviderDetector
            .Detect("any connection string", explicitProvider)
            .Should()
            .Be(DatabaseProvider.SqlServer);
    }

    [Theory]
    [InlineData("InvalidProvider")]
    [InlineData("MySQL")]
    [InlineData("Oracle")]
    public void Detect_InvalidExplicitProvider_ThrowsInvalidOperationException(
        string invalidProvider
    )
    {
        var action = () =>
            DatabaseProviderDetector.Detect("any connection string", invalidProvider);

        action
            .Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*Invalid database provider*Valid providers are*");
    }

    #endregion

    #region Connection String Heuristic Tests

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
    [InlineData("host=127.0.0.1;username=postgres")]
    public void Detect_PostgreSqlConnectionStrings_ReturnsPostgreSql(string connectionString)
    {
        DatabaseProviderDetector.Detect(connectionString).Should().Be(DatabaseProvider.PostgreSql);
    }

    [Theory]
    [InlineData("Server=.\\SQLEXPRESS;Initial Catalog=mydb;Trusted_Connection=True")]
    [InlineData("Server=(localdb)\\mssqllocaldb;Initial Catalog=mydb;Trusted_Connection=True")]
    [InlineData("Server=localhost;Initial Catalog=mydb;User Id=sa;Password=pass")]
    [InlineData("Server=sql.example.com;Initial Catalog=proddb")]
    public void Detect_SqlServerConnectionStrings_ReturnsSqlServer(string connectionString)
    {
        DatabaseProviderDetector.Detect(connectionString).Should().Be(DatabaseProvider.SqlServer);
    }

    #endregion

    #region Error Cases

    [Fact]
    public void Detect_EmptyConnectionString_ThrowsInvalidOperationException()
    {
        var action = () => DatabaseProviderDetector.Detect(string.Empty);

        action
            .Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*Unable to detect database provider*");
    }

    [Theory]
    [InlineData("User=app;Password=secret")]
    [InlineData("ConnectionTimeout=30;MultipleActiveResultSets=true")]
    [InlineData("DefaultDatabase=myapp")]
    [InlineData("Unknown=value;AndAnother=pair")]
    public void Detect_UnknownConnectionStringPatterns_ThrowsInvalidOperationException(
        string unknownConnectionString
    )
    {
        var action = () => DatabaseProviderDetector.Detect(unknownConnectionString);

        action
            .Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*Unable to detect database provider*Recognized patterns*");
    }

    [Fact]
    public void Detect_InvalidProviderWithValidConnectionString_ThrowsInvalidOperationException()
    {
        var action = () => DatabaseProviderDetector.Detect("Host=localhost;Database=mydb", "Redis");

        action
            .Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*Invalid database provider*Redis*");
    }

    #endregion

    #region Helper Method Tests

    [Fact]
    public void GetValidProviders_ReturnsAllProviders()
    {
        var providers = DatabaseProviderDetector.GetValidProviders();

        providers.Should().HaveCount(3).And.Contain(["Sqlite", "PostgreSql", "SqlServer"]);
    }

    [Fact]
    public void GetValidProviders_ReturnsReadOnlyList()
    {
        var providers = DatabaseProviderDetector.GetValidProviders();

        providers.Should().BeAssignableTo<IReadOnlyList<string>>();
    }

    #endregion

    #region Explicit Provider Override Tests

    [Fact]
    public void Detect_ExplicitProviderOverridesConnectionStringHeuristics()
    {
        var postgresConnectionString =
            "Host=localhost;Database=mydb;Username=postgres;Password=secret";

        // Despite PostgreSQL connection string, explicit provider takes precedence
        DatabaseProviderDetector
            .Detect(postgresConnectionString, "Sqlite")
            .Should()
            .Be(DatabaseProvider.Sqlite);
    }

    [Fact]
    public void Detect_EmptyExplicitProviderFallsBackToHeuristics()
    {
        var postgresConnectionString = "Host=localhost;Database=mydb";

        // Empty/whitespace explicit provider is ignored, heuristics apply
        DatabaseProviderDetector
            .Detect(postgresConnectionString, "   ")
            .Should()
            .Be(DatabaseProvider.PostgreSql);
    }

    #endregion
}
