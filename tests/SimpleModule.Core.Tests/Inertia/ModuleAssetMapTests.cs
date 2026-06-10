using System.Net;
using FluentAssertions;
using SimpleModule.Tests.Shared.Fixtures;

namespace SimpleModule.Core.Tests.Inertia;

[Collection(TestCollections.Integration)]
public class ModuleAssetMapTests
{
    private readonly SimpleModuleWebApplicationFactory _factory;

    public ModuleAssetMapTests(SimpleModuleWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HtmlShell_ContainsModuleAssetMap()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("id=\"sm-module-assets\"");
        html.Should()
            .Contain("_content/SimpleModule.FeatureFlags/SimpleModule.FeatureFlags.pages.js");
    }

    [Fact]
    public async Task AssetMapScript_IsJsonNotExecutable()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/");

        var html = await response.Content.ReadAsStringAsync();
        var scriptIndex = html.IndexOf("id=\"sm-module-assets\"", StringComparison.Ordinal);
        scriptIndex.Should().BeGreaterThan(0);

        var tagStart = html.LastIndexOf("<script", scriptIndex, StringComparison.Ordinal);
        var tag = html[tagStart..html.IndexOf('>', scriptIndex)];
        tag.Should().Contain("type=\"application/json\"");
    }
}
