using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SimpleModule.Core.Inertia;
using SimpleModule.Tests.Shared.Fixtures;

namespace SimpleModule.Localization.Tests.Integration;

public class InertiaTranslationPropsTests : IClassFixture<SimpleModuleWebApplicationFactory>
{
    private readonly SimpleModuleWebApplicationFactory _factory;

    public InertiaTranslationPropsTests(SimpleModuleWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Inertia_Response_Contains_Locale_And_Translations()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Inertia", "true");
        client.DefaultRequestHeaders.Add("X-Inertia-Version", InertiaMiddleware.Version);

        var response = await client.GetAsync("/products/browse");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var props = json.GetProperty("props");

        props
            .TryGetProperty("locale", out var locale)
            .Should()
            .BeTrue("Inertia response should contain locale shared prop");
        locale.GetString().Should().NotBeNullOrEmpty();

        props
            .TryGetProperty("translations", out _)
            .Should()
            .BeTrue("Inertia response should contain translations shared prop");
    }

    [Fact]
    public async Task Locale_Defaults_To_En_When_No_AcceptLanguage()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Inertia", "true");
        client.DefaultRequestHeaders.Add("X-Inertia-Version", InertiaMiddleware.Version);

        var response = await client.GetAsync("/products/browse");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var props = json.GetProperty("props");

        props.GetProperty("locale").GetString().Should().Be("en");
    }

    [Fact]
    public async Task Locale_Falls_Back_To_Default_When_AcceptLanguage_Not_Supported()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Inertia", "true");
        client.DefaultRequestHeaders.Add("X-Inertia-Version", InertiaMiddleware.Version);
        client.DefaultRequestHeaders.Add("Accept-Language", "fr-FR,fr;q=0.9,en;q=0.8");

        var response = await client.GetAsync("/products/browse");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var props = json.GetProperty("props");

        // "fr" is not a supported locale — should fall back through to "en" from Accept-Language
        props.GetProperty("locale").GetString().Should().Be("en");
    }
}
