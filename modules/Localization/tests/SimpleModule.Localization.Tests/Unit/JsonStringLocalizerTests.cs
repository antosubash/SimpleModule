using System.Globalization;
using FluentAssertions;
using SimpleModule.Localization.Services;

namespace SimpleModule.Localization.Tests.Unit;

public sealed class JsonStringLocalizerTests
{
    private readonly TranslationLoader _loader;
    private readonly JsonStringLocalizer _localizer;

    public JsonStringLocalizerTests()
    {
        _loader = new TranslationLoader();
        _loader.InitializeFromDictionary(
            "en",
            new Dictionary<string, string>
            {
                ["products.title"] = "Products",
                ["products.create"] = "Create Product",
                ["products.welcome"] = "Welcome, {0}!",
            }
        );
        _loader.InitializeFromDictionary(
            "es",
            new Dictionary<string, string>
            {
                ["products.title"] = "Productos",
                ["products.create"] = "Crear Producto",
                ["products.welcome"] = "Bienvenido, {0}!",
            }
        );

        _localizer = new JsonStringLocalizer("products", _loader);
    }

    [Fact]
    public void Indexer_KnownKey_ReturnsValueWithResourceNotFoundFalse()
    {
        CultureInfo.CurrentUICulture = new CultureInfo("en");

        var result = _localizer["title"];

        result.Value.Should().Be("Products");
        result.ResourceNotFound.Should().BeFalse();
    }

    [Fact]
    public void Indexer_LocalizedKey_ReturnsLocaleSpecificValue()
    {
        CultureInfo.CurrentUICulture = new CultureInfo("es");

        var result = _localizer["title"];

        result.Value.Should().Be("Productos");
        result.ResourceNotFound.Should().BeFalse();
    }

    [Fact]
    public void Indexer_FallbackToEnglish_WhenLocaleNotFound()
    {
        CultureInfo.CurrentUICulture = new CultureInfo("fr");

        var result = _localizer["title"];

        result.Value.Should().Be("Products");
        result.ResourceNotFound.Should().BeFalse();
    }

    [Fact]
    public void Indexer_UnknownKey_ReturnsKeyAsValueWithResourceNotFoundTrue()
    {
        CultureInfo.CurrentUICulture = new CultureInfo("en");

        var result = _localizer["nonexistent"];

        result.Value.Should().Be("nonexistent");
        result.Name.Should().Be("nonexistent");
        result.ResourceNotFound.Should().BeTrue();
    }

    [Fact]
    public void Indexer_WithFormatArgs_FormatsValue()
    {
        CultureInfo.CurrentUICulture = new CultureInfo("en");

        var result = _localizer["welcome", "Alice"];

        result.Value.Should().Be("Welcome, Alice!");
        result.ResourceNotFound.Should().BeFalse();
    }

    [Fact]
    public void GetAllStrings_ReturnsBareKeysWithoutPrefix()
    {
        CultureInfo.CurrentUICulture = new CultureInfo("en");

        var results = _localizer.GetAllStrings(false).ToList();

        results.Should().HaveCount(3);
        results.Select(r => r.Name).Should().Contain("title");
        results.Select(r => r.Name).Should().Contain("create");
        results.Select(r => r.Name).Should().Contain("welcome");
    }
}
