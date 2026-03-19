using FluentAssertions;
using SimpleModule.Database;

namespace SimpleModule.Database.Tests;

public sealed class DatabaseProviderExtensionsTests
{
    [Theory]
    [InlineData(DatabaseProvider.Sqlite, "Sqlite")]
    [InlineData(DatabaseProvider.PostgreSql, "PostgreSql")]
    [InlineData(DatabaseProvider.SqlServer, "SqlServer")]
    public void ToStringFast_ReturnsCorrectName(DatabaseProvider provider, string expected)
    {
        provider.ToStringFast().Should().Be(expected);
    }

    [Theory]
    [InlineData("Sqlite", DatabaseProvider.Sqlite)]
    [InlineData("PostgreSql", DatabaseProvider.PostgreSql)]
    [InlineData("SqlServer", DatabaseProvider.SqlServer)]
    public void TryParse_ValidName_ReturnsTrue(string name, DatabaseProvider expected)
    {
        DatabaseProviderExtensions.TryParse(name, out var result).Should().BeTrue();
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("sqlite")]
    [InlineData("POSTGRESQL")]
    [InlineData("sqlserver")]
    public void TryParse_CaseInsensitive_ReturnsTrue(string name)
    {
        // Use case-insensitive span-based parsing
        DatabaseProviderExtensions.TryParse(name, out var result, true).Should().BeTrue();
        Enum.IsDefined(result).Should().BeTrue();
    }

    [Fact]
    public void TryParse_InvalidName_ReturnsFalse()
    {
        DatabaseProviderExtensions.TryParse("MySQL", out _).Should().BeFalse();
    }

    [Theory]
    [InlineData(DatabaseProvider.Sqlite)]
    [InlineData(DatabaseProvider.PostgreSql)]
    [InlineData(DatabaseProvider.SqlServer)]
    public void IsDefined_ValidValue_ReturnsTrue(DatabaseProvider provider)
    {
        DatabaseProviderExtensions.IsDefined(provider).Should().BeTrue();
    }

    [Fact]
    public void IsDefined_InvalidValue_ReturnsFalse()
    {
        DatabaseProviderExtensions.IsDefined((DatabaseProvider)999).Should().BeFalse();
    }
}
