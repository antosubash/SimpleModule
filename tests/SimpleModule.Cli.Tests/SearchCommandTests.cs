using FluentAssertions;
using SimpleModule.Cli.Commands.Search;

namespace SimpleModule.Cli.Tests;

public class SearchCommandTests
{
    [Theory]
    [InlineData("SimpleModule.X.1.2.3", "SimpleModule.X", "1.2.3")]
    [InlineData("SimpleModule.X.Contracts.1.0.0-pre.1", "SimpleModule.X.Contracts", "1.0.0-pre.1")]
    [InlineData("Items.0.0.99-local", "Items", "0.0.99-local")]
    public void SplitIdAndVersion_SplitsAtFirstNumericSegment(
        string fileName,
        string expectedId,
        string expectedVersion
    )
    {
        var result = SearchCommand.SplitIdAndVersion(fileName);

        result.Should().NotBeNull();
        result!.Value.Id.Should().Be(expectedId);
        result.Value.Version.Should().Be(expectedVersion);
    }

    [Fact]
    public void SplitIdAndVersion_NoVersion_ReturnsNull()
    {
        SearchCommand.SplitIdAndVersion("JustAName").Should().BeNull();
    }
}
