using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core.Inertia;
using SimpleModule.Core.Menu;
using SimpleModule.Tests.Shared.Fixtures;

namespace SimpleModule.Core.Tests.Inertia;

[Collection(TestCollections.Integration)]
public class InertiaResultTests
{
    private readonly SimpleModuleWebApplicationFactory _factory;

    public InertiaResultTests(SimpleModuleWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task InertiaEndpoint_WithoutInertiaHeader_ReturnsHtml()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");

        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("id=\"app\"");
        html.Should().Contain("data-page=\"app\"").And.Contain("type=\"application/json\"");
        html.Should().Contain("/js/app.js");
    }

    [Fact]
    public async Task InertiaEndpoint_WithInertiaHeader_ReturnsJson()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Inertia", "true");
        client.DefaultRequestHeaders.Add("X-Inertia-Version", InertiaMiddleware.Version);

        var response = await client.GetAsync("/");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("component").GetString().Should().Be("Dashboard/Home");
        json.TryGetProperty("props", out _).Should().BeTrue();
        json.TryGetProperty("url", out _).Should().BeTrue();
        json.TryGetProperty("version", out _).Should().BeTrue();
    }

    [Fact]
    public async Task InertiaEndpoint_JsonResponse_ContainsCorrectUrl()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Inertia", "true");
        client.DefaultRequestHeaders.Add("X-Inertia-Version", InertiaMiddleware.Version);

        var response = await client.GetAsync("/");
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("url").GetString().Should().Be("/");
    }

    [Fact]
    public async Task InertiaEndpoint_JsonResponse_HasInertiaResponseHeader()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Inertia", "true");
        client.DefaultRequestHeaders.Add("X-Inertia-Version", InertiaMiddleware.Version);

        var response = await client.GetAsync("/");
        response.Headers.Contains("X-Inertia").Should().BeTrue();
        response.Headers.GetValues("X-Inertia").Should().Contain("true");
    }

    [Fact]
    public async Task InertiaEndpoint_HtmlResponse_ContainsImportMap()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("importmap");
        html.Should().Contain("react");
    }

    [Fact]
    public async Task InertiaMiddleware_AddsVersionHeader()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/");
        response.Headers.Contains("X-Inertia-Version").Should().BeTrue();
    }

    [Fact]
    public async Task InertiaEndpoint_DeclaresAssemblyNameThatServesEachModuleBundle()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        var declared = ParseModuleAssemblies(html);
        declared.Should().NotBeEmpty("the client needs the mapping to build a bundle URL");

        // Every module that renders pages needs an entry, or the client is back to
        // guessing the bundle URL for it (#287). Key on the first segment of the page
        // name — that is what the client looks up, and it is only the [Module] name the
        // map is built from by convention, so asserting on the module name directly
        // would just restate how the map is built.
        var pages = _factory.Services.GetRequiredService<IReadOnlyList<AvailablePage>>();
        declared
            .Keys.Should()
            .Contain(pages.Select(p => p.PageRoute.Split('/')[0]).Distinct(StringComparer.Ordinal));

        // A module's RCL serves its assets under its AssemblyName. Declaring anything
        // else — such as the "SimpleModule."-prefixed module name, which is not an
        // assembly at all in a scaffolded app — 404s on first load of its pages.
        var loaded = AppDomain
            .CurrentDomain.GetAssemblies()
            .Select(a => a.GetName().Name)
            .ToHashSet(StringComparer.Ordinal);
        declared
            .Values.Should()
            .OnlyContain(
                assembly => loaded.Contains(assembly),
                "declared names must be assemblies"
            );
    }

    private static Dictionary<string, string> ParseModuleAssemblies(string html)
    {
        var match = Regex.Match(
            html,
            "<script data-module-assemblies[^>]*>(?<json>.*?)</script>",
            RegexOptions.Singleline,
            TimeSpan.FromSeconds(5)
        );
        match.Success.Should().BeTrue("the page shell must declare the module assemblies");

        return JsonSerializer.Deserialize<Dictionary<string, string>>(match.Groups["json"].Value)
            ?? [];
    }

    [Fact]
    public async Task InertiaMiddleware_VersionMismatch_Returns409()
    {
        // Create a handler that doesn't auto-redirect
        using var handler = _factory.Server.CreateHandler();
        using var client = new HttpClient(handler) { BaseAddress = _factory.Server.BaseAddress };
        client.DefaultRequestHeaders.Add("X-Inertia", "true");
        client.DefaultRequestHeaders.Add("X-Inertia-Version", "wrong-version");

        var response = await client.GetAsync("/");
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
