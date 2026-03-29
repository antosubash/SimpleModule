using FluentAssertions;
using SimpleModule.Marketplace.Contracts;

namespace SimpleModule.Marketplace.Tests.Unit;

public class CategoryMapperTests
{
    [Theory]
    [InlineData("auth", MarketplaceCategory.Auth)]
    [InlineData("authentication", MarketplaceCategory.Auth)]
    [InlineData("identity", MarketplaceCategory.Auth)]
    [InlineData("storage", MarketplaceCategory.Storage)]
    [InlineData("blob", MarketplaceCategory.Storage)]
    [InlineData("ui", MarketplaceCategory.UI)]
    [InlineData("components", MarketplaceCategory.UI)]
    [InlineData("analytics", MarketplaceCategory.Analytics)]
    [InlineData("email", MarketplaceCategory.Communication)]
    [InlineData("monitoring", MarketplaceCategory.Monitoring)]
    [InlineData("webhook", MarketplaceCategory.Integration)]
    public void MapCategory_KnownTag_ReturnsExpectedCategory(
        string tag,
        MarketplaceCategory expected
    )
    {
        var result = CategoryMapper.MapCategory([tag]);
        result.Should().Be(expected);
    }

    [Fact]
    public void MapCategory_UnknownTag_ReturnsOther()
    {
        var result = CategoryMapper.MapCategory(["something-unknown"]);
        result.Should().Be(MarketplaceCategory.Other);
    }

    [Fact]
    public void MapCategory_EmptyTags_ReturnsOther()
    {
        var result = CategoryMapper.MapCategory([]);
        result.Should().Be(MarketplaceCategory.Other);
    }

    [Fact]
    public void MapCategory_MultipleTags_ReturnsFirstMatch()
    {
        var result = CategoryMapper.MapCategory(["something", "auth", "storage"]);
        result.Should().Be(MarketplaceCategory.Auth);
    }
}
